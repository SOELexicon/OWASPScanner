using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecurityScanner.Core.Exceptions;

namespace SecurityScanner.Infrastructure.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddSecurityScannerConfiguration(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var appSettings = new AppSettings();
        configuration.Bind(appSettings);
        
        ValidateConfiguration(appSettings);
        
        services.Configure<AppSettings>(configuration);
        services.Configure<ScannerSettings>(configuration.GetSection("ScannerSettings"));
        services.Configure<ReportSettings>(configuration.GetSection("ReportSettings"));
        services.Configure<NotificationSettings>(configuration.GetSection("NotificationSettings"));
        services.Configure<PerformanceSettings>(configuration.GetSection("PerformanceSettings"));
        services.Configure<LoggingSettings>(configuration.GetSection("LoggingSettings"));
        
        return services;
    }
    
    public static IConfigurationBuilder AddSecurityScannerConfiguration(
        this IConfigurationBuilder builder,
        string[] args)
    {
        return builder
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables("SCANNER_")
            .AddCommandLine(args);
    }
    
    private static void ValidateConfiguration(AppSettings settings)
    {
        if (settings.ScannerSettings.OwaspZap.ApiUrl == null)
        {
            throw new ConfigurationException("OWASP ZAP API URL is required", "ScannerSettings:OwaspZap:ApiUrl");
        }
        
        if (settings.PerformanceSettings.MaxConcurrentScans <= 0)
        {
            throw new ConfigurationException("MaxConcurrentScans must be greater than 0", "PerformanceSettings:MaxConcurrentScans");
        }
        
        if (settings.NotificationSettings.Email.Enabled && 
            string.IsNullOrEmpty(settings.NotificationSettings.Email.SmtpServer))
        {
            throw new ConfigurationException("SMTP server is required when email notifications are enabled", "NotificationSettings:Email:SmtpServer");
        }
    }
}