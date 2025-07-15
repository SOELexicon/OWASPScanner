# Troubleshooting Guide

## Common Issues and Solutions

### Build and Installation Issues

#### .NET SDK Issues

**Problem**: Build fails with version errors
```
error NETSDK1045: The current .NET SDK does not support targeting .NET 9.0
```

**Solution**:
```bash
# Check .NET version
dotnet --version

# Install .NET 9 SDK
# Download from https://dotnet.microsoft.com/download/dotnet/9.0

# Verify installation
dotnet --list-sdks
```

#### Dependency Resolution Issues

**Problem**: Package restore fails
```
error NU1101: Unable to find package
```

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# If still failing, check NuGet sources
dotnet nuget list source
```

#### Build Configuration Issues

**Problem**: Build fails in specific configuration
```
error CS0246: The type or namespace name could not be found
```

**Solution**:
```bash
# Clean solution
dotnet clean

# Rebuild
dotnet build --configuration Release

# Check project references
dotnet list reference
```

### Runtime Issues

#### Command Line Parsing

**Problem**: Invalid command line arguments
```
Required command was not provided
```

**Solution**:
```bash
# Check command syntax
dotnet run --project src/SecurityScanner.App -- --help

# Correct usage examples
dotnet run --project src/SecurityScanner.App -- example.com --json
dotnet run --project src/SecurityScanner.App -- --file urls.json --json
```

#### Configuration Issues

**Problem**: Configuration not loading
```
System.ArgumentNullException: Value cannot be null. (Parameter 'configuration')
```

**Solution**:
```bash
# Check configuration file exists
ls src/SecurityScanner.App/appsettings.json

# Validate JSON syntax
python -m json.tool src/SecurityScanner.App/appsettings.json

# Check environment variables
printenv | grep SCANNER_
```

#### JSON Output Issues

**Problem**: Invalid JSON output
```
Unexpected character encountered while parsing value
```

**Solution**:
```bash
# Use --json flag explicitly
dotnet run --project src/SecurityScanner.App -- example.com --json

# Validate JSON output
dotnet run --project src/SecurityScanner.App -- example.com --json | jq .

# Check for error messages in output
dotnet run --project src/SecurityScanner.App -- example.com --json --verbose
```

### Scanner-Specific Issues

#### Security Headers Scanner

**Problem**: Connection timeouts
```
The operation was canceled due to timeout
```

**Solution**:
```bash
# Increase timeout
dotnet run --project src/SecurityScanner.App -- example.com --timeout 120

# Check network connectivity
curl -I https://example.com

# Reduce concurrency
dotnet run --project src/SecurityScanner.App -- example.com --max-concurrent 1
```

**Problem**: SSL certificate errors
```
The remote certificate is invalid according to the validation procedure
```

**Solution**:
```json
{
  "ScannerSettings": {
    "SecurityHeaders": {
      "ValidateCertificates": false
    }
  }
}
```

#### SSL Labs Scanner

**Problem**: API rate limiting
```
SSL Labs API rate limit exceeded
```

**Solution**:
```bash
# Check API status
curl "https://api.ssllabs.com/api/v3/info"

# Reduce scan frequency
dotnet run --project src/SecurityScanner.App -- example.com --tools headers

# Check rate limit configuration
dotnet run --project src/SecurityScanner.App -- config show --section ScannerSettings.SslLabs
```

**Problem**: Analysis timeout
```
SSL Labs analysis timeout
```

**Solution**:
```json
{
  "ScannerSettings": {
    "SslLabs": {
      "AnalysisTimeout": "00:20:00",
      "FromCache": true
    }
  }
}
```

#### OWASP ZAP Scanner

**Problem**: ZAP service unavailable
```
Unable to connect to ZAP API
```

**Solution**:
```bash
# Check ZAP is running
curl http://localhost:8080/JSON/core/version/

# Start ZAP daemon
zap.sh -daemon -port 8080

