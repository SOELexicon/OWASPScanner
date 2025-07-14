using FluentValidation;
using SecurityScanner.Commands.Models;

namespace SecurityScanner.Commands.Validation;

public class ReportCommandRequestValidator : AbstractValidator<ReportCommandRequest>
{
    private static readonly string[] ValidFormats = { "json", "html", "csv" };

    public ReportCommandRequestValidator()
    {
        RuleFor(x => x.InputFile)
            .NotEmpty()
            .WithMessage("Input file path is required")
            .Must(BeValidFilePath)
            .WithMessage("Input file path is not valid")
            .Must(FileExists)
            .WithMessage("Input file does not exist");

        RuleFor(x => x.Format)
            .NotEmpty()
            .WithMessage("Report format is required")
            .Must(format => ValidFormats.Contains(format.ToLowerInvariant()))
            .WithMessage($"Report format must be one of: {string.Join(", ", ValidFormats)}");

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

    private static bool FileExists(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        return File.Exists(filePath);
    }
}