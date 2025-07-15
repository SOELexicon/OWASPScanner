# SecurityScanner Documentation

A comprehensive security scanning tool for web applications and domains.

## Quick Start

```bash
# Build the project
dotnet build

# Scan a single domain
dotnet run --project src/SecurityScanner.App -- example.com --json

# Scan multiple domains from file
dotnet run --project src/SecurityScanner.App -- --file urls.json --json
```

## Documentation Structure

- **[Installation Guide](installation.md)** - Setup and prerequisites
- **[User Guide](user-guide.md)** - Command-line usage and examples
- **[Configuration](configuration.md)** - Configuration options and settings
- **[Architecture](architecture.md)** - Technical architecture and design
- **[Scanner Types](scanners/)** - Individual scanner documentation
- **[API Reference](api-reference.md)** - Core interfaces and models
- **[Development](development.md)** - Development workflow and contribution guide
- **[Troubleshooting](troubleshooting.md)** - Common issues and solutions

## Features

- **Security Headers Analysis** - Checks for missing security headers
- **SSL/TLS Certificate Analysis** - Integration with SSL Labs API
- **OWASP ZAP Integration** - Automated vulnerability scanning
- **Load Testing** - Performance analysis and bottleneck detection
- **JSON File Input** - Bulk scanning from URL lists
- **Multiple Output Formats** - JSON, HTML, and text reports

## Security Notice

⚠️ **Important**: Only scan domains you own or have explicit permission to test. Unauthorized scanning may violate terms of service and local laws.

## Support

For issues, questions, or contributions, please refer to the individual documentation sections or the project's issue tracker.