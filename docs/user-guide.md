# User Guide

## Basic Usage

### Single Domain Scan

```bash
# Scan with default security headers check
dotnet run --project src/SecurityScanner.App -- example.com

# JSON output for automation
dotnet run --project src/SecurityScanner.App -- example.com --json
```

### Multiple Domains

```bash
# Comma-separated domains
dotnet run --project src/SecurityScanner.App -- example.com,api.example.com,test.example.com --json

# From JSON file
dotnet run --project src/SecurityScanner.App -- --file urls.json --json
```

### JSON File Format

Create a `urls.json` file:

```json
[
  "example.com",
  "api.example.com",
  "test.example.com",
  "https://secure.example.com"
]
```

## Scanner Types

### Available Scanners

- **headers** - Security headers analysis (default)
- **ssl** - SSL/TLS certificate analysis via SSL Labs
- **zap** - OWASP ZAP vulnerability scanning
- **loadtest** - Performance and load testing

### Using Specific Scanners

```bash
# Security headers only (default)
dotnet run --project src/SecurityScanner.App -- example.com --tools headers --json

# SSL analysis
dotnet run --project src/SecurityScanner.App -- example.com --tools ssl --json

# Multiple scanners
dotnet run --project src/SecurityScanner.App -- example.com --tools headers,ssl,zap --json

# All scanners
dotnet run --project src/SecurityScanner.App -- example.com --tools headers,ssl,zap,loadtest --json
```

## Advanced Options

### Load Testing

```bash
# Custom load test parameters
dotnet run --project src/SecurityScanner.App -- example.com \
  --tools loadtest \
  --loadtest-rps 20 \
  --loadtest-duration 120 \
  --loadtest-rampup 30 \
  --loadtest-max-concurrent 100 \
  --json
```

Load test parameters:
- `--loadtest-rps` - Requests per second (default: 10)
- `--loadtest-duration` - Test duration in seconds (default: 60)
- `--loadtest-rampup` - Ramp-up time in seconds (default: 0)
- `--loadtest-max-concurrent` - Maximum concurrent requests (default: 50)

### Concurrency and Timeouts

```bash
# Control scan concurrency
dotnet run --project src/SecurityScanner.App -- example.com \
  --max-concurrent 5 \
  --timeout 600 \
  --json
```

### Output Options

```bash
# Save to file
dotnet run --project src/SecurityScanner.App -- example.com --json --output results.json

# Verbose logging
dotnet run --project src/SecurityScanner.App -- example.com --verbose

# Both JSON and file output
dotnet run --project src/SecurityScanner.App -- example.com --json --output results.json --verbose
```

## Output Formats

### JSON Output Structure

```json
{
  "ScanResults": [
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
          "Description": "The Strict-Transport-Security header is missing...",
          "Solution": "Add the Strict-Transport-Security header...",
          "Evidence": null
        }
      ],
      "VulnerabilityCount": 1
    }
  ],
  "Summary": {
    "TotalDomains": 1,
    "SuccessfulScans": 1,
    "FailedScans": 0,
    "TotalVulnerabilities": 1,
    "CriticalVulnerabilities": 0,
    "HighVulnerabilities": 0
  },
  "Success": true,
  "Timestamp": "2025-01-15T10:30:00Z"
}
```

### Severity Levels

- **Critical** (3) - Immediate action required
- **High** (2) - Important security issues
- **Medium** (1) - Moderate security concerns
- **Low** (0) - Minor issues or informational

## Common Workflows

### CI/CD Integration

```bash
#!/bin/bash
# Example CI script

# Build and test
dotnet build
dotnet test

# Security scan
dotnet run --project src/SecurityScanner.App -- \
  --file production-urls.json \
  --tools headers,ssl \
  --json \
  --output security-report.json

# Check for critical vulnerabilities
if jq -e '.Summary.CriticalVulnerabilities > 0' security-report.json; then
  echo "Critical vulnerabilities found!"
  exit 1
fi
```

### Automated Reporting

```bash
# Generate HTML report
dotnet run --project src/SecurityScanner.App -- report security-report.json --format html

# Email results (if configured)
dotnet run --project src/SecurityScanner.App -- report security-report.json --format email
```

### Bulk Domain Scanning

```bash
# Create URL list
echo '["site1.com", "site2.com", "site3.com"]' > bulk-urls.json

# Scan all domains
dotnet run --project src/SecurityScanner.App -- \
  --file bulk-urls.json \
  --tools headers,ssl \
  --max-concurrent 10 \
  --json \
  --output bulk-results.json
```

## Best Practices

### Security Considerations

1. **Permission Required** - Only scan domains you own or have explicit permission to test
2. **Rate Limiting** - Use appropriate concurrency settings to avoid overwhelming targets
3. **Load Testing** - Be especially cautious with load testing; it can impact production systems

### Performance Optimization

1. **Concurrency** - Adjust `--max-concurrent` based on your system and network capacity
2. **Timeouts** - Set appropriate timeout values for your environment
3. **Scanner Selection** - Use only the scanners you need to reduce scan time

### Automation

1. **JSON Output** - Always use `--json` for automated processing
2. **File Output** - Save results to files for analysis and archiving
3. **Error Handling** - Check the `Success` field in JSON output for error detection

## Troubleshooting

### Common Issues

1. **Connection Timeouts**
   ```bash
   # Increase timeout
   dotnet run --project src/SecurityScanner.App -- example.com --timeout 600 --json
   ```

2. **Rate Limiting**
   ```bash
   # Reduce concurrency
   dotnet run --project src/SecurityScanner.App -- example.com --max-concurrent 1 --json
   ```

3. **SSL Labs API Limits**
   ```bash
   # Check service availability
   dotnet run --project src/SecurityScanner.App -- health --scanners ssl
   ```

### Getting Help

- Use `--help` for command-line options
- Check logs in the `logs/` directory
- Use `--verbose` for detailed output
- Review the [Troubleshooting Guide](troubleshooting.md)

## Next Steps

- Learn about [Configuration Options](configuration.md)
- Explore [Individual Scanner Documentation](scanners/)
- Review [Architecture Documentation](architecture.md) for customization