# Check ZAP configuration
dotnet run --project src/SecurityScanner.App -- health --scanners zap
```

**Problem**: ZAP scan timeout
```
ZAP scan timed out
```

**Solution**:
```json
{
  "ScannerSettings": {
    "OwaspZap": {
      "ScanTimeout": "00:60:00",
      "EnableActiveScan": false
    }
  }
}
```

#### Load Test Scanner

**Problem**: Target server overwhelmed
```
High error rate during load testing
```

**Solution**:
```bash
# Reduce load test intensity
dotnet run --project src/SecurityScanner.App -- example.com \
  --tools loadtest \
  --loadtest-rps 5 \
  --loadtest-duration 30

# Add ramp-up time
dotnet run --project src/SecurityScanner.App -- example.com \
  --tools loadtest \
  --loadtest-rps 10 \
  --loadtest-rampup 30
```

### File Input Issues

#### JSON File Problems

**Problem**: File not found
```
Could not find file 'urls.json'
```

**Solution**:
```bash
# Check file exists
ls -la urls.json

# Use absolute path
dotnet run --project src/SecurityScanner.App -- --file /full/path/to/urls.json --json

# Check file permissions
chmod 644 urls.json
```

**Problem**: Invalid JSON format
```
Invalid JSON in input file
```

**Solution**:
```bash
# Validate JSON syntax
python -m json.tool urls.json

# Correct format example
echo '["example.com", "test.com"]' > urls.json

# Check for hidden characters
cat -v urls.json
```

### Performance Issues

#### Memory Usage

**Problem**: High memory consumption
```
System.OutOfMemoryException
```

**Solution**:
```bash
# Reduce concurrent scans
dotnet run --project src/SecurityScanner.App -- example.com --max-concurrent 1

# Monitor memory usage
dotnet run --project src/SecurityScanner.App -- example.com --verbose
```

#### Slow Performance

**Problem**: Scans taking too long
```
Scan duration exceeded expectations
```

**Solution**:
```bash
# Profile individual scanners
dotnet run --project src/SecurityScanner.App -- example.com --tools headers --verbose

# Optimize scanner selection
dotnet run --project src/SecurityScanner.App -- example.com --tools headers,ssl

# Check network latency
ping example.com
```

### Output and Reporting Issues

#### File Output Problems

**Problem**: Cannot write to output file
```
Access to the path is denied
```

**Solution**:
```bash
# Check directory permissions
ls -la reports/

# Create output directory
mkdir -p reports

# Use different output path
dotnet run --project src/SecurityScanner.App -- example.com --json --output /tmp/results.json
```

#### Report Generation Issues

**Problem**: Report generation fails
```
Report generation failed
```

**Solution**:
```bash
# Check report command syntax
dotnet run --project src/SecurityScanner.App -- report --help

# Validate input file
dotnet run --project src/SecurityScanner.App -- report results.json --format json

# Check report configuration
dotnet run --project src/SecurityScanner.App -- config show --section ReportSettings
```

### Logging and Debugging

#### Enable Verbose Logging

```bash
# Enable verbose output
dotnet run --project src/SecurityScanner.App -- example.com --verbose

# Enable debug logging
export SCANNER_Logging__LogLevel__Default=Debug
dotnet run --project src/SecurityScanner.App -- example.com
```

#### Check Log Files

```bash
# View application logs
tail -f logs/SecurityScanner.log

# View specific scanner logs
grep "SecurityHeaders" logs/SecurityScanner.log

# Check for errors
grep "ERROR" logs/SecurityScanner.log
```

### Network and Connectivity Issues

#### Proxy Configuration

**Problem**: Requests fail behind corporate proxy
```
The remote name could not be resolved
```

**Solution**:
```bash
# Set proxy environment variables
export HTTP_PROXY=http://proxy.company.com:8080
export HTTPS_PROXY=http://proxy.company.com:8080

# Or configure in appsettings.json
```

```json
{
  "HttpClientSettings": {
    "Proxy": {
      "Address": "http://proxy.company.com:8080",
      "Username": "user",
      "Password": "pass"
    }
  }
}
```

#### DNS Issues

**Problem**: Domain resolution failures
```
No such host is known
```

**Solution**:
```bash
# Test DNS resolution
nslookup example.com

# Try alternative DNS
export SCANNER_DnsServer=8.8.8.8

