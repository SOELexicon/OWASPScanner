# Development Guide

## Getting Started

### Prerequisites

- .NET 9 SDK
- Git
- Visual Studio 2022 or VS Code
- OWASP ZAP (optional, for ZAP integration testing)

### Setup

1. **Clone Repository**
   ```bash
   git clone <repository-url>
   cd SecurityScanner
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Build Solution**
   ```bash
   dotnet build
   ```

4. **Run Tests**
   ```bash
   dotnet test
   ```

## Project Structure

```
SecurityScanner/
├── src/
│   ├── SecurityScanner.Core/          # Domain models and interfaces
│   ├── SecurityScanner.Infrastructure/ # External dependencies
│   ├── SecurityScanner.Services/      # Business logic
│   ├── SecurityScanner.Commands/      # CLI command handling
│   └── SecurityScanner.App/           # Application entry point
├── tests/
│   ├── SecurityScanner.Core.Tests/
│   ├── SecurityScanner.Services.Tests/
│   └── SecurityScanner.Integration.Tests/
├── docs/                              # Documentation
└── scripts/                           # Build and deployment scripts
```

## Development Workflow

### 1. Feature Development

```bash
# Create feature branch
git checkout -b feature/new-scanner

# Make changes
# ... code changes ...

# Build and test
dotnet build
dotnet test

# Run manual tests
dotnet run --project src/SecurityScanner.App -- example.com --json
```

### 2. Code Quality

```bash
# Format code
dotnet format

# Analyze code
dotnet build --verbosity normal

# Run security analysis
dotnet list package --vulnerable
```

### 3. Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/SecurityScanner.Core.Tests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Adding New Scanners

### 1. Create Scanner Service

```csharp
// src/SecurityScanner.Services/Scanners/CustomScannerService.cs
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
            // Implement scanning logic
            var vulnerabilities = await PerformScanAsync(domain.Url, cancellationToken);
            result.Vulnerabilities = vulnerabilities;
            result.Status = ScanStatus.Completed;
        }
        catch (Exception ex)
        {
            result.Status = ScanStatus.Failed;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Custom scan failed for {Domain}", domain.Name);
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        // Check service availability
        return true;
    }

    public async Task<string> GetServiceVersionAsync()
    {
        return "Custom Scanner v1.0";
    }

    private async Task<List<VulnerabilityReport>> PerformScanAsync(string url, CancellationToken cancellationToken)
    {
        // Implement actual scanning logic
        return new List<VulnerabilityReport>();
    }
}
```

### 2. Add Scanner Type Enum

```csharp
// src/SecurityScanner.Core/Enums/ScannerType.cs
public enum ScannerType
{
    SecurityHeaders = 1,
    SslLabs = 2,
    OwaspZap = 3,
    LoadTest = 4,
    Custom = 5  // Add new scanner type
}
```

### 3. Register Scanner Service

```csharp
// src/SecurityScanner.App/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddSecurityScannerServices(this IServiceCollection services, IConfiguration configuration)
{
    // ... existing registrations ...
    
    // Register new scanner
    services.AddTransient<IScannerService, CustomScannerService>();
    
    return services;
}
```

### 4. Add Command Line Support

```csharp
// src/SecurityScanner.Commands/Parsing/CommandLineParser.cs
var scannerType = toolName switch
{
    "headers" => ScannerType.SecurityHeaders,
    "ssl" => ScannerType.SslLabs,
    "zap" => ScannerType.OwaspZap,
    "loadtest" => ScannerType.LoadTest,
    "custom" => ScannerType.Custom,  // Add new tool
    _ => throw new ArgumentException($"Unknown scanner tool: {toolName}")
};
```

### 5. Add Configuration

```json
// src/SecurityScanner.App/appsettings.json
{
  "ScannerSettings": {
    "Custom": {
      "ApiUrl": "https://api.example.com",
      "RequestTimeout": "00:01:00",
      "ApiKey": ""
    }
  }
}
```

### 6. Create Unit Tests

```csharp
// tests/SecurityScanner.Services.Tests/Scanners/CustomScannerServiceTests.cs
[TestFixture]
public class CustomScannerServiceTests
{
    private Mock<IHttpClientFactory> _httpClientFactoryMock;
    private Mock<ILogger<CustomScannerService>> _loggerMock;
    private CustomScannerService _service;

