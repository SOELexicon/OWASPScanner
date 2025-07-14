using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurityScanner.Core.Interfaces;
using SecurityScanner.Core.Models;

namespace SecurityScanner.Services.Scanners;

public class SecurityHeadersService : IScannerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SecurityHeadersService> _logger;

    public string ServiceName => "Security Headers";
    public ScannerType ScannerType => ScannerType.SecurityHeaders;

    public SecurityHeadersService(IHttpClientFactory httpClientFactory, ILogger<SecurityHeadersService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("SecurityHeaders-RateLimited");
        _logger = logger;
    }

    public async Task<ScanResult> ScanAsync(DomainConfig domain, CancellationToken cancellationToken = default)
    {
        var result = new ScanResult
        {
            Domain = domain.Name,
            Url = domain.Url,
            Scanner = ServiceName,
            StartTime = DateTime.UtcNow,
            Status = ScanStatus.InProgress
        };

        try
        {
            _logger.LogInformation("Starting security headers scan for {Domain}", domain.Name);
            
            var response = await _httpClient.GetAsync(domain.Url, cancellationToken).ConfigureAwait(false);
            
            var vulnerabilities = new List<VulnerabilityReport>();
            
            // Check for missing security headers
            CheckHstsHeader(response.Headers, domain.Url, vulnerabilities);
            CheckContentTypeOptionsHeader(response.Headers, domain.Url, vulnerabilities);
            CheckFrameOptionsHeader(response.Headers, domain.Url, vulnerabilities);
            CheckXssProtectionHeader(response.Headers, domain.Url, vulnerabilities);
            CheckContentSecurityPolicyHeader(response.Headers, domain.Url, vulnerabilities);
            CheckReferrerPolicyHeader(response.Headers, domain.Url, vulnerabilities);
            CheckPermissionsPolicyHeader(response.Headers, domain.Url, vulnerabilities);
            
            // Check for information disclosure headers
            CheckServerHeader(response.Headers, domain.Url, vulnerabilities);
            CheckPoweredByHeader(response.Headers, domain.Url, vulnerabilities);
            
            result.Vulnerabilities = vulnerabilities;
            result.Status = ScanStatus.Completed;
            result.EndTime = DateTime.UtcNow;
            
            _logger.LogInformation("Completed security headers scan for {Domain}. Found {VulnerabilityCount} issues", 
                domain.Name, vulnerabilities.Count);
        }
        catch (HttpRequestException ex)
        {
            result.Status = ScanStatus.Failed;
            result.ErrorMessage = $"HTTP request failed: {ex.Message}";
            _logger.LogError(ex, "HTTP request failed for {Domain}", domain.Name);
        }
        catch (TaskCanceledException ex)
        {
            result.Status = ScanStatus.Timeout;
            result.ErrorMessage = "Request timed out";
            _logger.LogWarning(ex, "Request timed out for {Domain}", domain.Name);
        }
        catch (Exception ex)
        {
            result.Status = ScanStatus.Failed;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Unexpected error during security headers scan for {Domain}", domain.Name);
        }

        return result;
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            // Test with a simple HEAD request to a reliable endpoint
            using var response = await _httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Head, "https://www.google.com"),
                CancellationToken.None).ConfigureAwait(false);
            
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task<string> GetServiceVersionAsync()
    {
        return Task.FromResult("SecurityHeaders/1.0");
    }

    private static void CheckHstsHeader(System.Net.Http.Headers.HttpResponseHeaders headers, string url, List<VulnerabilityReport> vulnerabilities)
    {
        if (!headers.Contains("Strict-Transport-Security") && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "SEC-001",
                Name = "Missing HSTS Header",
                Severity = SeverityLevel.Medium,
                Category = "Transport Security",
                Scanner = "Security Headers",
                Url = url,
                Description = "The Strict-Transport-Security header is missing. This header helps protect against man-in-the-middle attacks by ensuring browsers only connect via HTTPS.",
                Solution = "Add the Strict-Transport-Security header: 'Strict-Transport-Security: max-age=31536000; includeSubDomains'"
            });
        }
    }

    private static void CheckContentTypeOptionsHeader(System.Net.Http.Headers.HttpResponseHeaders headers, string url, List<VulnerabilityReport> vulnerabilities)
    {
        if (!headers.Contains("X-Content-Type-Options"))
        {
            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "SEC-002",
                Name = "Missing X-Content-Type-Options Header",
                Severity = SeverityLevel.Low,
                Category = "Content Security",
                Scanner = "Security Headers",
                Url = url,
                Description = "The X-Content-Type-Options header is missing. This header prevents browsers from MIME-sniffing responses.",
                Solution = "Add the X-Content-Type-Options header: 'X-Content-Type-Options: nosniff'"
            });
        }
    }

    private static void CheckFrameOptionsHeader(System.Net.Http.Headers.HttpResponseHeaders headers, string url, List<VulnerabilityReport> vulnerabilities)
    {
        if (!headers.Contains("X-Frame-Options") && !headers.Contains("Content-Security-Policy"))
        {
            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "SEC-003",
                Name = "Missing X-Frame-Options Header",
                Severity = SeverityLevel.Medium,
                Category = "Clickjacking Protection",
                Scanner = "Security Headers",
                Url = url,
                Description = "The X-Frame-Options header is missing and no frame-ancestors directive found in CSP. This may allow clickjacking attacks.",
                Solution = "Add the X-Frame-Options header: 'X-Frame-Options: DENY' or 'X-Frame-Options: SAMEORIGIN'"
            });
        }
    }

    private static void CheckXssProtectionHeader(System.Net.Http.Headers.HttpResponseHeaders headers, string url, List<VulnerabilityReport> vulnerabilities)
    {
        if (headers.Contains("X-XSS-Protection"))
        {
            var xssProtection = headers.GetValues("X-XSS-Protection").FirstOrDefault();
            if (xssProtection != "0")
            {
                vulnerabilities.Add(new VulnerabilityReport
                {
                    Id = "SEC-004",
                    Name = "Deprecated X-XSS-Protection Header",
                    Severity = SeverityLevel.Info,
                    Category = "Content Security",
                    Scanner = "Security Headers",
                    Url = url,
                    Description = "The X-XSS-Protection header is deprecated and should be disabled or removed in favor of Content Security Policy.",
                    Solution = "Remove the X-XSS-Protection header or set it to '0', and implement a strong Content Security Policy instead."
                });
            }
        }
    }

    private static void CheckContentSecurityPolicyHeader(System.Net.Http.Headers.HttpResponseHeaders headers, string url, List<VulnerabilityReport> vulnerabilities)
    {
        if (!headers.Contains("Content-Security-Policy") && !headers.Contains("Content-Security-Policy-Report-Only"))
        {
            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "SEC-005",
                Name = "Missing Content Security Policy",
                Severity = SeverityLevel.High,
                Category = "Content Security",
                Scanner = "Security Headers",
                Url = url,
                Description = "No Content Security Policy (CSP) header found. CSP helps prevent XSS attacks and other code injection attacks.",
                Solution = "Implement a Content Security Policy header appropriate for your application's needs."
            });
        }
    }

    private static void CheckReferrerPolicyHeader(System.Net.Http.Headers.HttpResponseHeaders headers, string url, List<VulnerabilityReport> vulnerabilities)
    {
        if (!headers.Contains("Referrer-Policy"))
        {
            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "SEC-006",
                Name = "Missing Referrer-Policy Header",
                Severity = SeverityLevel.Low,
                Category = "Privacy",
                Scanner = "Security Headers",
                Url = url,
                Description = "The Referrer-Policy header is missing. This header controls how much referrer information is sent with requests.",
                Solution = "Add the Referrer-Policy header with an appropriate value like 'strict-origin-when-cross-origin'"
            });
        }
    }

    private static void CheckPermissionsPolicyHeader(System.Net.Http.Headers.HttpResponseHeaders headers, string url, List<VulnerabilityReport> vulnerabilities)
    {
        if (!headers.Contains("Permissions-Policy") && !headers.Contains("Feature-Policy"))
        {
            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "SEC-007",
                Name = "Missing Permissions-Policy Header",
                Severity = SeverityLevel.Info,
                Category = "Privacy",
                Scanner = "Security Headers",
                Url = url,
                Description = "The Permissions-Policy header is missing. This header controls which browser features can be used.",
                Solution = "Consider adding a Permissions-Policy header to control access to browser features."
            });
        }
    }

    private static void CheckServerHeader(System.Net.Http.Headers.HttpResponseHeaders headers, string url, List<VulnerabilityReport> vulnerabilities)
    {
        if (headers.Contains("Server"))
        {
            var serverHeader = headers.GetValues("Server").FirstOrDefault();
            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "SEC-008",
                Name = "Server Information Disclosure",
                Severity = SeverityLevel.Info,
                Category = "Information Disclosure",
                Scanner = "Security Headers",
                Url = url,
                Description = $"The Server header reveals server information: {serverHeader}",
                Solution = "Remove or obfuscate the Server header to prevent information disclosure.",
                Evidence = serverHeader
            });
        }
    }

    private static void CheckPoweredByHeader(System.Net.Http.Headers.HttpResponseHeaders headers, string url, List<VulnerabilityReport> vulnerabilities)
    {
        if (headers.Contains("X-Powered-By"))
        {
            var poweredByHeader = headers.GetValues("X-Powered-By").FirstOrDefault();
            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "SEC-009",
                Name = "Technology Information Disclosure",
                Severity = SeverityLevel.Info,
                Category = "Information Disclosure",
                Scanner = "Security Headers",
                Url = url,
                Description = $"The X-Powered-By header reveals technology information: {poweredByHeader}",
                Solution = "Remove the X-Powered-By header to prevent technology stack disclosure.",
                Evidence = poweredByHeader
            });
        }
    }
}