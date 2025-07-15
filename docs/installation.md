# Installation Guide

## Prerequisites

### Required Software

- **.NET 9 SDK** - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Git** - For cloning the repository
- **Visual Studio 2022** or **VS Code** (recommended for development)

### Optional External Tools

- **OWASP ZAP** - For advanced vulnerability scanning
  - Download from [zaproxy.org](https://www.zaproxy.org/)
  - Enable API access (default port: 8080)
- **SSL Labs API** - No installation required (cloud service)

## Installation Steps

### 1. Clone the Repository

```bash
git clone <repository-url>
cd SecurityScanner
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Project

```bash
dotnet build
```

### 4. Verify Installation

```bash
dotnet run --project src/SecurityScanner.App -- --help
```

Expected output:
```
SecurityScanner - Automated security scanning tool for web domains

Usage:
  SecurityScanner.App [<domains>...] [command] [options]
```

## Configuration

### 1. Basic Configuration

The application works out-of-the-box with default settings. Configuration is stored in `src/SecurityScanner.App/appsettings.json`.

### 2. OWASP ZAP Setup (Optional)

If you want to use OWASP ZAP scanning:

1. Install OWASP ZAP
2. Start ZAP with API enabled:
   ```bash
   zap.sh -daemon -port 8080 -config api.addrs.addr.name=* -config api.addrs.addr.regex=true
   ```
3. Update configuration:
   ```json
   {
     "ScannerSettings": {
       "OwaspZap": {
         "ApiUrl": "http://localhost:8080",
         "EnableActiveScan": true,
         "EnablePassiveScan": true
       }
     }
   }
   ```

### 3. Environment Variables

For production deployments, configure sensitive settings via environment variables:

```bash
export SCANNER_OwaspZap__ApiKey="your-api-key"
export SCANNER_SslLabs__ApiKey="your-ssl-labs-key"
```

## Deployment

### Self-Contained Deployment

```bash
dotnet publish src/SecurityScanner.App -c Release -r win-x64 --self-contained -o publish/
```

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "SecurityScanner.App.dll"]
```

## Troubleshooting

### Common Issues

1. **Build Errors**
   - Ensure .NET 9 SDK is installed
   - Run `dotnet --version` to verify

2. **Permission Errors**
   - Ensure proper file permissions
   - Run as administrator if needed on Windows

3. **Network Issues**
   - Check firewall settings
   - Verify internet connectivity for SSL Labs API

### Getting Help

1. Check the [Troubleshooting Guide](troubleshooting.md)
2. Review application logs in the `logs/` directory
3. Use `--verbose` flag for detailed output

## Next Steps

- Review the [User Guide](user-guide.md) for usage examples
- Configure settings in the [Configuration Guide](configuration.md)
- Explore scanner options in [Scanner Types](scanners/)