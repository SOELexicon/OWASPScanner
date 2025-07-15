# Security Headers Scanner

The Security Headers Scanner analyzes HTTP response headers to identify missing or misconfigured security headers.

## Overview

This scanner performs a simple HTTP GET request to the target domain and analyzes the response headers for common security headers. It's the fastest scanner and runs by default.

## Configuration

```json
{
  "ScannerSettings": {
    "SecurityHeaders": {
      "RequestTimeout": "00:00:30",
      "DelayBetweenRequests": "00:00:00.100",
      "MaxRedirects": 5,
      "UserAgent": "SecurityScanner/1.0",
      "FollowRedirects": true,
      "ValidateCertificates": true
    }
  }
}
```

### Configuration Options

- `RequestTimeout` - Maximum time to wait for response
- `DelayBetweenRequests` - Delay between consecutive requests
- `MaxRedirects` - Maximum number of redirects to follow
- `UserAgent` - User agent string for requests
- `FollowRedirects` - Whether to follow HTTP redirects
- `ValidateCertificates` - Whether to validate SSL certificates

## Usage

### Command Line

```bash
# Security headers scan (default)
dotnet run --project src/SecurityScanner.App -- example.com

# Explicit security headers scan
dotnet run --project src/SecurityScanner.App -- example.com --tools headers

# With JSON output
dotnet run --project src/SecurityScanner.App -- example.com --tools headers --json
```

### Bulk Scanning

```bash
# Scan multiple domains
dotnet run --project src/SecurityScanner.App -- example.com,api.example.com --tools headers --json

# Scan from file
dotnet run --project src/SecurityScanner.App -- --file urls.json --tools headers --json
```

## Analyzed Headers

### Security Headers Checked

1. **Strict-Transport-Security (HSTS)**
   - Prevents protocol downgrade attacks
   - Severity: Medium if missing

2. **X-Content-Type-Options**
   - Prevents MIME type sniffing
   - Severity: Low if missing

3. **X-Frame-Options**
   - Prevents clickjacking attacks
   - Severity: Medium if missing (and no CSP frame-ancestors)

4. **X-XSS-Protection**
   - Legacy XSS protection header
   - Severity: Low if missing

5. **Content-Security-Policy (CSP)**
   - Comprehensive content security policy
   - Severity: High if missing

6. **Referrer-Policy**
   - Controls referrer information
   - Severity: Low if missing

7. **Permissions-Policy**
   - Controls browser feature access
   - Severity: Info if missing

### Information Disclosure

- **Server Header** - Reveals server technology
- **X-Powered-By** - Reveals framework information
- **X-AspNet-Version** - Reveals ASP.NET version

## Vulnerability Detection

### Example Vulnerabilities

```json
{
  "Id": "SEC-001",
  "Name": "Missing HSTS Header",
  "Severity": 2,
  "Category": "Transport Security",
  "Description": "The Strict-Transport-Security header is missing...",
  "Solution": "Add the Strict-Transport-Security header: 'Strict-Transport-Security: max-age=31536000; includeSubDomains'",
  "Evidence": null
}
```

### Vulnerability Categories

- **Transport Security** - HSTS related issues
- **Content Security** - CSP and content type issues
- **Clickjacking Protection** - Frame options issues
- **Privacy** - Referrer and permissions policy issues
- **Information Disclosure** - Server information leakage

## Advanced Analysis

### CSP Analysis

The scanner performs basic CSP analysis:

```csharp
private bool HasValidCSP(string cspHeader)
{
    // Check for basic CSP directives
    return cspHeader.Contains("default-src") || 
           cspHeader.Contains("script-src") || 
           cspHeader.Contains("style-src");
}
```

### Frame Options vs CSP

Checks for clickjacking protection:

```csharp
private bool HasClickjackingProtection(HttpResponseHeaders headers)
{
    // Check X-Frame-Options
    if (headers.Contains("X-Frame-Options"))
        return true;
    
    // Check CSP frame-ancestors
    if (headers.TryGetValues("Content-Security-Policy", out var cspValues))
        return cspValues.Any(csp => csp.Contains("frame-ancestors"));
    
    return false;
}
```

## Output Example

### JSON Output

