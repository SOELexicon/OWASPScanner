using System.CommandLine;
using SecurityScanner.Commands.Models;
using SecurityScanner.Core.Models;

namespace SecurityScanner.Commands.Parsing;

public static class CommandLineParser
{
    public static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("SecurityScanner - Automated security scanning tool for web domains");

        // Global options
        var jsonOption = new Option<bool>("--json") { Description = "Output results in JSON format" };
        var outputOption = new Option<string?>("--output") { Description = "Save output to specified file" };
        var verboseOption = new Option<bool>("--verbose") { Description = "Enable verbose logging" };
        var fileOption = new Option<string?>("--file") { Description = "JSON file containing array of URLs to scan" };

        rootCommand.Options.Add(jsonOption);
        rootCommand.Options.Add(outputOption);
        rootCommand.Options.Add(verboseOption);
        rootCommand.Options.Add(fileOption);

        // Scan command (default)
        var scanCommand = CreateScanCommand();
        rootCommand.Subcommands.Add(scanCommand);

        // Report command
        var reportCommand = CreateReportCommand();
        rootCommand.Subcommands.Add(reportCommand);

        // Config command
        var configCommand = CreateConfigCommand();
        rootCommand.Subcommands.Add(configCommand);

        // Health command
        var healthCommand = CreateHealthCommand();
        rootCommand.Subcommands.Add(healthCommand);

        // Make scan the default command for positional arguments
        var domainsArgument = new Argument<string[]>("domains") 
        {
            Description = "Domain(s) to scan. Can be single domain or comma-separated list",
            Arity = ArgumentArity.ZeroOrMore
        };
        
        rootCommand.Arguments.Add(domainsArgument);

