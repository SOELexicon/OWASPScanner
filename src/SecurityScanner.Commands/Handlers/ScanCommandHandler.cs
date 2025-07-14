using Microsoft.Extensions.Logging;
using SecurityScanner.Commands.Middleware;
using SecurityScanner.Commands.Models;
using SecurityScanner.Core.Interfaces;
using SecurityScanner.Core.Models;
using SecurityScanner.Services.Orchestration;
using System.Diagnostics;

namespace SecurityScanner.Commands.Handlers;

public interface IScanCommandHandler
{
    Task<ScanCommandResponse> HandleAsync(ScanCommandRequest request, CancellationToken cancellationToken = default);
}

public class ScanCommandHandler : IScanCommandHandler
{
    private readonly IScanOrchestrator _scanOrchestrator;
    private readonly IValidationMiddleware _validationMiddleware;
    private readonly ILoggingMiddleware _loggingMiddleware;
    private readonly ILogger<ScanCommandHandler> _logger;

    public ScanCommandHandler(
        IScanOrchestrator scanOrchestrator,
        IValidationMiddleware validationMiddleware,
        ILoggingMiddleware loggingMiddleware,
        ILogger<ScanCommandHandler> logger)
    {
        _scanOrchestrator = scanOrchestrator;
        _validationMiddleware = validationMiddleware;
        _loggingMiddleware = loggingMiddleware;
        _logger = logger;
    }

    public async Task<ScanCommandResponse> HandleAsync(ScanCommandRequest request, CancellationToken cancellationToken = default)
    {
        return await _loggingMiddleware.ExecuteWithLoggingAsync(
            "ScanCommand",
            async () => await ExecuteScanAsync(request, cancellationToken),
            request,
            cancellationToken);
    }

    private async Task<ScanCommandResponse> ExecuteScanAsync(ScanCommandRequest request, CancellationToken cancellationToken)
    {
        // Validate the request
        var validationResult = await _validationMiddleware.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new ScanCommandResponse
            {
                Success = false,
                ErrorMessage = string.Join("; ", validationResult.Errors),
                Timestamp = DateTime.UtcNow
            };
        }

        var totalStopwatch = Stopwatch.StartNew();
        var response = new ScanCommandResponse { Success = true };
        var allScanResults = new List<ScanResult>();

        try
        {
            // Determine domains to scan
            var domainsToScan = await GetDomainsToScanAsync(request);
            if (!domainsToScan.Any())
            {
                return new ScanCommandResponse
                {
                    Success = false,
                    ErrorMessage = "No domains found to scan",
                    Timestamp = DateTime.UtcNow
                };
            }

            // Create scan configurations for each domain
            var scanConfigurations = CreateScanConfigurations(domainsToScan, request);

            _logger.LogInformation("Starting scan for {DomainCount} domains with {ToolCount} tools", 
                domainsToScan.Count, request.Tools.Count);

            // Execute scans using the orchestrator
            var scanConfiguration = new ScanConfiguration
            {
                Domains = scanConfigurations,
                EnabledScanners = request.Tools,
                MaxConcurrentScans = request.MaxConcurrent,
                DefaultTimeout = TimeSpan.FromSeconds(request.TimeoutSeconds),
                ParallelExecution = request.ParallelExecution,
                JsonOutput = request.JsonOutput,
                OutputFile = request.OutputFile,
                LoadTestSettings = new LoadTestSettings
                {
                    RequestsPerSecond = request.LoadTestRps,
                    DurationSeconds = request.LoadTestDuration,
                    RampUpSeconds = request.LoadTestRampUp,
                    MaxConcurrentRequests = request.LoadTestMaxConcurrent
                }
            };

            var scanResults = await _scanOrchestrator.ExecuteScansAsync(scanConfiguration, cancellationToken);

            allScanResults.AddRange(scanResults);

            // Generate summary
            totalStopwatch.Stop();
            response.ScanResults = allScanResults;
            response.Summary = GenerateScanSummary(allScanResults, totalStopwatch.Elapsed);

            _logger.LogInformation("Scan completed - {SuccessCount}/{TotalCount} domains successful, {VulnCount} vulnerabilities found",
                response.Summary.SuccessfulScans, response.Summary.TotalDomains, response.Summary.TotalVulnerabilities);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan execution failed");
            return new ScanCommandResponse
            {
                Success = false,
                ErrorMessage = $"Scan execution failed: {ex.Message}",
                Timestamp = DateTime.UtcNow,
                ScanResults = allScanResults,
                Summary = GenerateScanSummary(allScanResults, totalStopwatch.Elapsed)
            };
        }
    }

    private Task<List<string>> GetDomainsToScanAsync(ScanCommandRequest request)
    {
        var domains = new List<string>();

        if (request.ScanAll)
        {
            // TODO: In future versions, load from configuration file
            _logger.LogWarning("ScanAll option is not yet implemented - defaulting to provided domains");
            domains.AddRange(request.Domains);
        }
        else
        {
            domains.AddRange(request.Domains);
        }

        // Remove duplicates and normalize
        var normalizedDomains = domains
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(NormalizeDomain)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(normalizedDomains);
    }

    private static string NormalizeDomain(string domain)
    {
        // Remove protocol if present
        domain = domain.Replace("https://", "").Replace("http://", "");
        
        // Remove trailing slash and whitespace
        domain = domain.TrimEnd('/').Trim();
        
        return domain.ToLowerInvariant();
    }

    private List<DomainConfig> CreateScanConfigurations(List<string> domains, ScanCommandRequest request)
    {
        var configurations = new List<DomainConfig>();

        foreach (var domain in domains)
        {
            var config = new DomainConfig
            {
                Name = domain,
                Url = EnsureUrlScheme(domain),
                EnabledScanners = request.Tools
            };

            // Add load test specific settings to scanner settings if load testing is enabled
            if (request.Tools.Contains(ScannerType.LoadTest))
            {
                config.ScannerSettings["LoadTestRps"] = request.LoadTestRps;
                config.ScannerSettings["LoadTestDuration"] = request.LoadTestDuration;
                config.ScannerSettings["LoadTestRampUp"] = request.LoadTestRampUp;
                config.ScannerSettings["LoadTestMaxConcurrent"] = request.LoadTestMaxConcurrent;
            }

            configurations.Add(config);
        }

        return configurations;
    }

    private static string EnsureUrlScheme(string domain)
    {
        if (domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return domain;
        }
        
        return $"https://{domain}";
    }

    private static ScanSummary GenerateScanSummary(List<ScanResult> scanResults, TimeSpan totalDuration)
    {
        var summary = new ScanSummary
        {
            TotalDomains = scanResults.Count,
            SuccessfulScans = scanResults.Count(r => r.Status == ScanStatus.Completed),
            FailedScans = scanResults.Count(r => r.Status == ScanStatus.Failed),
            TotalScanTimeSeconds = totalDuration.TotalSeconds
        };

        // Calculate vulnerability counts
        foreach (var result in scanResults)
        {
            summary.TotalVulnerabilities += result.VulnerabilityCount;
            summary.CriticalVulnerabilities += result.Vulnerabilities.Count(v => v.Severity == SeverityLevel.Critical);
            summary.HighVulnerabilities += result.Vulnerabilities.Count(v => v.Severity == SeverityLevel.High);
        }

        return summary;
    }
}