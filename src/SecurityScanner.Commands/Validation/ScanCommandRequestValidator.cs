using FluentValidation;
using SecurityScanner.Commands.Models;
using SecurityScanner.Core.Models;

namespace SecurityScanner.Commands.Validation;

public class ScanCommandRequestValidator : AbstractValidator<ScanCommandRequest>
{
    public ScanCommandRequestValidator()
    {
        RuleFor(x => x.Domains)
            .Must((request, domains) => domains.Count > 0 || request.ScanAll || !string.IsNullOrEmpty(request.InputFile))
            .WithMessage("At least one domain must be specified, --all flag must be used, or --file option must be provided");

        RuleForEach(x => x.Domains)
            .Must(BeValidDomain)
            .WithMessage("Domain '{PropertyValue}' is not valid");

        RuleFor(x => x.Tools)
            .NotEmpty()
            .WithMessage("At least one scanner tool must be specified");

        RuleFor(x => x.MaxConcurrent)
            .GreaterThan(0)
            .LessThanOrEqualTo(50)
            .WithMessage("Max concurrent scans must be between 1 and 50");

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(3600)
            .WithMessage("Timeout must be between 1 and 3600 seconds");

        RuleFor(x => x.LoadTestRps)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .WithMessage("Load test RPS must be between 1 and 1000")
            .When(x => x.Tools.Contains(ScannerType.LoadTest));

        RuleFor(x => x.LoadTestDuration)
            .GreaterThan(0)
            .LessThanOrEqualTo(3600)
            .WithMessage("Load test duration must be between 1 and 3600 seconds")
            .When(x => x.Tools.Contains(ScannerType.LoadTest));

        RuleFor(x => x.LoadTestRampUp)
            .GreaterThanOrEqualTo(0)
            .LessThan(x => x.LoadTestDuration)
            .WithMessage("Load test ramp-up must be non-negative and less than duration")
            .When(x => x.Tools.Contains(ScannerType.LoadTest));

        RuleFor(x => x.LoadTestMaxConcurrent)
            .GreaterThan(0)
            .LessThanOrEqualTo(10000)
            .WithMessage("Load test max concurrent must be between 1 and 10000")
            .When(x => x.Tools.Contains(ScannerType.LoadTest));

        RuleFor(x => x.OutputFile)
            .Must(BeValidFilePath)
            .WithMessage("Output file path is not valid")
            .When(x => !string.IsNullOrEmpty(x.OutputFile));
    }

    private static bool BeValidDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return false;

        // Remove protocol if present
        domain = domain.Replace("https://", "").Replace("http://", "");
        
        // Remove trailing slash
        domain = domain.TrimEnd('/');

        // Basic domain validation
        if (domain.Length > 253)
            return false;

        // Check for invalid characters
        if (domain.Contains(' ') || domain.Contains('\t') || domain.Contains('\n'))
            return false;

        // Must contain at least one dot (unless it's localhost)
        if (!domain.Contains('.') && domain != "localhost")
            return false;

        // Check each part of the domain
        var parts = domain.Split('.');
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part) || part.Length > 63)
                return false;

            // Must not start or end with hyphen
            if (part.StartsWith('-') || part.EndsWith('-'))
                return false;
        }

        return true;
    }

    private static bool BeValidFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return true;

        try
        {
            var invalidChars = Path.GetInvalidPathChars();
            return !filePath.Any(c => invalidChars.Contains(c));
        }
        catch
        {
            return false;
        }
    }
}