```json
{
  "Domain": "example.com",
  "Url": "https://example.com",
  "Status": 2,
  "Scanner": "Security Headers",
  "Vulnerabilities": [
    {
      "Id": "SEC-001",
      "Name": "Missing HSTS Header",
      "Severity": 2,
      "Category": "Transport Security",
      "Description": "The Strict-Transport-Security header is missing. This header helps protect against man-in-the-middle attacks by ensuring browsers only connect via HTTPS.",
      "Solution": "Add the Strict-Transport-Security header: 'Strict-Transport-Security: max-age=31536000; includeSubDomains'",
      "Evidence": null
    }
  ],
  "VulnerabilityCount": 1
}
```

### Text Output

```
Domain: example.com
Status: Completed
Scanner: Security Headers
Vulnerabilities: 1
  - Missing HSTS Header (Medium)
    The Strict-Transport-Security header is missing. This header helps protect against man-in-the-middle attacks by ensuring browsers only connect via HTTPS.
```

## Performance

### Characteristics

- **Speed**: Very fast (< 1 second per domain)
- **Network**: Single HTTP request per domain
- **Resources**: Minimal CPU and memory usage
- **Scaling**: Handles hundreds of domains efficiently

### Optimization Tips

1. **Concurrency**: Increase `--max-concurrent` for bulk scans
2. **Timeouts**: Reduce timeout for faster scans
3. **Redirects**: Disable redirect following if not needed
4. **User Agent**: Use consistent user agent to avoid blocking

## Troubleshooting

### Common Issues

1. **Connection Timeouts**
   ```bash
   # Increase timeout
   dotnet run --project src/SecurityScanner.App -- example.com --timeout 60
   ```

2. **SSL Certificate Errors**
   ```json
   {
     "ScannerSettings": {
       "SecurityHeaders": {
         "ValidateCertificates": false
       }
     }
   }
   ```

3. **Rate Limiting**
   ```json
   {
     "ScannerSettings": {
       "SecurityHeaders": {
         "DelayBetweenRequests": "00:00:01"
       }
     }
   }
   ```

### Error Messages

- **"Request timed out"** - Increase timeout value
- **"SSL handshake failed"** - Check certificate validity
- **"Too many redirects"** - Reduce MaxRedirects value
- **"Access denied"** - User agent may be blocked

## Best Practices

### Security Testing

1. **Regular Scans** - Include in CI/CD pipeline
2. **Baseline Establishment** - Track header improvements over time
3. **False Positive Handling** - Some headers may not be applicable
4. **Complementary Tools** - Use with other scanners for comprehensive analysis

### Performance Optimization

1. **Batch Processing** - Use JSON file input for multiple domains
2. **Concurrent Scanning** - Adjust concurrency based on target capacity
3. **Result Caching** - Cache results for repeated scans
4. **Monitoring** - Track scan duration and success rates

## Integration

### CI/CD Pipeline

```yaml
- name: Security Headers Scan
  run: |
    dotnet run --project src/SecurityScanner.App -- \
      --file production-urls.json \
      --tools headers \
      --json \
      --output headers-report.json
    
    # Check for critical issues
    if jq -e '.Summary.HighVulnerabilities > 0' headers-report.json; then
      echo "High-severity header issues found!"
      exit 1
    fi
```

### Monitoring

```bash
# Health check
dotnet run --project src/SecurityScanner.App -- health --scanners headers

# Batch scan with monitoring
dotnet run --project src/SecurityScanner.App -- \
  --file monitoring-urls.json \
  --tools headers \
  --json \
  --output "headers-$(date +%Y%m%d).json"
```

## Limitations

1. **Static Analysis Only** - No dynamic content analysis
2. **No JavaScript Headers** - Headers set by JavaScript not detected
3. **Context Unaware** - Doesn't understand application context
4. **No Content Analysis** - Only analyzes headers, not content

## Future Enhancements

1. **Dynamic Header Detection** - Analyze JavaScript-set headers
2. **Context-Aware Analysis** - Understand application types
3. **Header Validation** - Validate header syntax and values
4. **Custom Rules** - Allow custom header validation rules

## Related Documentation

- [Configuration Guide](../configuration.md) - Detailed configuration options
- [User Guide](../user-guide.md) - Usage examples
- [Troubleshooting](../troubleshooting.md) - Common issues and solutions