using Microsoft.Extensions.Logging;
using SecurityScanner.Commands.Models;
using SecurityScanner.Core.Exceptions;

namespace SecurityScanner.Commands.Middleware;

public interface IErrorHandlingMiddleware
{
    Task<T> HandleErrorsAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default) 
        where T : CommandResponse;
}

public class ErrorHandlingMiddleware : IErrorHandlingMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task<T> HandleErrorsAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default) 
        where T : CommandResponse
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            return HandleException<T>(ex);
        }
    }

    private T HandleException<T>(Exception ex) where T : CommandResponse
    {
        var response = Activator.CreateInstance<T>();
        response.Success = false;
        response.Timestamp = DateTime.UtcNow;

        switch (ex)
        {
            case TaskCanceledException when ex.InnerException is TimeoutException:
                response.ErrorMessage = "Operation timed out";
                _logger.LogWarning("Operation timed out: {Message}", ex.Message);
                break;

            case OperationCanceledException:
                response.ErrorMessage = "Operation was cancelled by user or timeout";
                _logger.LogWarning("Operation cancelled: {Message}", ex.Message);
                break;

            case ArgumentNullException nullEx:
                response.ErrorMessage = $"Required parameter is missing: {nullEx.ParamName}";
                _logger.LogWarning("Required parameter missing: {ParamName}", nullEx.ParamName);
                break;

            case ArgumentException argEx:
                response.ErrorMessage = $"Invalid argument: {argEx.Message}";
                _logger.LogWarning("Invalid argument provided: {Message}", argEx.Message);
                break;

            case ScannerException scanEx:
                response.ErrorMessage = $"Scanner error: {scanEx.Message}";
                _logger.LogError(scanEx, "Scanner execution failed");
                break;

            case ConfigurationException configEx:
                response.ErrorMessage = $"Configuration error: {configEx.Message}";
                _logger.LogError(configEx, "Configuration validation failed");
                break;

            case FileNotFoundException fileEx:
                response.ErrorMessage = $"File not found: {fileEx.FileName}";
                _logger.LogError("Required file not found: {FileName}", fileEx.FileName);
                break;

            case DirectoryNotFoundException dirEx:
                response.ErrorMessage = $"Directory not found: {dirEx.Message}";
                _logger.LogError("Required directory not found: {Message}", dirEx.Message);
                break;

            case UnauthorizedAccessException accessEx:
                response.ErrorMessage = $"Access denied: {accessEx.Message}";
                _logger.LogError("Access denied to required resource: {Message}", accessEx.Message);
                break;

            case HttpRequestException httpEx:
                response.ErrorMessage = $"Network error: {httpEx.Message}";
                _logger.LogError(httpEx, "HTTP request failed");
                break;

            case TimeoutException timeoutEx:
                response.ErrorMessage = $"Operation timed out: {timeoutEx.Message}";
                _logger.LogWarning("Operation timed out: {Message}", timeoutEx.Message);
                break;

            case NotSupportedException notSupportedEx:
                response.ErrorMessage = $"Operation not supported: {notSupportedEx.Message}";
                _logger.LogError("Unsupported operation attempted: {Message}", notSupportedEx.Message);
                break;

            case InvalidOperationException invalidOpEx:
                response.ErrorMessage = $"Invalid operation: {invalidOpEx.Message}";
                _logger.LogError("Invalid operation attempted: {Message}", invalidOpEx.Message);
                break;

            case OutOfMemoryException:
                response.ErrorMessage = "Insufficient memory to complete operation";
                _logger.LogCritical("Out of memory exception occurred");
                break;

            case Exception generalEx:
                response.ErrorMessage = "An unexpected error occurred. Please check logs for details.";
                _logger.LogError(generalEx, "Unhandled exception occurred: {Message}", generalEx.Message);
                break;
        }

        return response;
    }
}

public static class ErrorHandlingExtensions
{
    public static string GetUserFriendlyMessage(this Exception ex)
    {
        return ex switch
        {
            ArgumentNullException => "Required parameter is missing",
            ArgumentException => "Invalid input provided",
            FileNotFoundException => "Required file could not be found",
            DirectoryNotFoundException => "Required directory could not be found",
            UnauthorizedAccessException => "Access denied to required resource",
            HttpRequestException => "Network connection failed",
            TimeoutException => "Operation timed out",
            ScannerException => "Scanner execution failed",
            ConfigurationException => "Configuration is invalid",
            OperationCanceledException => "Operation was cancelled",
            NotSupportedException => "Operation is not supported",
            InvalidOperationException => "Invalid operation attempted",
            OutOfMemoryException => "Insufficient memory available",
            _ => "An unexpected error occurred"
        };
    }

    public static bool IsRetryable(this Exception ex)
    {
        return ex switch
        {
            HttpRequestException => true,
            TimeoutException => true,
            TaskCanceledException when ex.InnerException is TimeoutException => true,
            _ => false
        };
    }
}