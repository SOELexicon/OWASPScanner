# SecurityScanner

[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/your-org/security-scanner)

A comprehensive, enterprise-grade security scanning tool for web applications and domains. Built with .NET 9 and designed for CI/CD integration, SecurityScanner provides automated vulnerability detection, SSL/TLS analysis, security header validation, and performance testing.

## ✨ Features

### 🔍 **Multi-Scanner Support**
- **Security Headers Analysis** - Validates HTTP security headers (HSTS, CSP, X-Frame-Options, etc.)
- **SSL/TLS Certificate Analysis** - Integration with SSL Labs API for comprehensive certificate validation
- **OWASP ZAP Integration** - Automated vulnerability scanning with industry-standard tools
- **Load Testing** - Performance analysis with bottleneck detection and vulnerability reporting

### 🚀 **Enterprise Ready**
- **JSON File Input** - Bulk scanning from URL lists for large-scale operations
- **Concurrent Scanning** - Configurable parallel execution for optimal performance
- **Multiple Output Formats** - JSON, HTML, and text reports for different use cases
- **CI/CD Integration** - Designed for automated pipelines and continuous security monitoring

### 🛠️ **Developer Friendly**
- **Clean Architecture** - Modular design with clear separation of concerns
- **Extensible** - Easy to add new scanners and customize behavior
- **Comprehensive Logging** - Detailed logging and diagnostic capabilities
- **Health Checks** - Built-in service availability monitoring

## 🚀 Quick Start

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Git (for cloning the repository)

### Installation

```bash
# Clone the repository
git clone https://github.com/your-org/security-scanner.git
cd security-scanner

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run a basic scan
dotnet run --project src/SecurityScanner.App -- example.com --json
```

### Basic Usage

```bash
# Scan a single domain
dotnet run --project src/SecurityScanner.App -- example.com

# Multiple domains with JSON output
dotnet run --project src/SecurityScanner.App -- example.com,api.example.com --json

# Scan from JSON file
dotnet run --project src/SecurityScanner.App -- --file urls.json --json

# Use specific scanners
dotnet run --project src/SecurityScanner.App -- example.com --tools headers,ssl,zap --json

# Save results to file
dotnet run --project src/SecurityScanner.App -- example.com --json --output results.json
```

### JSON File Input

Create a `urls.json` file:
```json
[
  "example.com",
  "api.example.com",
  "secure.example.com"
]
```

Then scan all URLs:
```bash
dotnet run --project src/SecurityScanner.App -- --file urls.json --json
```

## 📊 Sample Output

### JSON Output
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
          "Solution": "Add the Strict-Transport-Security header..."
        }
      ],
      "VulnerabilityCount": 1
    }
  ],
  "Summary": {
    "TotalDomains": 1,
    "SuccessfulScans": 1,
    "TotalVulnerabilities": 1,
    "CriticalVulnerabilities": 0,
    "HighVulnerabilities": 0
  },
  "Success": true
}
```

### Text Output
```
=== Security Scan Results ===
Status: SUCCESS
Total Domains: 1
Total Vulnerabilities: 1

Domain: example.com
Scanner: Security Headers
Vulnerabilities: 1
  - Missing HSTS Header (Medium)
    The Strict-Transport-Security header is missing...
