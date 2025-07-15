# API Reference

## Core Interfaces

### IScannerService

The main interface implemented by all scanner services.

```csharp
public interface IScannerService
{
    /// <summary>
    /// Human-readable name of the scanner service
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Type identifier for the scanner
    /// </summary>
    ScannerType ScannerType { get; }

    /// <summary>
    /// Performs a security scan of the specified domain
    /// </summary>
    /// <param name="domain">Domain configuration to scan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scan results including vulnerabilities found</returns>
    Task<ScanResult> ScanAsync(DomainConfig domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the scanner service is available
    /// </summary>
    /// <returns>True if service is available</returns>
    Task<bool> IsServiceAvailableAsync();

    /// <summary>
    /// Gets the version information of the scanner service
    /// </summary>
    /// <returns>Version string</returns>
    Task<string> GetServiceVersionAsync();
}
```

### ICommandHandler<TRequest, TResponse>

Interface for command handlers in the command pattern.

```csharp
public interface ICommandHandler<TRequest, TResponse>
    where TRequest : CommandRequest
    where TResponse : CommandResponse
{
    /// <summary>
    /// Handles the command request
    /// </summary>
    /// <param name="request">Command request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Command response</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
```

### IReportService

Interface for report generation services.

```csharp
public interface IReportService
{
    /// <summary>
    /// Generates a JSON report from scan results
    /// </summary>
    /// <param name="scanResults">Scan results to include in report</param>
    /// <returns>JSON report string</returns>
    Task<string> GenerateJsonReportAsync(IEnumerable<ScanResult> scanResults);

    /// <summary>
    /// Generates an HTML report from scan results
    /// </summary>
    /// <param name="scanResults">Scan results to include in report</param>
    /// <returns>HTML report string</returns>
    Task<string> GenerateHtmlReportAsync(IEnumerable<ScanResult> scanResults);

    /// <summary>
    /// Saves a report to the specified file path
    /// </summary>
    /// <param name="reportContent">Report content to save</param>
    /// <param name="filePath">Destination file path</param>
    Task SaveReportAsync(string reportContent, string filePath);
}
```

## Core Models

### ScanResult

Represents the result of a security scan.

```csharp
public class ScanResult
{
    /// <summary>
    /// Domain name that was scanned
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Full URL that was scanned
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Status of the scan (Completed, Failed, Timeout)
    /// </summary>
    public ScanStatus Status { get; set; }

    /// <summary>
    /// When the scan started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the scan ended
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Duration of the scan
    /// </summary>
    public double ScanDuration => (EndTime - StartTime).TotalSeconds;

    /// <summary>
    /// Error message if scan failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Name of the scanner that performed the scan
    /// </summary>
    public string Scanner { get; set; } = string.Empty;

    /// <summary>
    /// List of vulnerabilities found
    /// </summary>
    public List<VulnerabilityReport> Vulnerabilities { get; set; } = new();

    /// <summary>
    /// Scanner-specific additional data
    /// </summary>
    public Dictionary<string, object> ScannerSpecificData { get; set; } = new();

    /// <summary>
    /// Total number of vulnerabilities found
    /// </summary>
    public int VulnerabilityCount => Vulnerabilities.Count;

    /// <summary>
    /// Number of critical vulnerabilities
    /// </summary>
    public int CriticalVulnerabilityCount => Vulnerabilities.Count(v => v.Severity == SeverityLevel.Critical);

    /// <summary>
    /// Number of high severity vulnerabilities
    /// </summary>
    public int HighVulnerabilityCount => Vulnerabilities.Count(v => v.Severity == SeverityLevel.High);
}
```

### VulnerabilityReport

Represents a security vulnerability found during scanning.

```csharp
public class VulnerabilityReport
{
    /// <summary>
    /// Unique identifier for the vulnerability
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the vulnerability
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Severity level of the vulnerability
    /// </summary>
    public SeverityLevel Severity { get; set; }

    /// <summary>
    /// Category of the vulnerability
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Scanner that detected the vulnerability
    /// </summary>
    public string Scanner { get; set; } = string.Empty;

    /// <summary>
    /// URL where the vulnerability was found
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the vulnerability
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Recommended solution or mitigation
    /// </summary>
    public string Solution { get; set; } = string.Empty;

    /// <summary>
    /// Evidence or proof of the vulnerability
    /// </summary>
    public string? Evidence { get; set; }

    /// <summary>
    /// Additional metadata about the vulnerability
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    /// <summary>
    /// When the vulnerability was discovered
    /// </summary>
    public DateTime DiscoveredAt { get; set; }
}
```

### ScanConfiguration

Configuration for running scans.

