using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SecurityScanner.Core.Interfaces;
using SecurityScanner.Core.Models;
using SecurityScanner.Infrastructure.Configuration;

namespace SecurityScanner.Services.Scanners;

public class SslLabsService : IScannerService
{
    public string ServiceName => "SSL Labs";
    public ScannerType ScannerType => ScannerType.SslLabs;

    private readonly HttpClient _httpClient;
    private readonly ILogger<SslLabsService> _logger;
    private readonly SslLabsSettings _settings;

    public SslLabsService(
        IHttpClientFactory httpClientFactory,
        ILogger<SslLabsService> logger,
        IOptions<ScannerSettings> scannerSettings)
    {
        _httpClient = httpClientFactory.CreateClient("SslLabs-RateLimited");
        _logger = logger;
        _settings = scannerSettings.Value.SslLabs;
    }

    public async Task<ScanResult> ScanAsync(DomainConfig domain, CancellationToken cancellationToken = default)
    {
        var result = new ScanResult
        {
            Domain = domain.Name,
            Url = domain.Url,
            Scanner = ServiceName,
            StartTime = DateTime.UtcNow
        };

        _logger.LogInformation("Starting SSL Labs scan for {Domain}", domain.Name);

        try
        {
            // Start the SSL Labs assessment
            var assessmentId = await StartAssessmentAsync(domain.Name, cancellationToken);
            
            // Poll for results
            var sslLabsResult = await PollForResultsAsync(domain.Name, assessmentId, cancellationToken);
            
            if (sslLabsResult != null)
            {
                result.Vulnerabilities = AnalyzeSslResults(sslLabsResult, domain.Url);
                result.Status = ScanStatus.Completed;
                result.ScannerSpecificData["sslLabsResult"] = sslLabsResult;
                
                _logger.LogInformation("Completed SSL Labs scan for {Domain}. Found {IssueCount} issues", 
                    domain.Name, result.VulnerabilityCount);
            }
            else
            {
                result.Status = ScanStatus.Failed;
                result.ErrorMessage = "SSL Labs assessment failed or timed out";
                _logger.LogWarning("SSL Labs scan failed for {Domain}", domain.Name);
            }
        }
        catch (Exception ex)
        {
            result.Status = ScanStatus.Failed;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "SSL Labs scan failed for {Domain}", domain.Name);
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }

    private async Task<string?> StartAssessmentAsync(string hostname, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_settings.ApiUrl}/analyze?host={Uri.EscapeDataString(hostname)}&publish=off&startNew=on&all=done&ignoreMismatch=on";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<SslLabsAssessmentResponse>(content);
            
