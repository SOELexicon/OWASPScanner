using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using SecurityScanner.Infrastructure.Configuration;

namespace SecurityScanner.Infrastructure.Http;

public class SecurityScannerHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SecurityScannerHttpClientFactory> _logger;
    private readonly PerformanceSettings _settings;

    public SecurityScannerHttpClientFactory(
        IHttpClientFactory httpClientFactory,
        ILogger<SecurityScannerHttpClientFactory> logger,
        IOptions<PerformanceSettings> settings)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    public HttpClient CreateClient(string name = "SecurityScanner")
    {
        return _httpClientFactory.CreateClient(name);
    }

    public HttpClient CreateRateLimitedClient(string name, TimeSpan? rateLimitDelay = null)
    {
        var client = _httpClientFactory.CreateClient($"{name}-RateLimited");
        var handler = new RateLimitingHandler(rateLimitDelay ?? TimeSpan.FromMilliseconds(100), _logger);
        
        return client;
    }
}

public static class HttpClientExtensions
{
    public static IServiceCollection AddSecurityScannerHttpClients(
        this IServiceCollection services,
        IOptions<PerformanceSettings> performanceSettings)
    {
        var settings = performanceSettings.Value;
        
        // Default HTTP client with retry policy
        services.AddHttpClient("SecurityScanner", client =>
        {
            client.Timeout = settings.DefaultTimeout;
            client.DefaultRequestHeaders.Add("User-Agent", "SecurityScanner/1.0");
        })
        .AddPolicyHandler(GetRetryPolicy(settings))
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Rate-limited HTTP client for SSL Labs
        services.AddHttpClient("SslLabs-RateLimited", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Add("User-Agent", "SecurityScanner/1.0");
        })
        .AddPolicyHandler(GetRetryPolicy(settings))
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Security Headers HTTP client
        services.AddHttpClient("SecurityHeaders-RateLimited", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "SecurityScanner/1.0");
            client.MaxResponseContentBufferSize = 1024 * 1024; // 1MB limit
        })
        .AddPolicyHandler(GetRetryPolicy(settings));

        // OWASP ZAP HTTP client
        services.AddHttpClient("OwaspZap", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(30);
            client.DefaultRequestHeaders.Add("User-Agent", "SecurityScanner/1.0");
        })
        .AddPolicyHandler(GetRetryPolicy(settings));

        // Load testing HTTP client
        services.AddHttpClient("LoadTest", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "SecurityScanner-LoadTest/1.0");
        });

        services.AddTransient<SecurityScannerHttpClientFactory>();
        
        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(PerformanceSettings settings)
    {
        if (!settings.EnableRetries)
        {
            return Policy.NoOpAsync<HttpResponseMessage>();
        }

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: settings.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + settings.RetryDelay,
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempt without context logger
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}