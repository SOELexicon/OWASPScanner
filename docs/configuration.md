# Configuration Guide

## Configuration Sources

SecurityScanner uses a hierarchical configuration system:

1. **appsettings.json** - Base configuration
2. **appsettings.Development.json** - Development overrides
3. **Environment Variables** - Runtime configuration
4. **Command Line Arguments** - Per-execution overrides

## Configuration Files

### Base Configuration (`appsettings.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ScannerSettings": {
    "OwaspZap": {
      "ApiUrl": "http://localhost:8080",
      "ScanTimeout": "00:30:00",
      "EnableActiveScan": true,
      "EnablePassiveScan": true
    },
    "SslLabs": {
      "ApiUrl": "https://api.ssllabs.com/api/v3",
      "AnalysisTimeout": "00:10:00",
      "FromCache": false,
      "MaxAge": 24
    },
    "SecurityHeaders": {
      "RequestTimeout": "00:00:30",
      "DelayBetweenRequests": "00:00:00.100",
      "MaxRedirects": 5
    },
    "LoadTest": {
      "RequestsPerSecond": 10,
      "DurationSeconds": 60,
      "RampUpSeconds": 0,
      "MaxConcurrentRequests": 50,
      "RequestTimeout": "00:00:30"
    }
  },
  "PerformanceSettings": {
    "MaxConcurrentScans": 3,
    "DefaultTimeout": "00:05:00",
    "EnableRetries": true,
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:01"
  },
  "ReportSettings": {
    "OutputDirectory": "reports",
    "IncludeTimestamp": true,
    "DefaultFormat": "json",
    "CompressLargeReports": true
  }
}
```

### Development Configuration (`appsettings.Development.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "SecurityScanner": "Trace"
    }
  },
  "ScannerSettings": {
    "OwaspZap": {
      "EnableActiveScan": false
    },
    "LoadTest": {
      "RequestsPerSecond": 5,
      "DurationSeconds": 30
    }
  }
}
```

## Scanner Configuration

### OWASP ZAP Settings

```json
{
  "ScannerSettings": {
    "OwaspZap": {
      "ApiUrl": "http://localhost:8080",
      "ApiKey": "",
      "ScanTimeout": "00:30:00",
      "EnableActiveScan": true,
      "EnablePassiveScan": true,
      "ScanPolicy": "Default Policy",
      "UserAgent": "SecurityScanner/1.0"
    }
  }
}
```

**Key Settings:**
- `ApiUrl` - ZAP API endpoint
- `ApiKey` - Authentication key (set via environment variable)
- `ScanTimeout` - Maximum time for scan completion
- `EnableActiveScan` - Enable aggressive scanning (use with caution)
- `EnablePassiveScan` - Enable passive analysis during spidering

### SSL Labs Settings

```json
{
  "ScannerSettings": {
    "SslLabs": {
      "ApiUrl": "https://api.ssllabs.com/api/v3",
      "AnalysisTimeout": "00:10:00",
      "FromCache": false,
      "MaxAge": 24,
      "Publish": false,
      "StartNew": false,
      "All": "done"
    }
  }
}
```

**Key Settings:**
- `ApiUrl` - SSL Labs API endpoint
- `AnalysisTimeout` - Maximum time for analysis
- `FromCache` - Use cached results if available
- `MaxAge` - Maximum age of cached results (hours)
- `Publish` - Publish results to SSL Labs public board

### Security Headers Settings

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

### Load Test Settings

```json
{
  "ScannerSettings": {
    "LoadTest": {
      "RequestsPerSecond": 10,
      "DurationSeconds": 60,
      "RampUpSeconds": 0,
      "MaxConcurrentRequests": 50,
      "RequestTimeout": "00:00:30",
      "WarmupRequests": 5,
      "CooldownSeconds": 5
    }
  }
}
```

## Environment Variables

Use environment variables for sensitive configuration:

```bash
# OWASP ZAP
export SCANNER_OwaspZap__ApiKey="your-api-key"
export SCANNER_OwaspZap__ApiUrl="http://zap-server:8080"

# SSL Labs
export SCANNER_SslLabs__ApiKey="your-ssl-labs-key"

# Performance
export SCANNER_PerformanceSettings__MaxConcurrentScans=5