```

## 🔧 Available Scanners

| Scanner | Description | Speed | External Dependency |
|---------|-------------|-------|-------------------|
| **headers** | Security headers analysis | Fast | None |
| **ssl** | SSL/TLS certificate validation | Medium | SSL Labs API |
| **zap** | OWASP ZAP vulnerability scanning | Slow | OWASP ZAP |
| **loadtest** | Performance and load testing | Variable | None |

## 🏗️ Architecture

SecurityScanner follows Clean Architecture principles:

```
┌─────────────────────────────────────────────────────────────┐
│                    SecurityScanner.App                     │
│                  (Console Application)                     │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────┼───────────────────────────────────┐
│              SecurityScanner.Commands                      │
│         (CLI Handling & Request Processing)                │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────┼───────────────────────────────────┐
│              SecurityScanner.Services                      │
│          (Scanner Implementations & Orchestration)         │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────┼───────────────────────────────────┐
│           SecurityScanner.Infrastructure                   │
│        (HTTP Clients, Logging, Configuration)              │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────┼───────────────────────────────────┐
│                SecurityScanner.Core                        │
│         (Domain Models, Interfaces, Enums)                 │
└─────────────────────────────────────────────────────────────┘
```

## 📚 Documentation

### Getting Started
- **[Installation Guide](docs/installation.md)** - Setup instructions and prerequisites
- **[User Guide](docs/user-guide.md)** - Comprehensive usage examples and command reference
- **[Configuration](docs/configuration.md)** - Configuration options and environment variables

### Advanced Usage
- **[Scanner Documentation](docs/scanners/)** - Detailed information about each scanner type
- **[API Reference](docs/api-reference.md)** - Complete interface and model documentation
- **[Architecture](docs/architecture.md)** - Technical architecture and design patterns

### Development
- **[Development Guide](docs/development.md)** - How to contribute and extend the tool
- **[Troubleshooting](docs/troubleshooting.md)** - Common issues and solutions

## 🔒 Security & Ethics

⚠️ **Important Security Notice**

- **Permission Required**: Only scan domains you own or have explicit written permission to test
- **Responsible Use**: This tool is designed for defensive security testing and compliance validation
- **Load Testing**: Exercise extreme caution with load testing - it can impact production systems
- **Rate Limiting**: Respect service limits and implement appropriate delays between requests

## 🧪 CI/CD Integration

### GitHub Actions Example

```yaml
name: Security Scan

on:
  schedule:
    - cron: '0 2 * * *'  # Daily at 2 AM
  push:
    branches: [ main ]

jobs:
  security-scan:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Security Scan
      run: |
        dotnet run --project src/SecurityScanner.App -- \
          --file production-urls.json \
          --tools headers,ssl \
          --json \
          --output security-report.json
    
    - name: Check Results
      run: |
        if jq -e '.Summary.CriticalVulnerabilities > 0' security-report.json; then
          echo "Critical vulnerabilities found!"
          exit 1
        fi
```

### Jenkins Pipeline Example

```groovy
pipeline {
    agent any
    
    stages {
        stage('Security Scan') {
            steps {
                script {
                    sh '''
                        dotnet run --project src/SecurityScanner.App -- \
                          --file urls.json \
                          --tools headers,ssl \
                          --json \
                          --output security-report.json
                    '''
                    
                    def report = readJSON file: 'security-report.json'
                    if (report.Summary.CriticalVulnerabilities > 0) {
                        error "Critical vulnerabilities found!"
                    }
                }
            }
        }
    }
    
    post {
        always {
            archiveArtifacts artifacts: 'security-report.json'
        }
    }
}
```

## 🚀 Advanced Configuration

### Environment Variables

```bash
# OWASP ZAP Configuration
export SCANNER_OwaspZap__ApiUrl="http://zap-server:8080"
export SCANNER_OwaspZap__ApiKey="your-api-key"

# SSL Labs Configuration
export SCANNER_SslLabs__ApiKey="your-ssl-labs-key"

# Performance Settings
export SCANNER_PerformanceSettings__MaxConcurrentScans=10
export SCANNER_PerformanceSettings__DefaultTimeout="00:10:00"
```

### Configuration File

```json
{
  "ScannerSettings": {
    "SecurityHeaders": {
      "RequestTimeout": "00:00:30",
      "MaxRedirects": 5
    },
    "SslLabs": {
      "ApiUrl": "https://api.ssllabs.com/api/v3",
      "AnalysisTimeout": "00:10:00"
    },
    "OwaspZap": {
      "ApiUrl": "http://localhost:8080",
      "EnableActiveScan": true
    },
    "LoadTest": {
      "RequestsPerSecond": 10,
      "DurationSeconds": 60
    }
  }
}
```

## 🛠️ Development

### Adding a New Scanner

1. **Implement IScannerService**
   ```csharp
   public class CustomScannerService : IScannerService
   {
       public string ServiceName => "Custom Scanner";
       public ScannerType ScannerType => ScannerType.Custom;
       
       public async Task<ScanResult> ScanAsync(DomainConfig domain, CancellationToken cancellationToken)
       {
           // Implementation
       }
   }
   ```

2. **Register in DI Container**
   ```csharp
   services.AddTransient<IScannerService, CustomScannerService>();
   ```

3. **Add to Command Line Parser**
   ```csharp
   "custom" => ScannerType.Custom,
   ```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/SecurityScanner.Services.Tests/
```

