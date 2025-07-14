using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecurityScanner.Commands.Handlers;
using SecurityScanner.Commands.Middleware;
using SecurityScanner.Core.Interfaces;
using SecurityScanner.Infrastructure.Configuration;
using SecurityScanner.Infrastructure.Http;
using SecurityScanner.Infrastructure.Persistence;
using SecurityScanner.Services.Orchestration;
using SecurityScanner.Services.Scanners;

namespace SecurityScanner.App.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecurityScannerServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register configuration options
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        services.Configure<ScannerSettings>(configuration.GetSection("ScannerSettings"));
        services.Configure<PerformanceSettings>(configuration.GetSection("PerformanceSettings"));
        services.Configure<ReportSettings>(configuration.GetSection("ReportSettings"));

        // Register infrastructure services
        services.AddSingleton<IFileStorage, FileStorage>();
        services.AddSingleton<SecurityScannerHttpClientFactory>();

        // Register scanner services
        services.AddTransient<IScannerService, SecurityHeadersService>();
        // TODO: Add other scanner services when implemented
        // services.AddTransient<IScannerService, SslLabsService>();
        // services.AddTransient<IScannerService, LoadTestService>();
        // services.AddTransient<IScannerService, OwaspZapService>();

        // Register orchestration services
        services.AddTransient<IScanOrchestrator, ScanOrchestrator>();

        // Register command services
        services.AddTransient<IValidationMiddleware, ValidationMiddleware>();
        services.AddTransient<ILoggingMiddleware, LoggingMiddleware>();
        services.AddTransient<IErrorHandlingMiddleware, ErrorHandlingMiddleware>();
        services.AddTransient<IScanCommandHandler, ScanCommandHandler>();

        // Register HTTP client factory
        services.AddHttpClient();
        services.AddSecurityScannerHttpClients(configuration);

        return services;
    }

    private static IServiceCollection AddSecurityScannerHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var performanceSettings = configuration.GetSection("PerformanceSettings").Get<PerformanceSettings>() 
                                ?? new PerformanceSettings();

        // Security Headers HTTP client
        services.AddHttpClient("SecurityHeaders-RateLimited", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "SecurityScanner/1.0");
            client.MaxResponseContentBufferSize = 1024 * 1024; // 1MB limit
        });

        // SSL Labs HTTP client
        services.AddHttpClient("SslLabs-RateLimited", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Add("User-Agent", "SecurityScanner/1.0");
        });

        // OWASP ZAP HTTP client
        services.AddHttpClient("OwaspZap", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(30);
            client.DefaultRequestHeaders.Add("User-Agent", "SecurityScanner/1.0");
        });

        // Load testing HTTP client
        services.AddHttpClient("LoadTest", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "SecurityScanner-LoadTest/1.0");
        });

        return services;
    }
}