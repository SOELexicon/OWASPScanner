using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SecurityScanner.Core.Interfaces;
using SecurityScanner.Core.Models;
using SecurityScanner.Infrastructure.Configuration;

namespace SecurityScanner.Services.Scanners;

public class OwaspZapService : IScannerService
{
    public string ServiceName => "OWASP ZAP";
    public ScannerType ScannerType => ScannerType.OwaspZap;

    private readonly HttpClient _httpClient;
    private readonly ILogger<OwaspZapService> _logger;
    private readonly OwaspZapSettings _settings;

    public OwaspZapService(
        IHttpClientFactory httpClientFactory,
        ILogger<OwaspZapService> logger,
        IOptions<ScannerSettings> scannerSettings)
    {
        _httpClient = httpClientFactory.CreateClient("OwaspZap");
        _logger = logger;
        _settings = scannerSettings.Value.OwaspZap;
    }

    public async Task<ScanResult> ScanAsync(DomainConfig domain, CancellationToken cancellationToken = default)
    {
        var result = new ScanResult
        {
            Domain = domain.Name,
            Url = domain.Url,
            Scanner = ServiceName,
            StartTime = DateTime.UtcNow
        };

        _logger.LogInformation("Starting OWASP ZAP scan for {Domain}", domain.Name);

        try
        {
            // Check if ZAP is available
            if (!await IsZapAvailableAsync(cancellationToken))
            {
                result.Status = ScanStatus.Failed;
                result.ErrorMessage = "OWASP ZAP is not available. Please ensure ZAP is running and accessible.";
                return result;
            }

            // Create a new session
            var sessionName = $"SecurityScanner-{Guid.NewGuid():N}";
            await CreateNewSessionAsync(sessionName, cancellationToken);

            try
            {
                // Spider the target
                if (_settings.EnablePassiveScan)
                {
                    _logger.LogInformation("Starting spider scan for {Domain}", domain.Name);
                    await SpiderTargetAsync(domain.Url, cancellationToken);
                }

                // Active scan
                if (_settings.EnableActiveScan)
                {
                    _logger.LogInformation("Starting active scan for {Domain}", domain.Name);
                    await ActiveScanAsync(domain.Url, cancellationToken);
                }

                // Get scan results
                var zapResults = await GetScanResultsAsync(cancellationToken);
                result.Vulnerabilities = ConvertZapVulnerabilities(zapResults, domain.Url);
                result.Status = ScanStatus.Completed;
                result.ScannerSpecificData["zapResults"] = zapResults;

                _logger.LogInformation("Completed OWASP ZAP scan for {Domain}. Found {IssueCount} issues", 
                    domain.Name, result.VulnerabilityCount);
            }
            finally
            {
                // Clean up session
                await DeleteSessionAsync(sessionName, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            result.Status = ScanStatus.Failed;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "OWASP ZAP scan failed for {Domain}", domain.Name);
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }

    private async Task<bool> IsZapAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_settings.ApiUrl}/JSON/core/view/version/";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task CreateNewSessionAsync(string sessionName, CancellationToken cancellationToken)
    {
        var url = $"{_settings.ApiUrl}/JSON/core/action/newSession/?name={Uri.EscapeDataString(sessionName)}";
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            url += $"&apikey={_settings.ApiKey}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteSessionAsync(string sessionName, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_settings.ApiUrl}/JSON/core/action/deleteSession/?name={Uri.EscapeDataString(sessionName)}";
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                url += $"&apikey={_settings.ApiKey}";
            }

            await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete ZAP session {SessionName}", sessionName);
        }
    }

    private async Task SpiderTargetAsync(string targetUrl, CancellationToken cancellationToken)
    {
        var url = $"{_settings.ApiUrl}/JSON/spider/action/scan/?url={Uri.EscapeDataString(targetUrl)}";
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            url += $"&apikey={_settings.ApiKey}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonConvert.DeserializeObject<ZapApiResponse>(content);
        var scanId = result?.Scan;

        if (!string.IsNullOrEmpty(scanId))
        {
            await WaitForSpiderCompletionAsync(scanId, cancellationToken);
        }
    }

    private async Task WaitForSpiderCompletionAsync(string scanId, CancellationToken cancellationToken)
    {
        var maxAttempts = 60; // 10 minutes with 10-second intervals
        var attempt = 0;

        while (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(10000, cancellationToken);

            var url = $"{_settings.ApiUrl}/JSON/spider/view/status/?scanId={scanId}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonConvert.DeserializeObject<ZapStatusResponse>(content);
                
                if (result?.Status == "100")
                {
                    _logger.LogInformation("Spider scan completed");
                    return;
                }
                
                _logger.LogDebug("Spider scan progress: {Progress}%", result?.Status);
            }

            attempt++;
        }
        
        _logger.LogWarning("Spider scan timeout or cancelled");
    }

    private async Task ActiveScanAsync(string targetUrl, CancellationToken cancellationToken)
    {
        var url = $"{_settings.ApiUrl}/JSON/ascan/action/scan/?url={Uri.EscapeDataString(targetUrl)}";
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            url += $"&apikey={_settings.ApiKey}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonConvert.DeserializeObject<ZapApiResponse>(content);
        var scanId = result?.Scan;

        if (!string.IsNullOrEmpty(scanId))
        {
            await WaitForActiveScanCompletionAsync(scanId, cancellationToken);
        }
    }

    private async Task WaitForActiveScanCompletionAsync(string scanId, CancellationToken cancellationToken)
    {
        var maxAttempts = 180; // 30 minutes with 10-second intervals
        var attempt = 0;

        while (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(10000, cancellationToken);

            var url = $"{_settings.ApiUrl}/JSON/ascan/view/status/?scanId={scanId}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonConvert.DeserializeObject<ZapStatusResponse>(content);
                
                if (result?.Status == "100")
                {
                    _logger.LogInformation("Active scan completed");
                    return;
                }
                
                _logger.LogDebug("Active scan progress: {Progress}%", result?.Status);
            }

            attempt++;
        }
        
        _logger.LogWarning("Active scan timeout or cancelled");
    }

    private async Task<List<ZapAlert>> GetScanResultsAsync(CancellationToken cancellationToken)
    {
        var url = $"{_settings.ApiUrl}/JSON/core/view/alerts/";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonConvert.DeserializeObject<ZapAlertsResponse>(content);
        
        return result?.Alerts ?? new List<ZapAlert>();
    }

    private List<VulnerabilityReport> ConvertZapVulnerabilities(List<ZapAlert> zapAlerts, string url)
    {
        var vulnerabilities = new List<VulnerabilityReport>();

        foreach (var alert in zapAlerts)
        {
            var severity = ConvertZapRiskToSeverity(alert.Risk);
            
            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = $"ZAP-{alert.AlertRef ?? alert.PluginId}",
                Name = alert.Alert ?? "Unknown Vulnerability",
                Severity = severity,
                Category = alert.Category ?? "Security",
                Scanner = ServiceName,
                Url = alert.Url ?? url,
                Description = alert.Description ?? "No description available",
                Solution = alert.Solution ?? "No solution provided",
                Evidence = alert.Evidence,
                AdditionalData = new Dictionary<string, object>
                {
                    ["confidence"] = alert.Confidence ?? "Unknown",
                    ["pluginId"] = alert.PluginId ?? "Unknown",
                    ["cweid"] = alert.CweId ?? "Unknown",
                    ["wascid"] = alert.WascId ?? "Unknown",
                    ["reference"] = alert.Reference ?? "No reference"
                },
                DiscoveredAt = DateTime.UtcNow
            });
        }

        return vulnerabilities;
    }

    private static SeverityLevel ConvertZapRiskToSeverity(string? risk)
    {
        return risk?.ToUpperInvariant() switch
        {
            "HIGH" => SeverityLevel.High,
            "MEDIUM" => SeverityLevel.Medium,
            "LOW" => SeverityLevel.Low,
            "INFORMATIONAL" => SeverityLevel.Info,
            _ => SeverityLevel.Info
        };
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        return await IsZapAvailableAsync(CancellationToken.None);
    }

    public async Task<string> GetServiceVersionAsync()
    {
        try
        {
            var url = $"{_settings.ApiUrl}/JSON/core/view/version/";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(content);
                return result?.version?.ToString() ?? "OWASP ZAP";
            }
        }
        catch
        {
            // Ignore errors for version check
        }
        return "OWASP ZAP";
    }
}

// ZAP API Response Models
public class ZapApiResponse
{
    public string? Scan { get; set; }
}

public class ZapStatusResponse
{
    public string? Status { get; set; }
}

public class ZapAlertsResponse
{
    public List<ZapAlert>? Alerts { get; set; }
}

public class ZapAlert
{
    public string? Alert { get; set; }
    public string? AlertRef { get; set; }
    public string? PluginId { get; set; }
    public string? CweId { get; set; }
    public string? WascId { get; set; }
    public string? Risk { get; set; }
    public string? Confidence { get; set; }
    public string? Description { get; set; }
    public string? Solution { get; set; }
    public string? Reference { get; set; }
    public string? Evidence { get; set; }
    public string? Url { get; set; }
    public string? Category { get; set; }
}