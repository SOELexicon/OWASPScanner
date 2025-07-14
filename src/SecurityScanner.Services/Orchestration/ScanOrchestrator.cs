using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityScanner.Core.Interfaces;
using SecurityScanner.Core.Models;
using SecurityScanner.Infrastructure.Configuration;

namespace SecurityScanner.Services.Orchestration;

public interface IScanOrchestrator
{
    Task<IEnumerable<ScanResult>> ExecuteScansAsync(ScanConfiguration configuration, CancellationToken cancellationToken = default);
    Task<ScanResult> ExecuteSingleScanAsync(DomainConfig domain, ScannerType scannerType, CancellationToken cancellationToken = default);
}

public class ScanOrchestrator : IScanOrchestrator
{
    private readonly IEnumerable<IScannerService> _scanners;
    private readonly ILogger<ScanOrchestrator> _logger;
    private readonly PerformanceSettings _performanceSettings;

    public ScanOrchestrator(
        IEnumerable<IScannerService> scanners,
        ILogger<ScanOrchestrator> logger,
        IOptions<PerformanceSettings> performanceSettings)
    {
        _scanners = scanners;
        _logger = logger;
        _performanceSettings = performanceSettings.Value;
    }

    public async Task<IEnumerable<ScanResult>> ExecuteScansAsync(ScanConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scan orchestration for {DomainCount} domains with {ScannerCount} scanners", 
            configuration.Domains.Count, configuration.EnabledScanners.Count);

        var allResults = new List<ScanResult>();

        if (configuration.ParallelExecution)
        {
            allResults.AddRange(await ExecuteParallelScansAsync(configuration, cancellationToken).ConfigureAwait(false));
        }
        else
        {
            allResults.AddRange(await ExecuteSequentialScansAsync(configuration, cancellationToken).ConfigureAwait(false));
        }

        _logger.LogInformation("Scan orchestration completed. Total results: {ResultCount}", allResults.Count);

        return allResults;
    }

    public async Task<ScanResult> ExecuteSingleScanAsync(DomainConfig domain, ScannerType scannerType, CancellationToken cancellationToken = default)
    {
        var scanner = _scanners.FirstOrDefault(s => s.ScannerType == scannerType);
        
        if (scanner == null)
        {
            _logger.LogWarning("Scanner type {ScannerType} not found", scannerType);
            return new ScanResult
            {
                Domain = domain.Name,
                Url = domain.Url,
                Scanner = scannerType.ToString(),
                Status = ScanStatus.Failed,
                ErrorMessage = $"Scanner {scannerType} not available",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow
            };
        }

        return await ExecuteScanWithTimeoutAsync(scanner, domain, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IEnumerable<ScanResult>> ExecuteParallelScansAsync(ScanConfiguration configuration, CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(configuration.MaxConcurrentScans, configuration.MaxConcurrentScans);
        var tasks = new List<Task<IEnumerable<ScanResult>>>();

        foreach (var domain in configuration.Domains)
        {
            var domainTask = ExecuteDomainScansAsync(domain, configuration.EnabledScanners, semaphore, cancellationToken);
            tasks.Add(domainTask);
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.SelectMany(r => r);
    }

    private async Task<IEnumerable<ScanResult>> ExecuteSequentialScansAsync(ScanConfiguration configuration, CancellationToken cancellationToken)
    {
        var results = new List<ScanResult>();

        foreach (var domain in configuration.Domains)
        {
            var domainResults = await ExecuteDomainScansSequentiallyAsync(domain, configuration.EnabledScanners, cancellationToken).ConfigureAwait(false);
            results.AddRange(domainResults);
        }

        return results;
    }

    private async Task<IEnumerable<ScanResult>> ExecuteDomainScansAsync(
        DomainConfig domain, 
        List<ScannerType> enabledScanners, 
        SemaphoreSlim semaphore, 
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            var tasks = new List<Task<ScanResult>>();
            
            foreach (var scannerType in enabledScanners)
            {
                var scanner = _scanners.FirstOrDefault(s => s.ScannerType == scannerType);
                if (scanner != null)
                {
                    tasks.Add(ExecuteScanWithTimeoutAsync(scanner, domain, cancellationToken));
                }
                else
                {
                    _logger.LogWarning("Scanner {ScannerType} not found for domain {Domain}", scannerType, domain.Name);
                }
            }

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return results;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<IEnumerable<ScanResult>> ExecuteDomainScansSequentiallyAsync(
        DomainConfig domain, 
        List<ScannerType> enabledScanners, 
        CancellationToken cancellationToken)
    {
        var results = new List<ScanResult>();

        foreach (var scannerType in enabledScanners)
        {
            var scanner = _scanners.FirstOrDefault(s => s.ScannerType == scannerType);
            if (scanner != null)
            {
                var result = await ExecuteScanWithTimeoutAsync(scanner, domain, cancellationToken).ConfigureAwait(false);
                results.Add(result);
            }
            else
            {
                _logger.LogWarning("Scanner {ScannerType} not found for domain {Domain}", scannerType, domain.Name);
            }
        }

        return results;
    }

    private async Task<ScanResult> ExecuteScanWithTimeoutAsync(IScannerService scanner, DomainConfig domain, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_performanceSettings.DefaultTimeout);

        try
        {
            _logger.LogDebug("Starting {ScannerType} scan for {Domain}", scanner.ScannerType, domain.Name);
            
            var result = await scanner.ScanAsync(domain, timeoutCts.Token).ConfigureAwait(false);
            
            _logger.LogDebug("Completed {ScannerType} scan for {Domain} with status {Status}", 
                scanner.ScannerType, domain.Name, result.Status);
            
            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Scanner {ScannerType} timed out for domain {Domain}", scanner.ScannerType, domain.Name);
            
            return new ScanResult
            {
                Domain = domain.Name,
                Url = domain.Url,
                Scanner = scanner.ServiceName,
                Status = ScanStatus.Timeout,
                ErrorMessage = $"Scanner timed out after {_performanceSettings.DefaultTimeout.TotalSeconds} seconds",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scanner {ScannerType} failed for domain {Domain}", scanner.ScannerType, domain.Name);
            
            return new ScanResult
            {
                Domain = domain.Name,
                Url = domain.Url,
                Scanner = scanner.ServiceName,
                Status = ScanStatus.Failed,
                ErrorMessage = ex.Message,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow
            };
        }
    }

    public async Task<bool> CheckScannerAvailabilityAsync()
    {
        var availabilityTasks = _scanners.Select(async scanner =>
        {
            try
            {
                var isAvailable = await scanner.IsServiceAvailableAsync().ConfigureAwait(false);
                _logger.LogInformation("Scanner {ScannerName} availability: {IsAvailable}", scanner.ServiceName, isAvailable);
                return new { Scanner = scanner.ServiceName, IsAvailable = isAvailable };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check availability for scanner {ScannerName}", scanner.ServiceName);
                return new { Scanner = scanner.ServiceName, IsAvailable = false };
            }
        });

        var results = await Task.WhenAll(availabilityTasks).ConfigureAwait(false);
        
        var unavailableScanners = results.Where(r => !r.IsAvailable).ToList();
        if (unavailableScanners.Any())
        {
            _logger.LogWarning("Unavailable scanners: {UnavailableScanners}", 
                string.Join(", ", unavailableScanners.Select(s => s.Scanner)));
        }

        return results.All(r => r.IsAvailable);
    }
}