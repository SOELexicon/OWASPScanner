# Scanner Documentation

SecurityScanner includes multiple scanner types for comprehensive security analysis.

## Available Scanners

- **[Security Headers](security-headers.md)** - HTTP security header analysis
- **[SSL Labs](ssl-labs.md)** - SSL/TLS certificate and configuration analysis
- **[OWASP ZAP](owasp-zap.md)** - Comprehensive vulnerability scanning
- **[Load Test](load-test.md)** - Performance and load testing analysis

## Common Interface

All scanners implement the `IScannerService` interface:

```csharp
public interface IScannerService
{
    string ServiceName { get; }
    ScannerType ScannerType { get; }
    Task<ScanResult> ScanAsync(DomainConfig domain, CancellationToken cancellationToken);
    Task<bool> IsServiceAvailableAsync();
    Task<string> GetServiceVersionAsync();
}
```

## Scanner Selection

### Command Line Usage

```bash
# Single scanner
dotnet run --project src/SecurityScanner.App -- example.com --tools headers

# Multiple scanners
dotnet run --project src/SecurityScanner.App -- example.com --tools headers,ssl,zap

# All scanners
dotnet run --project src/SecurityScanner.App -- example.com --tools headers,ssl,zap,loadtest
```

### Scanner Types

- `headers` - Security Headers Scanner
- `ssl` - SSL Labs Scanner
- `zap` - OWASP ZAP Scanner
- `loadtest` - Load Test Scanner

## Configuration

Each scanner has its own configuration section in `appsettings.json`:

```json
{
  "ScannerSettings": {
    "SecurityHeaders": { /* ... */ },
    "SslLabs": { /* ... */ },
    "OwaspZap": { /* ... */ },
    "LoadTest": { /* ... */ }
  }
}
```

## Output Format

All scanners produce standardized `ScanResult` objects with:

- **Basic Information**: Domain, URL, scanner name, timestamps
- **Status**: Completed, Failed, or Timeout
- **Vulnerabilities**: List of security issues found
- **Scanner-Specific Data**: Additional metadata

## Severity Levels

Vulnerabilities are classified by severity:

- **Critical (3)**: Immediate security risks
- **High (2)**: Important security issues
- **Medium (1)**: Moderate security concerns
- **Low (0)**: Minor issues or informational

## Performance Considerations

- **Concurrency**: Scanners run in parallel by default
- **Timeouts**: Each scanner has configurable timeout values
- **Rate Limiting**: Built-in rate limiting for external APIs
- **Caching**: Some scanners support result caching

## Health Checks

Check scanner availability:

```bash
# Check all scanners
dotnet run --project src/SecurityScanner.App -- health

# Check specific scanners
dotnet run --project src/SecurityScanner.App -- health --scanners headers,ssl
```

## Adding Custom Scanners

1. Implement `IScannerService` interface
2. Register in dependency injection container
3. Add to command-line tool selection
4. Create documentation and tests

See [Development Guide](../development.md) for detailed instructions.

## Scanner Comparison

| Scanner | Speed | Accuracy | External Dependency | Use Case |
|---------|-------|----------|-------------------|----------|
| Security Headers | Fast | High | None | Basic security posture |
| SSL Labs | Medium | Very High | SSL Labs API | Certificate analysis |
| OWASP ZAP | Slow | Very High | ZAP Installation | Comprehensive security testing |
| Load Test | Variable | Medium | None | Performance analysis |

## Best Practices

1. **Start with Security Headers** - Fast initial assessment
2. **Use SSL Labs for Production** - Verify certificate configuration
3. **OWASP ZAP for Deep Analysis** - Comprehensive vulnerability scanning
4. **Load Test with Caution** - Can impact production systems
5. **Monitor External Dependencies** - Check service availability

## Troubleshooting

Common issues and solutions:

- **Timeouts**: Increase timeout values in configuration
- **Rate Limiting**: Reduce concurrency or add delays
- **Service Unavailable**: Check external service status
- **Authentication**: Verify API keys and credentials

For detailed troubleshooting, see the [Troubleshooting Guide](../troubleshooting.md).