```csharp
public class ScanConfiguration
{
    /// <summary>
    /// List of domains to scan
    /// </summary>
    public List<DomainConfig> Domains { get; set; } = new();

    /// <summary>
    /// List of scanner types to use
    /// </summary>
    public List<ScannerType> EnabledScanners { get; set; } = new();

    /// <summary>
    /// Maximum number of concurrent scans
    /// </summary>
    public int MaxConcurrentScans { get; set; } = 3;

    /// <summary>
    /// Default timeout for scans
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to run scans in parallel
    /// </summary>
    public bool ParallelExecution { get; set; } = true;

    /// <summary>
    /// Whether to output results in JSON format
    /// </summary>
    public bool JsonOutput { get; set; }

    /// <summary>
    /// Output file path for results
    /// </summary>
    public string? OutputFile { get; set; }

    /// <summary>
    /// Load testing configuration
    /// </summary>
    public LoadTestSettings? LoadTestSettings { get; set; }
}
```

### DomainConfig

Configuration for a specific domain to scan.

```csharp
public class DomainConfig
{
    /// <summary>
    /// Domain name (e.g., "example.com")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full URL to scan (e.g., "https://example.com")
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// List of scanner types enabled for this domain
    /// </summary>
    public List<ScannerType> EnabledScanners { get; set; } = new();

    /// <summary>
    /// Scanner-specific settings for this domain
    /// </summary>
    public Dictionary<string, object> ScannerSettings { get; set; } = new();

    /// <summary>
    /// Custom timeout for this domain
    /// </summary>
    public TimeSpan? CustomTimeout { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
```

## Enumerations

### ScannerType

Identifies the type of scanner.

```csharp
public enum ScannerType
{
    /// <summary>
    /// Security headers analysis
    /// </summary>
    SecurityHeaders = 1,

    /// <summary>
    /// SSL Labs certificate analysis
    /// </summary>
    SslLabs = 2,

    /// <summary>
    /// OWASP ZAP vulnerability scanning
    /// </summary>
    OwaspZap = 3,

    /// <summary>
    /// Load testing and performance analysis
    /// </summary>
    LoadTest = 4,

    /// <summary>
    /// Nmap port scanning
    /// </summary>
    Nmap = 5
}
```

### ScanStatus

Represents the status of a scan.

```csharp
public enum ScanStatus
{
    /// <summary>
    /// Scan is pending or in progress
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Scan completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Scan failed due to error
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Scan timed out
    /// </summary>
    Timeout = 4
}
```

### SeverityLevel

Represents the severity of a vulnerability.

```csharp
public enum SeverityLevel
{
    /// <summary>
    /// Informational or very low impact
    /// </summary>
    Info = 0,

    /// <summary>
    /// Low severity issue
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium severity issue
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High severity issue
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical security issue
    /// </summary>
    Critical = 4
}
```

## Command Models

### CommandRequest

Base class for all command requests.

```csharp
public abstract class CommandRequest
{
    /// <summary>
    /// Whether to output in JSON format
    /// </summary>
    public bool JsonOutput { get; set; }

    /// <summary>
    /// Output file path
    /// </summary>
    public string? OutputFile { get; set; }

    /// <summary>
    /// Enable verbose logging
    /// </summary>
    public bool Verbose { get; set; }
}
```

### CommandResponse

Base class for all command responses.

```csharp
public abstract class CommandResponse
{
    /// <summary>
    /// Whether the command succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if command failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the command was executed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

### ScanCommandRequest

Request for executing a scan command.

```csharp
public class ScanCommandRequest : CommandRequest
{
    /// <summary>
    /// List of domains to scan
    /// </summary>
    public List<string> Domains { get; set; } = new();

    /// <summary>
    /// List of scanner types to use
    /// </summary>
    public List<ScannerType> Tools { get; set; } = new() { ScannerType.SecurityHeaders };

    /// <summary>
    /// JSON file containing URLs to scan
    /// </summary>
    public string? InputFile { get; set; }

    /// <summary>
    /// Scan all configured domains
    /// </summary>
    public bool ScanAll { get; set; }

    /// <summary>
    /// Maximum concurrent scans
    /// </summary>
    public int MaxConcurrent { get; set; } = 3;

    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Enable parallel execution
    /// </summary>
    public bool ParallelExecution { get; set; } = true;

    // Load test specific options
    public int LoadTestRps { get; set; } = 10;
    public int LoadTestDuration { get; set; } = 60;
    public int LoadTestRampUp { get; set; } = 0;
    public int LoadTestMaxConcurrent { get; set; } = 50;
}
```

### ScanCommandResponse

Response from a scan command.

```csharp
public class ScanCommandResponse : CommandResponse
{
    /// <summary>
    /// List of scan results
    /// </summary>
    public List<ScanResult> ScanResults { get; set; } = new();

    /// <summary>
    /// Summary of scan results
    /// </summary>
    public ScanSummary Summary { get; set; } = new();
}
```

## Configuration Models

### ScannerSettings

Configuration for scanner services.

```csharp
public class ScannerSettings
{
    /// <summary>
    /// Security headers scanner settings
    /// </summary>
    public SecurityHeadersSettings SecurityHeaders { get; set; } = new();

