using FluentValidation;
using SecurityScanner.Commands.Models;
using SecurityScanner.Core.Models;

namespace SecurityScanner.Commands.Validation;

public class HealthCommandRequestValidator : AbstractValidator<HealthCommandRequest>
{
    private static readonly ScannerType[] ValidScannerTypes = 
    {
        ScannerType.SecurityHeaders,
        ScannerType.SslLabs,
        ScannerType.OwaspZap,
        ScannerType.LoadTest,
        ScannerType.Nmap
    };

    public HealthCommandRequestValidator()
    {
        RuleFor(x => x.SpecificScanners)
            .Must(scanners => scanners == null || scanners.All(s => ValidScannerTypes.Contains(s)))
            .WithMessage($"Scanner types must be one of: {string.Join(", ", ValidScannerTypes)}")
            .When(x => x.SpecificScanners != null);

        RuleFor(x => x.OutputFile)
            .Must(BeValidFilePath)
            .WithMessage("Output file path is not valid")
            .When(x => !string.IsNullOrEmpty(x.OutputFile));
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