### Building for Production

```bash
# Build release version
dotnet build -c Release

# Publish self-contained
dotnet publish src/SecurityScanner.App -c Release -r linux-x64 --self-contained -o publish/

# Run published version
./publish/SecurityScanner.App example.com --json
```

## 🔍 Common Use Cases

### 1. Automated Security Monitoring
```bash
# Daily security scan of production domains
dotnet run --project src/SecurityScanner.App -- \
  --file production-domains.json \
  --tools headers,ssl \
  --json \
  --output "security-$(date +%Y%m%d).json"
```

### 2. Pre-Deployment Testing
```bash
# Scan staging environment before deployment
dotnet run --project src/SecurityScanner.App -- \
  staging.example.com \
  --tools headers,ssl,zap \
  --json \
  --output staging-security-report.json
```

### 3. Compliance Validation
```bash
# Check SSL configuration compliance
dotnet run --project src/SecurityScanner.App -- \
  --file compliance-domains.json \
  --tools ssl \
  --json \
  --output ssl-compliance-report.json
```

### 4. Performance Analysis
```bash
# Load test with performance analysis
dotnet run --project src/SecurityScanner.App -- \
  api.example.com \
  --tools loadtest \
  --loadtest-rps 20 \
  --loadtest-duration 300 \
  --json \
  --output performance-report.json
```

## 🐛 Troubleshooting

### Common Issues

1. **Connection Timeouts**
   ```bash
   # Increase timeout
   dotnet run --project src/SecurityScanner.App -- example.com --timeout 120
   ```

2. **SSL Certificate Errors**
   ```bash
   # Skip certificate validation (development only)
   export SCANNER_SecurityHeaders__ValidateCertificates=false
   ```

3. **Rate Limiting**
   ```bash
   # Reduce concurrency
   dotnet run --project src/SecurityScanner.App -- example.com --max-concurrent 1
   ```

### Getting Help

- **Documentation**: Check the comprehensive [documentation](docs/)
- **Issues**: Search existing issues before creating new ones
- **Logs**: Enable verbose logging with `--verbose` flag
- **Health Check**: Use `dotnet run --project src/SecurityScanner.App -- health` to check service availability

## 📈 Performance Metrics

### Benchmark Results
- **Security Headers**: ~100 domains/minute
- **SSL Labs**: ~20 domains/minute (API rate limited)
- **OWASP ZAP**: ~1-5 domains/minute (depends on scan depth)
- **Load Testing**: Variable (depends on target capacity)

### Optimization Tips
- Use `--max-concurrent` to adjust parallelism
- Combine multiple scanners in single run for efficiency
- Use JSON file input for bulk operations
- Configure appropriate timeouts for your network

## 🤝 Contributing

We welcome contributions! Please see our [Development Guide](docs/development.md) for details on:

- Setting up the development environment
- Code style and standards
- Adding new scanners
- Submitting pull requests
- Running tests

### Code of Conduct

This project adheres to ethical security testing practices:
- Only scan systems you own or have permission to test
- Respect rate limits and service capacity
- Report vulnerabilities responsibly
- Use the tool for defensive security purposes

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [OWASP ZAP](https://www.zaproxy.org/) for vulnerability scanning capabilities
- [SSL Labs](https://www.ssllabs.com/) for SSL/TLS analysis API
- [.NET Foundation](https://dotnetfoundation.org/) for the excellent .NET platform
- Security community for best practices and vulnerability research

## 📞 Support

- **Documentation**: [docs/](docs/)
- **Issues**: [GitHub Issues](https://github.com/your-org/security-scanner/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-org/security-scanner/discussions)
- **Security**: Report security issues privately to security@your-org.com

---

**Made with ❤️ for the security community**