            return result?.Host;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SSL Labs assessment for {Hostname}", hostname);
            return null;
        }
    }

    private async Task<SslLabsAssessmentResponse?> PollForResultsAsync(string hostname, string? assessmentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(assessmentId)) return null;

        var maxAttempts = 30; // Max 5 minutes with 10-second intervals
        var attempt = 0;
        
        while (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(10000, cancellationToken); // Wait 10 seconds between polls
                
                var url = $"{_settings.ApiUrl}/analyze?host={Uri.EscapeDataString(hostname)}&publish=off&all=done";
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonConvert.DeserializeObject<SslLabsAssessmentResponse>(content);
                
                if (result?.Status == "READY")
                {
                    return result;
                }
                
                if (result?.Status == "ERROR")
                {
                    _logger.LogError("SSL Labs assessment failed with error for {Hostname}", hostname);
                    return null;
                }
                
                _logger.LogDebug("SSL Labs assessment in progress for {Hostname}, status: {Status}", 
                    hostname, result?.Status);
                
                attempt++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling SSL Labs results for {Hostname}", hostname);
                return null;
            }
        }
        
        _logger.LogWarning("SSL Labs assessment timed out for {Hostname}", hostname);
        return null;
    }

    private List<VulnerabilityReport> AnalyzeSslResults(SslLabsAssessmentResponse assessment, string url)
    {
        var vulnerabilities = new List<VulnerabilityReport>();

        if (assessment.Endpoints == null || !assessment.Endpoints.Any())
        {
            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "SSL-001",
                Name = "No SSL Endpoints Found",
                Severity = SeverityLevel.High,
                Category = "SSL/TLS Configuration",
                Scanner = ServiceName,
                Url = url,
                Description = "No SSL/TLS endpoints were found for this domain.",
                Solution = "Ensure the domain has a valid SSL/TLS certificate properly configured.",
                DiscoveredAt = DateTime.UtcNow
            });
            return vulnerabilities;
        }

        foreach (var endpoint in assessment.Endpoints)
        {
            if (endpoint.Details == null) continue;

            // Check overall grade
            if (!string.IsNullOrEmpty(endpoint.Grade) && (endpoint.Grade.StartsWith("C") || endpoint.Grade.StartsWith("D") || endpoint.Grade.StartsWith("F")))
            {
                var severity = endpoint.Grade.StartsWith("F") ? SeverityLevel.Critical :
                              endpoint.Grade.StartsWith("D") ? SeverityLevel.High : SeverityLevel.Medium;

                vulnerabilities.Add(new VulnerabilityReport
                {
                    Id = "SSL-002",
                    Name = $"Poor SSL Configuration Grade: {endpoint.Grade}",
                    Severity = severity,
                    Category = "SSL/TLS Configuration",
                    Scanner = ServiceName,
                    Url = url,
                    Description = $"SSL Labs assessment resulted in a poor grade of {endpoint.Grade}.",
                    Solution = "Review and improve SSL/TLS configuration according to current best practices.",
                    Evidence = $"Endpoint: {endpoint.IpAddress}, Grade: {endpoint.Grade}",
                    DiscoveredAt = DateTime.UtcNow
                });
            }

            // Check for weak protocols
            if (endpoint.Details.Protocols != null)
            {
                foreach (var protocol in endpoint.Details.Protocols)
                {
                    if (protocol.Version == "1.0" || protocol.Version == "1.1")
                    {
                        vulnerabilities.Add(new VulnerabilityReport
                        {
                            Id = "SSL-003",
                            Name = $"Weak TLS Protocol: TLS {protocol.Version}",
                            Severity = SeverityLevel.Medium,
                            Category = "SSL/TLS Configuration",
                            Scanner = ServiceName,
                            Url = url,
                            Description = $"The server supports TLS {protocol.Version}, which is considered insecure.",
                            Solution = "Disable TLS 1.0 and TLS 1.1. Use TLS 1.2 or higher.",
                            Evidence = $"Endpoint: {endpoint.IpAddress}, Protocol: TLS {protocol.Version}",
                            DiscoveredAt = DateTime.UtcNow
                        });
                    }
                }
            }

            // Check certificate issues
            if (endpoint.Details.Cert != null)
            {
                var cert = endpoint.Details.Cert;
                
                // Check for self-signed certificate
                if (cert.Issues.HasFlag(SslCertIssues.SelfSigned))
                {
                    vulnerabilities.Add(new VulnerabilityReport
                    {
                        Id = "SSL-004",
                        Name = "Self-Signed Certificate",
                        Severity = SeverityLevel.High,
                        Category = "SSL/TLS Certificate",
                        Scanner = ServiceName,
                        Url = url,
                        Description = "The certificate is self-signed and not trusted by browsers.",
                        Solution = "Use a certificate from a trusted Certificate Authority.",
                        Evidence = $"Endpoint: {endpoint.IpAddress}",
                        DiscoveredAt = DateTime.UtcNow
                    });
                }

                // Check for expired certificate
                if (cert.NotAfter < DateTime.UtcNow)
                {
                    vulnerabilities.Add(new VulnerabilityReport
                    {
                        Id = "SSL-005",
                        Name = "Expired Certificate",
                        Severity = SeverityLevel.Critical,
                        Category = "SSL/TLS Certificate",
                        Scanner = ServiceName,
                        Url = url,
                        Description = "The SSL certificate has expired.",
                        Solution = "Renew the SSL certificate immediately.",
                        Evidence = $"Endpoint: {endpoint.IpAddress}, Expired: {cert.NotAfter:yyyy-MM-dd}",
                        DiscoveredAt = DateTime.UtcNow
                    });
                }

                // Check for soon-to-expire certificate (within 30 days)
                else if (cert.NotAfter < DateTime.UtcNow.AddDays(30))
                {
                    vulnerabilities.Add(new VulnerabilityReport
                    {
                        Id = "SSL-006",
                        Name = "Certificate Expiring Soon",
                        Severity = SeverityLevel.Medium,
                        Category = "SSL/TLS Certificate",
                        Scanner = ServiceName,
                        Url = url,
                        Description = "The SSL certificate will expire within 30 days.",
                        Solution = "Renew the SSL certificate before it expires.",
                        Evidence = $"Endpoint: {endpoint.IpAddress}, Expires: {cert.NotAfter:yyyy-MM-dd}",
                        DiscoveredAt = DateTime.UtcNow
                    });
                }
            }

            // Check for weak key
            if (endpoint.Details.Key != null && endpoint.Details.Key.Size < 2048)
            {
                vulnerabilities.Add(new VulnerabilityReport
                {
                    Id = "SSL-007",
                    Name = "Weak Certificate Key",
                    Severity = SeverityLevel.High,
                    Category = "SSL/TLS Certificate",
                    Scanner = ServiceName,
                    Url = url,
                    Description = $"The certificate uses a weak {endpoint.Details.Key.Size}-bit key.",
                    Solution = "Use a certificate with at least a 2048-bit RSA key or 256-bit ECDSA key.",
                    Evidence = $"Endpoint: {endpoint.IpAddress}, Key Size: {endpoint.Details.Key.Size} bits",
                    DiscoveredAt = DateTime.UtcNow
                });
            }
        }

        return vulnerabilities;
    }
}

// SSL Labs API Response Models
public class SslLabsAssessmentResponse
{
    public string? Host { get; set; }
    public string? Status { get; set; }
    public List<SslLabsEndpoint>? Endpoints { get; set; }
}

public class SslLabsEndpoint
{
    public string? IpAddress { get; set; }
    public string? Grade { get; set; }
    public SslLabsEndpointDetails? Details { get; set; }
}

public class SslLabsEndpointDetails
{
    public List<SslLabsProtocol>? Protocols { get; set; }
    public SslLabsCert? Cert { get; set; }
    public SslLabsKey? Key { get; set; }
}

public class SslLabsProtocol
{
    public string? Version { get; set; }
    public string? Name { get; set; }
}

public class SslLabsCert
{
    public string? Subject { get; set; }
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
    public SslCertIssues Issues { get; set; }
}

public class SslLabsKey
{
    public int Size { get; set; }
    public string? Alg { get; set; }
}

[Flags]
public enum SslCertIssues
{
    None = 0,
    SelfSigned = 1,
    NotYetValid = 2,
    Expired = 4,
    WrongHost = 8,
    Revoked = 16,
    BadSignature = 32,
    BlackListed = 64,
    WeakKey = 128
}