    [SetUp]
    public void Setup()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<CustomScannerService>>();
        _service = new CustomScannerService(_httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task ScanAsync_ValidDomain_ReturnsScanResult()
    {
        // Arrange
        var domain = new DomainConfig { Name = "example.com", Url = "https://example.com" };

        // Act
        var result = await _service.ScanAsync(domain);

        // Assert
        Assert.That(result.Domain, Is.EqualTo("example.com"));
        Assert.That(result.Status, Is.EqualTo(ScanStatus.Completed));
    }
}
```

## Adding New Commands

### 1. Create Command Models

```csharp
// src/SecurityScanner.Commands/Models/CustomCommandRequest.cs
public class CustomCommandRequest : CommandRequest
{
    public string Parameter1 { get; set; } = string.Empty;
    public int Parameter2 { get; set; }
}

// src/SecurityScanner.Commands/Models/CustomCommandResponse.cs
public class CustomCommandResponse : CommandResponse
{
    public string Result { get; set; } = string.Empty;
}
```

### 2. Create Command Handler

```csharp
// src/SecurityScanner.Commands/Handlers/CustomCommandHandler.cs
public class CustomCommandHandler : ICommandHandler<CustomCommandRequest, CustomCommandResponse>
{
    private readonly ILogger<CustomCommandHandler> _logger;

    public CustomCommandHandler(ILogger<CustomCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<CustomCommandResponse> HandleAsync(CustomCommandRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing custom command");

        var response = new CustomCommandResponse
        {
            Success = true,
            Result = "Custom command executed successfully"
        };

        // Implement command logic
        
        return response;
    }
}
```

### 3. Add Command Line Parsing

```csharp
// src/SecurityScanner.Commands/Parsing/CommandLineParser.cs
private static Command CreateCustomCommand()
{
    var customCommand = new Command("custom", "Execute custom command");
    
    var param1Argument = new Argument<string>("parameter1") 
    { 
        Description = "First parameter" 
    };
    
    var param2Option = new Option<int>("--param2") 
    { 
        Description = "Second parameter",
        DefaultValueFactory = _ => 0
    };

    customCommand.Arguments.Add(param1Argument);
    customCommand.Options.Add(param2Option);

    return customCommand;
}
```

### 4. Register Command Handler

```csharp
// src/SecurityScanner.App/Extensions/ServiceCollectionExtensions.cs
services.AddTransient<ICommandHandler<CustomCommandRequest, CustomCommandResponse>, CustomCommandHandler>();
```

## Testing Guidelines

### Unit Testing

1. **Test Structure**
   ```csharp
   [TestFixture]
   public class ServiceTests
   {
       [SetUp]
       public void Setup() { /* ... */ }
       
       [Test]
       public async Task Method_Condition_ExpectedResult() { /* ... */ }
       
       [TearDown]
       public void TearDown() { /* ... */ }
   }
   ```

2. **Mocking External Dependencies**
   ```csharp
   var httpClientMock = new Mock<HttpClient>();
   var loggerMock = new Mock<ILogger<Service>>();
   ```

3. **Testing Async Methods**
   ```csharp
   [Test]
   public async Task ScanAsync_ValidInput_ReturnsResult()
   {
       var result = await service.ScanAsync(domain);
       Assert.That(result, Is.Not.Null);
   }
   ```

### Integration Testing

1. **Test with Real Dependencies**
   ```csharp
   [Test]
   public async Task EndToEnd_SecurityHeadersScan_ReturnsValidJson()
   {
       var result = await ExecuteCommand("example.com --tools headers --json");
       var json = JsonConvert.DeserializeObject<ScanCommandResponse>(result);
       Assert.That(json.Success, Is.True);
   }
   ```

2. **Test Configuration**
   ```csharp
   [Test]
   public void Configuration_ValidSettings_LoadsCorrectly()
   {
       var configuration = BuildConfiguration();
       var settings = configuration.GetSection("ScannerSettings").Get<ScannerSettings>();
       Assert.That(settings, Is.Not.Null);
   }
   ```

## Code Standards

### Naming Conventions

- **Classes**: PascalCase (`SecurityHeadersService`)
- **Methods**: PascalCase (`ScanAsync`)
- **Properties**: PascalCase (`ServiceName`)
- **Fields**: camelCase with underscore (`_httpClient`)
- **Constants**: PascalCase (`DefaultTimeout`)

### Documentation

1. **XML Documentation**
   ```csharp
   /// <summary>
   /// Scans the specified domain for security vulnerabilities.
   /// </summary>
   /// <param name="domain">The domain configuration to scan.</param>
   /// <param name="cancellationToken">Cancellation token for the operation.</param>
   /// <returns>A task representing the scan result.</returns>
   public async Task<ScanResult> ScanAsync(DomainConfig domain, CancellationToken cancellationToken)
   ```

2. **README Updates**
   - Update documentation when adding new features
   - Include usage examples
   - Document configuration options

### Error Handling

1. **Use Specific Exceptions**
   ```csharp
   throw new ScannerException("Scanner service unavailable", ScannerType.Custom, domain.Name);
   ```

2. **Log Errors with Context**
   ```csharp
   _logger.LogError(ex, "Scan failed for {Domain} using {Scanner}", domain.Name, ServiceName);
   ```

3. **Graceful Degradation**
   ```csharp
   try
   {
       return await ExecuteScanAsync(domain);
   }
   catch (Exception ex)
   {
       return new ScanResult 
       { 
           Status = ScanStatus.Failed, 
           ErrorMessage = ex.Message 
       };
   }
   ```

## Performance Guidelines

### Asynchronous Programming

1. **Use async/await consistently**
   ```csharp
   public async Task<ScanResult> ScanAsync(DomainConfig domain, CancellationToken cancellationToken)
   {
       var response = await _httpClient.GetAsync(domain.Url, cancellationToken);
       return await ProcessResponseAsync(response, cancellationToken);
   }
   ```

2. **Pass CancellationToken**
   ```csharp
   public async Task<ScanResult> ScanAsync(DomainConfig domain, CancellationToken cancellationToken = default)
   {
       // Use cancellationToken in all async operations
   }
   ```

### Resource Management

1. **Use HttpClientFactory**
   ```csharp
   private readonly HttpClient _httpClient;
   
