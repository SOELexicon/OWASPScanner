using FluentValidation;
using Microsoft.Extensions.Logging;
using SecurityScanner.Commands.Models;
using SecurityScanner.Commands.Validation;

namespace SecurityScanner.Commands.Middleware;

public interface IValidationMiddleware
{
    Task<ValidationResult> ValidateAsync<T>(T request, CancellationToken cancellationToken = default) 
        where T : CommandRequest;
}

public class ValidationMiddleware : IValidationMiddleware
{
    private readonly ILogger<ValidationMiddleware> _logger;
    private readonly Dictionary<Type, IValidator> _validators;

    public ValidationMiddleware(ILogger<ValidationMiddleware> logger)
    {
        _logger = logger;
        _validators = new Dictionary<Type, IValidator>
        {
            { typeof(ScanCommandRequest), new ScanCommandRequestValidator() },
            { typeof(ReportCommandRequest), new ReportCommandRequestValidator() },
            { typeof(ConfigCommandRequest), new ConfigCommandRequestValidator() },
            { typeof(HealthCommandRequest), new HealthCommandRequestValidator() }
        };
    }

    public async Task<ValidationResult> ValidateAsync<T>(T request, CancellationToken cancellationToken = default) 
        where T : CommandRequest
    {
        if (request == null)
        {
            _logger.LogWarning("Validation failed: Request is null");
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Request cannot be null" }
            };
        }

        var requestType = typeof(T);
        if (!_validators.TryGetValue(requestType, out var validator))
        {
            _logger.LogWarning("No validator found for request type: {RequestType}", requestType.Name);
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string> { $"No validator configured for request type: {requestType.Name}" }
            };
        }

        try
        {
            var validationContext = new ValidationContext<T>(request);
            var result = await validator.ValidateAsync(validationContext, cancellationToken);

            if (result.IsValid)
            {
                _logger.LogDebug("Validation passed for {RequestType}", requestType.Name);
                return new ValidationResult { IsValid = true };
            }

            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Validation failed for {RequestType}: {Errors}", 
                requestType.Name, string.Join(", ", errors));

            return new ValidationResult
            {
                IsValid = false,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during validation of {RequestType}", requestType.Name);
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "An error occurred during validation" }
            };
        }
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}