# Logging
export SCANNER_Logging__LogLevel__Default="Warning"
```

**Environment Variable Format:**
- Prefix: `SCANNER_`
- Hierarchy: Use `__` (double underscore) for nested properties
- Example: `SCANNER_ScannerSettings__OwaspZap__ApiKey`

## Performance Configuration

### Concurrent Scan Limits

```json
{
  "PerformanceSettings": {
    "MaxConcurrentScans": 3,
    "DefaultTimeout": "00:05:00",
    "EnableRetries": true,
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:01"
  }
}
```

### HTTP Client Configuration

```json
{
  "HttpClientSettings": {
    "DefaultTimeout": "00:01:00",
    "MaxConnectionsPerServer": 10,
    "PooledConnectionLifetime": "00:10:00",
    "EnableCookies": false,
    "EnableAutomaticDecompression": true
  }
}
```

## Logging Configuration

### Basic Logging Setup

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SecurityScanner": "Debug",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

### File Logging

```json
{
  "LoggingSettings": {
    "LogLevel": "Information",
    "LogToFile": true,
    "LogDirectory": "logs",
    "IncludeScopes": true,
    "MaxLogFileSizeMB": 10,
    "MaxLogFiles": 5,
    "LogFormat": "json"
  }
}
```

## Report Configuration

### Report Settings

```json
{
  "ReportSettings": {
    "OutputDirectory": "reports",
    "IncludeTimestamp": true,
    "DefaultFormat": "json",
    "CompressLargeReports": true,
    "MaxReportSizeMB": 50,
    "ArchiveOldReports": true,
    "RetentionDays": 30
  }
}
```

### Notification Settings

```json
{
  "NotificationSettings": {
    "Enabled": true,
    "Email": {
      "Enabled": true,
      "SmtpServer": "smtp.company.com",
      "SmtpPort": 587,
      "UseSsl": true,
      "FromAddress": "security@company.com",
      "ToAddresses": ["admin@company.com"],
      "SubjectPrefix": "[SecurityScanner]"
    },
    "Slack": {
      "Enabled": false,
      "WebhookUrl": "https://hooks.slack.com/...",
      "Channel": "#security",
      "Username": "SecurityScanner"
    }
  }
}
```

## Command Line Overrides

Command line arguments override configuration file settings:

```bash
# Override concurrent scan limit
dotnet run --project src/SecurityScanner.App -- example.com --max-concurrent 10

# Override timeout
dotnet run --project src/SecurityScanner.App -- example.com --timeout 300

# Override load test settings
dotnet run --project src/SecurityScanner.App -- example.com \
  --tools loadtest \
  --loadtest-rps 20 \
  --loadtest-duration 120
```

## Configuration Validation

### Validate Configuration

```bash
# Check configuration validity
dotnet run --project src/SecurityScanner.App -- config validate

# Show current configuration
dotnet run --project src/SecurityScanner.App -- config show

# Show specific section
dotnet run --project src/SecurityScanner.App -- config show --section ScannerSettings
```

### Health Checks

```bash
# Check all scanner services
dotnet run --project src/SecurityScanner.App -- health

# Check specific scanners
dotnet run --project src/SecurityScanner.App -- health --scanners zap,ssl
```

## Security Considerations

### Sensitive Data

- **Never commit API keys** to version control
- **Use environment variables** for sensitive configuration
- **Encrypt configuration files** in production environments
- **Limit API key permissions** to minimum required scope

### Network Security

- **Use HTTPS** for all external API calls
- **Validate SSL certificates** in production
- **Configure firewall rules** for ZAP integration
- **Use VPN or private networks** for internal scanning

## Best Practices

1. **Environment-Specific Configuration**
   - Use separate configuration files for each environment
   - Override sensitive settings with environment variables

2. **Performance Tuning**
   - Adjust concurrency based on target capacity
   - Set appropriate timeouts for your network
   - Monitor resource usage during scans

3. **Security**
   - Rotate API keys regularly
   - Use least-privilege access principles
   - Monitor scan logs for suspicious activity

4. **Maintenance**
   - Regular configuration reviews
   - Update timeout values based on performance
   - Archive old reports and logs

## Troubleshooting Configuration

### Common Issues

1. **Invalid JSON**
   ```bash
   # Validate JSON syntax
   python -m json.tool appsettings.json
   ```

2. **Missing Environment Variables**
   ```bash
   # Check environment variables
   printenv | grep SCANNER_
   ```

3. **Service Connectivity**
   ```bash
   # Test ZAP connectivity
   curl http://localhost:8080/JSON/core/version/
   ```

### Configuration Debugging

Use configuration validation commands:

```bash
# Show resolved configuration
dotnet run --project src/SecurityScanner.App -- config show --verbose

# Validate all settings
dotnet run --project src/SecurityScanner.App -- config validate --verbose
```

## Next Steps

- Review [User Guide](user-guide.md) for command-line usage
- Check [Scanner Documentation](scanners/) for specific scanner configuration
- See [Troubleshooting Guide](troubleshooting.md) for common issues