   public Service(IHttpClientFactory httpClientFactory)
   {
       _httpClient = httpClientFactory.CreateClient("ServiceName");
   }
   ```

2. **Dispose Resources**
   ```csharp
   using var response = await _httpClient.GetAsync(url);
   using var content = await response.Content.ReadAsStreamAsync();
   ```

## Debugging

### Local Development

1. **Use Development Configuration**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "SecurityScanner": "Trace"
       }
     }
   }
   ```

2. **Verbose Output**
   ```bash
   dotnet run --project src/SecurityScanner.App -- example.com --verbose
   ```

### Debugging Tests

1. **Debug Specific Test**
   ```bash
   dotnet test --filter "FullyQualifiedName~SecurityHeadersServiceTests.ScanAsync_ValidDomain_ReturnsScanResult"
   ```

2. **Debug with Logs**
   ```csharp
   [Test]
   public async Task TestWithLogging()
   {
       var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
       var logger = loggerFactory.CreateLogger<Service>();
       var service = new Service(logger);
       
       var result = await service.ScanAsync(domain);
       
       Console.WriteLine($"Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
   }
   ```

## CI/CD Integration

### GitHub Actions

```yaml
name: Build and Test

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Integration Test
      run: dotnet run --project src/SecurityScanner.App -- example.com --json
```

### Pre-commit Hooks

```bash
# .git/hooks/pre-commit
#!/bin/bash
dotnet format --verify-no-changes
dotnet build
dotnet test
```

## Contributing

### Pull Request Process

1. **Create Feature Branch**
   ```bash
   git checkout -b feature/description
   ```

2. **Make Changes**
   - Follow coding standards
   - Add tests for new functionality
   - Update documentation

3. **Test Changes**
   ```bash
   dotnet build
   dotnet test
   dotnet run --project src/SecurityScanner.App -- example.com --json
   ```

4. **Submit Pull Request**
   - Clear description of changes
   - Reference related issues
   - Include test results

### Code Review Checklist

- [ ] Code follows naming conventions
- [ ] Unit tests added/updated
- [ ] Documentation updated
- [ ] Error handling implemented
- [ ] Performance considerations addressed
- [ ] Security implications reviewed

## Common Issues

### Build Issues

1. **Missing Dependencies**
   ```bash
   dotnet restore
   dotnet build
   ```

2. **Version Conflicts**
   ```bash
   dotnet clean
   dotnet restore
   ```

### Test Issues

1. **Flaky Tests**
   - Use fixed test data
   - Mock external dependencies
   - Handle timing issues

2. **Integration Test Failures**
   - Check external service availability
   - Verify network connectivity
   - Review test configuration

## Next Steps

- Review [Architecture Documentation](architecture.md)
- Explore [Scanner Documentation](scanners/)
- Check [Configuration Guide](configuration.md)
- See [User Guide](user-guide.md) for usage examples