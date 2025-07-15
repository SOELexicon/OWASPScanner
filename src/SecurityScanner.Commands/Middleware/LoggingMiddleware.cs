using Microsoft.Extensions.Logging;
using SecurityScanner.Commands.Models;
using SecurityScanner.Core.Models;
using System.Diagnostics;

namespace SecurityScanner.Commands.Middleware;

public interface ILoggingMiddleware
{
    Task<T> ExecuteWithLoggingAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        CommandRequest? request = null,
        CancellationToken cancellationToken = default) where T : CommandResponse;
}

public class LoggingMiddleware : ILoggingMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteWithLoggingAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        CommandRequest? request = null,
        CancellationToken cancellationToken = default) where T : CommandResponse
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        try
        {
            _logger.LogInformation("Starting operation: {OperationName} [CorrelationId: {CorrelationId}]",
                operationName, correlationId);

            if (request != null)
            {
                LogRequestDetails(request, correlationId);
            }

            var result = await operation();

            stopwatch.Stop();
            _logger.LogInformation("Completed operation: {OperationName} [CorrelationId: {CorrelationId}] - Duration: {Duration}ms - Success: {Success}",
                operationName, correlationId, stopwatch.ElapsedMilliseconds, result.Success);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Operation cancelled: {OperationName} [CorrelationId: {CorrelationId}] - Duration: {Duration}ms",
                operationName, correlationId, stopwatch.ElapsedMilliseconds);
            
            var response = CreateErrorResponse<T>("Operation was cancelled", correlationId);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Operation failed: {OperationName} [CorrelationId: {CorrelationId}] - Duration: {Duration}ms - Error: {ErrorMessage}",
                operationName, correlationId, stopwatch.ElapsedMilliseconds, ex.Message);

            var response = CreateErrorResponse<T>($"Operation failed: {ex.Message}", correlationId);
            return response;
        }
    }

    private void LogRequestDetails(CommandRequest request, string correlationId)
    {
        var requestType = request.GetType().Name;
        
        _logger.LogDebug("Request details for {RequestType} [CorrelationId: {CorrelationId}] - JsonOutput: {JsonOutput}, Verbose: {Verbose}",
            requestType, correlationId, request.JsonOutput, request.Verbose);

        switch (request)
        {
            case ScanCommandRequest scanRequest:
                _logger.LogDebug("Scan request details [CorrelationId: {CorrelationId}] - Domains: {DomainCount}, Tools: {Tools}, MaxConcurrent: {MaxConcurrent}",
                    correlationId, scanRequest.Domains.Count, string.Join(",", scanRequest.Tools), scanRequest.MaxConcurrent);
                break;
                
            case ReportCommandRequest reportRequest:
                _logger.LogDebug("Report request details [CorrelationId: {CorrelationId}] - InputFile: {InputFile}, Format: {Format}",
                    correlationId, reportRequest.InputFile, reportRequest.Format);
                break;
                
            case ConfigCommandRequest configRequest:
                _logger.LogDebug("Config request details [CorrelationId: {CorrelationId}] - Action: {Action}, ConfigPath: {ConfigPath}",
                    correlationId, configRequest.Action, configRequest.ConfigPath ?? "default");
                break;
                
            case HealthCommandRequest healthRequest:
                var scanners = healthRequest.SpecificScanners?.Count > 0 
                    ? string.Join(",", healthRequest.SpecificScanners) 
                    : "all";
                _logger.LogDebug("Health request details [CorrelationId: {CorrelationId}] - Scanners: {Scanners}",
                    correlationId, scanners);
                break;
        }
    }

    private static T CreateErrorResponse<T>(string errorMessage, string correlationId) where T : CommandResponse
    {
        var response = Activator.CreateInstance<T>();
        response.Success = false;
        response.ErrorMessage = errorMessage;
        response.Timestamp = DateTime.UtcNow;
        return response;
    }
}