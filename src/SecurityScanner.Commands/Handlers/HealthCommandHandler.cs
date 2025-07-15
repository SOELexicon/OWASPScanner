using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecurityScanner.Commands.Models;
using SecurityScanner.Core.Interfaces;
using SecurityScanner.Core.Models;
using System.Diagnostics;

namespace SecurityScanner.Commands.Handlers;

public interface IHealthCommandHandler : ICommandHandler<HealthCommandRequest, HealthCommandResponse>
{
}

public class HealthCommandHandler : IHealthCommandHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HealthCommandHandler> _logger;

    public HealthCommandHandler(
        IServiceProvider serviceProvider,
        ILogger<HealthCommandHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCommandResponse> HandleAsync(HealthCommandRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting health check for all scanner services");

        var response = new HealthCommandResponse
        {
            Success = true,
            ScannerStatuses = new List<ScannerHealthStatus>()
        };

        try
        {
            // Get all registered scanner services
            var scannerServices = _serviceProvider.GetServices<IScannerService>();
            var healthCheckTasks = new List<Task<ScannerHealthStatus>>();

            foreach (var scanner in scannerServices)
            {
                healthCheckTasks.Add(CheckScannerHealthAsync(scanner, cancellationToken));
            }

            // Wait for all health checks to complete
            var healthStatuses = await Task.WhenAll(healthCheckTasks);
            response.ScannerStatuses.AddRange(healthStatuses);

            // Determine overall health
            response.AllScannersAvailable = response.ScannerStatuses.All(s => s.IsAvailable);

            if (!response.AllScannersAvailable)
            {
                var unavailableCount = response.ScannerStatuses.Count(s => !s.IsAvailable);
                _logger.LogWarning("Health check completed - {UnavailableCount} out of {TotalCount} scanners are unavailable",
                    unavailableCount, response.ScannerStatuses.Count);
            }
            else
            {
                _logger.LogInformation("Health check completed - All {TotalCount} scanners are available",
                    response.ScannerStatuses.Count);
            }
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.ErrorMessage = $"Health check failed: {ex.Message}";
            _logger.LogError(ex, "Health check failed");
        }

        return response;
    }

    private async Task<ScannerHealthStatus> CheckScannerHealthAsync(IScannerService scanner, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var status = new ScannerHealthStatus
        {
            ScannerName = scanner.ServiceName,
            ScannerType = scanner.ScannerType
        };

        try
        {
            _logger.LogDebug("Checking health for scanner: {ScannerName}", scanner.ServiceName);

            // Check if service is available
            status.IsAvailable = await scanner.IsServiceAvailableAsync();

            // Get service version if available
            if (status.IsAvailable)
            {
                try
                {
                    status.Version = await scanner.GetServiceVersionAsync();
                }
                catch (Exception versionEx)
                {
                    _logger.LogDebug(versionEx, "Could not retrieve version for scanner: {ScannerName}", scanner.ServiceName);
                    status.Version = "Unknown";
                }
            }
            else
            {
                status.ErrorMessage = "Service is not available";
                _logger.LogWarning("Scanner {ScannerName} is not available", scanner.ServiceName);
            }
        }
        catch (Exception ex)
        {
            status.IsAvailable = false;
            status.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Health check failed for scanner: {ScannerName}", scanner.ServiceName);
        }
        finally
        {
            stopwatch.Stop();
            status.ResponseTime = stopwatch.Elapsed;
        }

        return status;
    }
}

public class HealthCommandRequest
{
    public bool Verbose { get; set; }
    public List<ScannerType> SpecificScanners { get; set; } = new();
}