        return rootCommand;
    }

    private static Command CreateScanCommand()
    {
        var scanCommand = new Command("scan", "Scan domains for security vulnerabilities");

        var domainsArgument = new Argument<string[]>("domains") 
        {
            Description = "Domain(s) to scan. Can be single domain or comma-separated list",
            Arity = ArgumentArity.ZeroOrMore
        };

        var fileOption = new Option<string?>("--file") 
        { 
            Description = "JSON file containing array of URLs to scan" 
        };
        var allOption = new Option<bool>("--all") { Description = "Scan all configured domains" };
        var toolsOption = new Option<string[]>("--tools") 
        {
            Description = "Scanner tools to use: headers, ssl, zap, loadtest",
            DefaultValueFactory = _ => new[] { "headers" }
        };
        var maxConcurrentOption = new Option<int>("--max-concurrent") 
        {
            Description = "Maximum concurrent scans",
            DefaultValueFactory = _ => 3
        };
        var timeoutOption = new Option<int>("--timeout") 
        {
            Description = "Timeout in seconds for each scan",
            DefaultValueFactory = _ => 300
        };
        var parallelOption = new Option<bool>("--parallel") 
        {
            Description = "Enable parallel scanning",
            DefaultValueFactory = _ => true
        };

        // Load test specific options
        var loadTestRpsOption = new Option<int>("--loadtest-rps") 
        {
            Description = "Load test requests per second",
            DefaultValueFactory = _ => 10
        };
        var loadTestDurationOption = new Option<int>("--loadtest-duration") 
        {
            Description = "Load test duration in seconds",
            DefaultValueFactory = _ => 60
        };
        var loadTestRampUpOption = new Option<int>("--loadtest-rampup") 
        {
            Description = "Load test ramp-up duration in seconds",
            DefaultValueFactory = _ => 0
        };
        var loadTestMaxConcurrentOption = new Option<int>("--loadtest-max-concurrent") 
        {
            Description = "Load test maximum concurrent requests",
            DefaultValueFactory = _ => 50
        };

        scanCommand.Arguments.Add(domainsArgument);
        scanCommand.Options.Add(fileOption);
        scanCommand.Options.Add(allOption);
        scanCommand.Options.Add(toolsOption);
        scanCommand.Options.Add(maxConcurrentOption);
        scanCommand.Options.Add(timeoutOption);
        scanCommand.Options.Add(parallelOption);
        scanCommand.Options.Add(loadTestRpsOption);
        scanCommand.Options.Add(loadTestDurationOption);
        scanCommand.Options.Add(loadTestRampUpOption);
        scanCommand.Options.Add(loadTestMaxConcurrentOption);

        return scanCommand;
    }

    private static Command CreateReportCommand()
    {
        var reportCommand = new Command("report", "Generate reports from scan results");

        var inputFileArgument = new Argument<string>("input-file") 
        {
            Description = "Path to scan results file"
        };
        var formatOption = new Option<string>("--format") 
        {
            Description = "Report format: json, html, csv",
            DefaultValueFactory = _ => "json"
        };

        reportCommand.Arguments.Add(inputFileArgument);
        reportCommand.Options.Add(formatOption);

        return reportCommand;
    }

    private static Command CreateConfigCommand()
    {
        var configCommand = new Command("config", "Manage scanner configuration");

        var actionArgument = new Argument<string>("action") 
        {
            Description = "Action to perform: list, validate, test"
        };
        var configPathOption = new Option<string?>("--config") 
        {
            Description = "Configuration file path"
        };

        configCommand.Arguments.Add(actionArgument);
        configCommand.Options.Add(configPathOption);

        return configCommand;
    }

    private static Command CreateHealthCommand()
    {
        var healthCommand = new Command("health", "Check scanner service availability");

        var scannersOption = new Option<string[]?>("--scanners") 
        {
            Description = "Specific scanners to check: headers, ssl, zap, loadtest"
        };

        healthCommand.Options.Add(scannersOption);

        return healthCommand;
    }

    public static ScanCommandRequest ParseScanRequest(string[] domains, bool all, string[] tools, 
        int maxConcurrent, int timeout, bool parallel, int loadTestRps, int loadTestDuration, 
        int loadTestRampUp, int loadTestMaxConcurrent, bool jsonOutput, string? outputFile, bool verbose, string? inputFile = null)
    {
        var request = new ScanCommandRequest
        {
            JsonOutput = jsonOutput,
            OutputFile = outputFile,
            Verbose = verbose,
            InputFile = inputFile,
            ScanAll = all,
            MaxConcurrent = maxConcurrent,
            TimeoutSeconds = timeout,
            ParallelExecution = parallel,
            LoadTestRps = loadTestRps,
            LoadTestDuration = loadTestDuration,
            LoadTestRampUp = loadTestRampUp,
            LoadTestMaxConcurrent = loadTestMaxConcurrent
        };

        // Parse domains
        if (domains?.Any() == true)
        {
            foreach (var domainInput in domains)
            {
                var domainList = domainInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .Where(d => !string.IsNullOrEmpty(d));
                
                request.Domains.AddRange(domainList);
            }
        }

        // Parse tools
        if (tools?.Any() == true)
        {
            var toolList = new List<ScannerType>();
            foreach (var tool in tools)
            {
                var toolNames = tool.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLowerInvariant());

                foreach (var toolName in toolNames)
                {
                    var scannerType = toolName switch
                    {
                        "headers" => ScannerType.SecurityHeaders,
                        "ssl" => ScannerType.SslLabs,
                        "zap" => ScannerType.OwaspZap,
                        "loadtest" => ScannerType.LoadTest,
                        "nmap" => ScannerType.Nmap,
                        _ => throw new ArgumentException($"Unknown scanner tool: {toolName}")
                    };
                    
                    if (!toolList.Contains(scannerType))
                    {
                        toolList.Add(scannerType);
                    }
                }
            }
            request.Tools = toolList;
        }

        return request;
    }

    public static ReportCommandRequest ParseReportRequest(string inputFile, string format, 
        bool jsonOutput, string? outputFile, bool verbose)
    {
        return new ReportCommandRequest
        {
            InputFile = inputFile,
            Format = format.ToLowerInvariant(),
            JsonOutput = jsonOutput,
            OutputFile = outputFile,
            Verbose = verbose
        };
    }

    public static ConfigCommandRequest ParseConfigRequest(string action, string? configPath, 
        bool jsonOutput, string? outputFile, bool verbose)
    {
        return new ConfigCommandRequest
        {
            Action = action.ToLowerInvariant(),
            ConfigPath = configPath,
            JsonOutput = jsonOutput,
            OutputFile = outputFile,
            Verbose = verbose
        };
    }

    public static HealthCommandRequest ParseHealthRequest(string[]? scanners, 
        bool jsonOutput, string? outputFile, bool verbose)
    {
        var request = new HealthCommandRequest
        {
            JsonOutput = jsonOutput,
            OutputFile = outputFile,
            Verbose = verbose
        };

        if (scanners?.Any() == true)
        {
            var scannerTypes = new List<ScannerType>();
            foreach (var scanner in scanners)
            {
                var scannerNames = scanner.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToLowerInvariant());

                foreach (var scannerName in scannerNames)
                {
                    var scannerType = scannerName switch
                    {
                        "headers" => ScannerType.SecurityHeaders,
                        "ssl" => ScannerType.SslLabs,
                        "zap" => ScannerType.OwaspZap,
                        "loadtest" => ScannerType.LoadTest,
                        "nmap" => ScannerType.Nmap,
                        _ => throw new ArgumentException($"Unknown scanner: {scannerName}")
                    };
                    
                    if (!scannerTypes.Contains(scannerType))
                    {
                        scannerTypes.Add(scannerType);
                    }
                }
            }
            request.SpecificScanners = scannerTypes;
        }

        return request;
    }
}