# Use IP address instead
dotnet run --project src/SecurityScanner.App -- 93.184.216.34 --json
```

### Testing and Development Issues

#### Unit Test Failures

**Problem**: Tests fail intermittently
```
Test failed with timeout
```

**Solution**:
```bash
# Run tests with verbose output
dotnet test --verbosity detailed

# Run specific test
dotnet test --filter "FullyQualifiedName~TestName"

# Increase test timeout
dotnet test -- MSTest.TestTimeout=60000
```

#### Integration Test Issues

**Problem**: Integration tests fail
```
External service unavailable
```

**Solution**:
```bash
# Check service availability
dotnet run --project src/SecurityScanner.App -- health

# Use mock services for testing
export SCANNER_UseMockServices=true
dotnet test
```

## Diagnostic Commands

### Health Checks

```bash
# Check all services
dotnet run --project src/SecurityScanner.App -- health

# Check specific scanner
dotnet run --project src/SecurityScanner.App -- health --scanners ssl

# Verbose health check
dotnet run --project src/SecurityScanner.App -- health --verbose
```

### Configuration Validation

```bash
# Show current configuration
dotnet run --project src/SecurityScanner.App -- config show

# Validate configuration
dotnet run --project src/SecurityScanner.App -- config validate

# Show specific section
dotnet run --project src/SecurityScanner.App -- config show --section ScannerSettings
```

### Network Diagnostics

```bash
# Test connectivity
curl -I https://example.com

# Check SSL Labs API
curl "https://api.ssllabs.com/api/v3/info"

# Test ZAP API
curl http://localhost:8080/JSON/core/version/
```

## Performance Monitoring

### Resource Usage

```bash
# Monitor during scan
dotnet run --project src/SecurityScanner.App -- example.com --verbose &
top -p $!

# Memory profiling
dotnet run --project src/SecurityScanner.App -- example.com --json | while read line; do echo "$(date): $line"; done
```

### Timing Analysis

```bash
# Time individual operations
time dotnet run --project src/SecurityScanner.App -- example.com --tools headers --json

# Profile scanner performance
dotnet run --project src/SecurityScanner.App -- example.com --tools headers --verbose 2>&1 | grep "Completed.*scan"
```

## Getting Help

### Log Collection

When reporting issues, include:

1. **Command used**:
   ```bash
   dotnet run --project src/SecurityScanner.App -- example.com --json --verbose
   ```

2. **Environment information**:
   ```bash
   dotnet --version
   uname -a
   ```

3. **Configuration** (sanitized):
   ```bash
   dotnet run --project src/SecurityScanner.App -- config show
   ```

4. **Error logs**:
   ```bash
   tail -100 logs/SecurityScanner.log
   ```

### Support Channels

1. **Documentation**: Check relevant documentation sections
2. **Issues**: Search existing issues for similar problems
3. **Logs**: Enable verbose logging for detailed diagnostics
4. **Community**: Engage with the community for help

### Creating Bug Reports

Include:
- Clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Environment details
- Relevant logs and configuration
- Screenshots if applicable

## Prevention Tips

### Best Practices

1. **Regular Updates**
   ```bash
   dotnet list package --outdated
   dotnet add package PackageName
   ```

2. **Configuration Management**
   - Use environment variables for sensitive data
   - Validate configuration after changes
   - Document configuration requirements

3. **Monitoring**
   - Set up health checks
   - Monitor resource usage
   - Track scan success rates

4. **Testing**
   - Test configuration changes
   - Validate in staging environment
   - Use CI/CD for automated testing

### Maintenance

1. **Regular Cleanup**
   ```bash
   # Clean old logs
   find logs/ -name "*.log" -mtime +30 -delete
   
   # Clean old reports
   find reports/ -name "*.json" -mtime +7 -delete
   ```

2. **Performance Monitoring**
   - Monitor scan duration trends
   - Track error rates
   - Analyze resource usage patterns

3. **Security**
   - Rotate API keys regularly
   - Review access logs
   - Update dependencies

This troubleshooting guide should help resolve most common issues. For persistent problems, please consult the relevant documentation sections or seek community support.