    /// <summary>
    /// SSL Labs scanner settings
    /// </summary>
    public SslLabsSettings SslLabs { get; set; } = new();

    /// <summary>
    /// OWASP ZAP scanner settings
    /// </summary>
    public OwaspZapSettings OwaspZap { get; set; } = new();

    /// <summary>
    /// Load test scanner settings
    /// </summary>
    public LoadTestSettings LoadTest { get; set; } = new();
}
```

### SecurityHeadersSettings

Configuration for security headers scanner.

```csharp
public class SecurityHeadersSettings
{
    /// <summary>
    /// Request timeout
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Delay between requests
    /// </summary>
    public TimeSpan DelayBetweenRequests { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Maximum number of redirects to follow
    /// </summary>
    public int MaxRedirects { get; set; } = 5;

    /// <summary>
    /// User agent string
    /// </summary>
    public string UserAgent { get; set; } = "SecurityScanner/1.0";

    /// <summary>
    /// Whether to follow redirects
    /// </summary>
    public bool FollowRedirects { get; set; } = true;

    /// <summary>
    /// Whether to validate SSL certificates
    /// </summary>
    public bool ValidateCertificates { get; set; } = true;
}
```

## Exceptions

### ScannerException

Base exception for scanner-related errors.

```csharp
public class ScannerException : Exception
{
    /// <summary>
    /// Type of scanner that threw the exception
    /// </summary>
    public ScannerType ScannerType { get; }

    /// <summary>
    /// Domain name associated with the error
    /// </summary>
    public string DomainName { get; }

    public ScannerException(string message, ScannerType scannerType, string domainName) 
        : base(message)
    {
        ScannerType = scannerType;
        DomainName = domainName;
    }

    public ScannerException(string message, Exception innerException, ScannerType scannerType, string domainName) 
        : base(message, innerException)
    {
        ScannerType = scannerType;
        DomainName = domainName;
    }
}
```

### ConfigurationException

Exception thrown for configuration-related errors.

```csharp
public class ConfigurationException : ScannerException
{
    /// <summary>
    /// Configuration key that caused the error
    /// </summary>
    public string ConfigurationKey { get; }

    public ConfigurationException(string message, string configurationKey, ScannerType scannerType, string domainName) 
        : base(message, scannerType, domainName)
    {
        ConfigurationKey = configurationKey;
    }
}
```

## Usage Examples

### Basic Scanner Implementation

```csharp
public class CustomScannerService : IScannerService
{
    public string ServiceName => "Custom Scanner";
    public ScannerType ScannerType => ScannerType.Custom;

    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomScannerService> _logger;

    public CustomScannerService(IHttpClientFactory httpClientFactory, ILogger<CustomScannerService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CustomScanner");
        _logger = logger;
    }

    public async Task<ScanResult> ScanAsync(DomainConfig domain, CancellationToken cancellationToken)
    {
        var result = new ScanResult
        {
            Domain = domain.Name,
            Url = domain.Url,
            Scanner = ServiceName,
            StartTime = DateTime.UtcNow
        };

        try
        {
            var response = await _httpClient.GetAsync(domain.Url, cancellationToken);
            var vulnerabilities = await AnalyzeResponseAsync(response, domain.Url);
            
            result.Vulnerabilities = vulnerabilities;
            result.Status = ScanStatus.Completed;
        }
        catch (Exception ex)
        {
            result.Status = ScanStatus.Failed;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Scan failed for {Domain}", domain.Name);
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            // Check service availability
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetServiceVersionAsync()
    {
        return "Custom Scanner v1.0";
    }

    private async Task<List<VulnerabilityReport>> AnalyzeResponseAsync(HttpResponseMessage response, string url)
    {
        var vulnerabilities = new List<VulnerabilityReport>();
        
        // Implement analysis logic
        
        return vulnerabilities;
    }
}
```

### Command Handler Implementation

```csharp
public class CustomCommandHandler : ICommandHandler<CustomCommandRequest, CustomCommandResponse>
{
    private readonly ILogger<CustomCommandHandler> _logger;

    public CustomCommandHandler(ILogger<CustomCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<CustomCommandResponse> HandleAsync(CustomCommandRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing custom command for {Parameter}", request.Parameter);

        try
        {
            // Implement command logic
            var result = await ProcessCommandAsync(request, cancellationToken);

            return new CustomCommandResponse
            {
                Success = true,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom command failed");
            
            return new CustomCommandResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<string> ProcessCommandAsync(CustomCommandRequest request, CancellationToken cancellationToken)
    {
        // Implement processing logic
        return "Command processed successfully";
    }
}
```

This API reference provides comprehensive documentation for all core interfaces, models, and usage patterns in the SecurityScanner application. For implementation examples and best practices, refer to the [Development Guide](development.md).