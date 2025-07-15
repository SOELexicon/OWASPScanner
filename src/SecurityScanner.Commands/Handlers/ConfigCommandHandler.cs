using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SecurityScanner.Commands.Models;
using SecurityScanner.Core.Interfaces;
using SecurityScanner.Core.Models;
using SecurityScanner.Infrastructure.Configuration;

namespace SecurityScanner.Commands.Handlers;

public interface IConfigCommandHandler : ICommandHandler<ConfigCommandRequest, ConfigCommandResponse>
{
}

public class ConfigCommandHandler : IConfigCommandHandler
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigCommandHandler> _logger;
    private readonly IOptionsMonitor<AppSettings> _appSettings;
    private readonly IOptionsMonitor<ScannerSettings> _scannerSettings;

    public ConfigCommandHandler(
        IConfiguration configuration,
        ILogger<ConfigCommandHandler> logger,
        IOptionsMonitor<AppSettings> appSettings,
        IOptionsMonitor<ScannerSettings> scannerSettings)
    {
        _configuration = configuration;
        _logger = logger;
        _appSettings = appSettings;
        _scannerSettings = scannerSettings;
    }

    public async Task<ConfigCommandResponse> HandleAsync(ConfigCommandRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing config command - Action: {Action}", request.Action);

        var response = new ConfigCommandResponse
        {
            Success = true,
            ValidationErrors = new List<string>()
        };

        try
        {
            switch (request.Action.ToLowerInvariant())
            {
                case "show":
                    response.ConfigData = await ShowConfigurationAsync(request);
                    break;
                case "validate":
                    response.ValidationErrors = await ValidateConfigurationAsync(request);
                    response.Success = !response.ValidationErrors.Any();
                    break;
                case "set":
                    await SetConfigurationAsync(request);
                    response.ConfigData = "Configuration updated successfully";
                    break;
                case "reset":
                    await ResetConfigurationAsync(request);
                    response.ConfigData = "Configuration reset to defaults";
                    break;
                default:
                    response.Success = false;
                    response.ErrorMessage = $"Unknown action: {request.Action}. Valid actions: show, validate, set, reset";
                    break;
            }
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.ErrorMessage = $"Configuration operation failed: {ex.Message}";
            _logger.LogError(ex, "Configuration operation failed");
        }

        return response;
    }

    private async Task<string> ShowConfigurationAsync(ConfigCommandRequest request)
    {
        var config = new
        {
            AppSettings = _appSettings.CurrentValue,
            ScannerSettings = _scannerSettings.CurrentValue,
            ConfigSources = GetConfigurationSources()
        };

        var jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        if (!string.IsNullOrEmpty(request.Section))
        {
            // Show specific section
            var sectionData = _configuration.GetSection(request.Section);
            var sectionDict = new Dictionary<string, string?>();
            
            foreach (var child in sectionData.GetChildren())
            {
                sectionDict[child.Key] = child.Value;
            }

            return await Task.FromResult(JsonConvert.SerializeObject(sectionDict, jsonSettings));
        }

        return await Task.FromResult(JsonConvert.SerializeObject(config, jsonSettings));
    }

    private async Task<List<string>> ValidateConfigurationAsync(ConfigCommandRequest request)
    {
        var errors = new List<string>();

        // Validate AppSettings
        var appSettings = _appSettings.CurrentValue;
        if (appSettings == null)
        {
            errors.Add("AppSettings configuration is null");
        }

        // Validate ScannerSettings
        var scannerSettings = _scannerSettings.CurrentValue;
        if (scannerSettings == null)
        {
            errors.Add("ScannerSettings configuration is null");
        }
        else
        {
            // Validate individual scanner settings
            if (scannerSettings.SecurityHeaders != null)
            {
                if (scannerSettings.SecurityHeaders.RequestTimeout <= TimeSpan.Zero)
                {
                    errors.Add("SecurityHeaders.RequestTimeout must be greater than 0");
                }
            }

            if (scannerSettings.SslLabs != null)
            {
                if (string.IsNullOrEmpty(scannerSettings.SslLabs.ApiUrl))
                {
                    errors.Add("SslLabs.ApiUrl is required");
                }
                
                if (!Uri.TryCreate(scannerSettings.SslLabs.ApiUrl, UriKind.Absolute, out _))
                {
                    errors.Add("SslLabs.ApiUrl must be a valid URL");
                }
            }

            if (scannerSettings.OwaspZap != null)
            {
                if (string.IsNullOrEmpty(scannerSettings.OwaspZap.ApiUrl))
                {
                    errors.Add("OwaspZap.ApiUrl is required");
                }
                
                if (!Uri.TryCreate(scannerSettings.OwaspZap.ApiUrl, UriKind.Absolute, out _))
                {
                    errors.Add("OwaspZap.ApiUrl must be a valid URL");
                }
            }

            if (scannerSettings.LoadTest != null)
            {
                if (scannerSettings.LoadTest.RequestsPerSecond <= 0)
                {
                    errors.Add("LoadTest.RequestsPerSecond must be greater than 0");
                }
                
                if (scannerSettings.LoadTest.DurationSeconds <= 0)
                {
                    errors.Add("LoadTest.DurationSeconds must be greater than 0");
                }
                
                if (scannerSettings.LoadTest.MaxConcurrentRequests <= 0)
                {
                    errors.Add("LoadTest.MaxConcurrentRequests must be greater than 0");
                }
            }
        }

        return await Task.FromResult(errors);
    }

    private async Task SetConfigurationAsync(ConfigCommandRequest request)
    {
        if (string.IsNullOrEmpty(request.Key) || string.IsNullOrEmpty(request.Value))
        {
            throw new ArgumentException("Both Key and Value are required for set operation");
        }

        // For this implementation, we'll just log the operation
        // In a real application, you might want to update a configuration file
        _logger.LogInformation("Configuration set request: {Key} = {Value}", request.Key, request.Value);
        
        await Task.CompletedTask;
        throw new NotImplementedException("Configuration setting is not implemented in this version. Use appsettings.json or environment variables.");
    }

    private async Task ResetConfigurationAsync(ConfigCommandRequest request)
    {
        // For this implementation, we'll just log the operation
        _logger.LogInformation("Configuration reset request for section: {Section}", request.Section ?? "all");
        
        await Task.CompletedTask;
        throw new NotImplementedException("Configuration reset is not implemented in this version. Manually edit appsettings.json or restart the application.");
    }

    private List<string> GetConfigurationSources()
    {
        var sources = new List<string>();
        
        if (_configuration is IConfigurationRoot configRoot)
        {
            foreach (var provider in configRoot.Providers)
            {
                sources.Add(provider.ToString() ?? "Unknown provider");
            }
        }

        return sources;
    }
}

public class ConfigCommandRequest
{
    public string Action { get; set; } = "show"; // show, validate, set, reset
    public string? Section { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
    public bool Verbose { get; set; }
}