using FluentValidation;
using SecurityScanner.Commands.Models;

namespace SecurityScanner.Commands.Validation;

public class ConfigCommandRequestValidator : AbstractValidator<ConfigCommandRequest>
{
    private static readonly string[] ValidActions = { "list", "validate", "test" };

    public ConfigCommandRequestValidator()
    {
        RuleFor(x => x.Action)
            .NotEmpty()
            .WithMessage("Config action is required")
            .Must(action => ValidActions.Contains(action.ToLowerInvariant()))
            .WithMessage($"Config action must be one of: {string.Join(", ", ValidActions)}");

        RuleFor(x => x.ConfigPath)
            .Must(BeValidFilePath)
            .WithMessage("Config file path is not valid")
            .When(x => !string.IsNullOrEmpty(x.ConfigPath))
            .Must(FileExists)
            .WithMessage("Config file does not exist")
            .When(x => !string.IsNullOrEmpty(x.ConfigPath) && (x.Action == "validate" || x.Action == "test"));

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

    private static bool FileExists(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        return File.Exists(filePath);
    }
}