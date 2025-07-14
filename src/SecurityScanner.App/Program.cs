using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecurityScanner.App.Extensions;
using SecurityScanner.Commands.Handlers;
using SecurityScanner.Commands.Models;
using SecurityScanner.Commands.Parsing;
using System.CommandLine;

namespace SecurityScanner.App;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Create host builder with configuration and DI
            var host = CreateHostBuilder(args).Build();

            // Create and configure the root command
            var rootCommand = CommandLineParser.CreateRootCommand();

            // Parse the command line arguments
            var parseResult = rootCommand.Parse(args);
            
            // If no arguments provided, show help
            if (args.Length == 0)
            {
                Console.WriteLine(rootCommand.Description);
                rootCommand.Parse(new[] { "--help" }).Invoke();
                return 0;
            }

            // Handle scan command manually (simplified approach for beta version)
            if (args.Length > 0 && !args[0].StartsWith('-'))
            {
                // Direct domain scan
                await HandleSimpleScanAsync(host.Services, args);
                return 0;
            }

            // Execute the parsed command
            var result = parseResult.Invoke();
            
            return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            if (args.Contains("--verbose"))
            {
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return 1;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", 
                        optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables(prefix: "SECURITYSCANNER_")
                    .AddCommandLine(args);
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                
                // Add file logging if configured
                var logLevel = context.Configuration.GetValue<LogLevel>("Logging:LogLevel:Default", LogLevel.Information);
                logging.SetMinimumLevel(logLevel);
            })
            .ConfigureServices((context, services) =>
            {
                // Register all SecurityScanner services
                services.AddSecurityScannerServices(context.Configuration);
            });

    private static async Task HandleSimpleScanAsync(IServiceProvider services, string[] args)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            // Extract domains from command line arguments (simplified)
            var domains = args.Where(arg => !arg.StartsWith('-')).ToArray();
            var jsonOutput = args.Contains("--json");
            var verbose = args.Contains("--verbose");
            
            // Get output file if specified
            string? outputFile = null;
            var outputIndex = Array.IndexOf(args, "--output");
            if (outputIndex >= 0 && outputIndex + 1 < args.Length)
            {
                outputFile = args[outputIndex + 1];
            }

            // Create a basic scan request
            var request = new ScanCommandRequest
            {
                Domains = domains.ToList(),
                Tools = new List<Core.Models.ScannerType> { Core.Models.ScannerType.SecurityHeaders },
                JsonOutput = jsonOutput,
                OutputFile = outputFile,
                Verbose = verbose,
                MaxConcurrent = 3,
                TimeoutSeconds = 300,
                ParallelExecution = true
            };

            // Get the scan command handler and execute
            var handler = services.GetRequiredService<IScanCommandHandler>();
            var response = await handler.HandleAsync(request);

            // Output the results
            await OutputResultsAsync(response, request, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing simple scan command");
            Console.Error.WriteLine($"Scan failed: {ex.Message}");
        }
    }

    private static async Task HandleScanCommandAsync(
        IServiceProvider services,
        string[] domains,
        bool all,
        string[] tools,
        int maxConcurrent,
        int timeout,
        bool parallel,
        int loadTestRps,
        int loadTestDuration,
        int loadTestRampUp,
        int loadTestMaxConcurrent,
        bool jsonOutput,
        string? outputFile,
        bool verbose)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            // Create scan request from command line arguments
            var request = CommandLineParser.ParseScanRequest(
                domains, all, tools, maxConcurrent, timeout, parallel,
                loadTestRps, loadTestDuration, loadTestRampUp, loadTestMaxConcurrent,
                jsonOutput, outputFile, verbose);

            // Get the scan command handler
            var handler = services.GetRequiredService<IScanCommandHandler>();

            // Execute the scan
            var response = await handler.HandleAsync(request);

            // Output the results
            await OutputResultsAsync(response, request, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing scan command");
            Console.Error.WriteLine($"Scan failed: {ex.Message}");
            
            if (verbose)
            {
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    private static async Task OutputResultsAsync(ScanCommandResponse response, ScanCommandRequest request, ILogger logger)
    {
        try
        {
            string output;

            if (request.JsonOutput)
            {
                // Serialize to JSON
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented);
                output = json;
            }
            else
            {
                // Format as human-readable text
                output = FormatTextOutput(response);
            }

            // Write to file if specified
            if (!string.IsNullOrEmpty(request.OutputFile))
            {
                await File.WriteAllTextAsync(request.OutputFile, output);
                logger.LogInformation("Results saved to: {OutputFile}", request.OutputFile);
                
                if (!request.JsonOutput)
                {
                    Console.WriteLine($"Results saved to: {request.OutputFile}");
                }
            }
            else
            {
                // Write to console
                Console.WriteLine(output);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error outputting results");
            Console.Error.WriteLine($"Error writing output: {ex.Message}");
        }
    }

    private static string FormatTextOutput(ScanCommandResponse response)
    {
        var output = new System.Text.StringBuilder();

        output.AppendLine("=== Security Scan Results ===");
        output.AppendLine($"Status: {(response.Success ? "SUCCESS" : "FAILED")}");
        output.AppendLine($"Timestamp: {response.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        
        if (!string.IsNullOrEmpty(response.ErrorMessage))
        {
            output.AppendLine($"Error: {response.ErrorMessage}");
        }

        output.AppendLine();
        output.AppendLine("=== Summary ===");
        output.AppendLine($"Total Domains: {response.Summary.TotalDomains}");
        output.AppendLine($"Successful Scans: {response.Summary.SuccessfulScans}");
        output.AppendLine($"Failed Scans: {response.Summary.FailedScans}");
        output.AppendLine($"Total Vulnerabilities: {response.Summary.TotalVulnerabilities}");
        output.AppendLine($"Critical Vulnerabilities: {response.Summary.CriticalVulnerabilities}");
        output.AppendLine($"High Vulnerabilities: {response.Summary.HighVulnerabilities}");
        output.AppendLine($"Total Scan Time: {response.Summary.TotalScanTimeSeconds:F2} seconds");

        if (response.ScanResults.Any())
        {
            output.AppendLine();
            output.AppendLine("=== Detailed Results ===");
            
            foreach (var result in response.ScanResults)
            {
                output.AppendLine($"\nDomain: {result.Domain}");
                output.AppendLine($"Status: {result.Status}");
                output.AppendLine($"Scanner: {result.Scanner}");
                output.AppendLine($"Vulnerabilities: {result.VulnerabilityCount}");
                
                if (result.Vulnerabilities.Any())
                {
                    foreach (var vuln in result.Vulnerabilities)
                    {
                        output.AppendLine($"  - {vuln.Name} ({vuln.Severity})");
                        if (!string.IsNullOrEmpty(vuln.Description))
                        {
                            output.AppendLine($"    {vuln.Description}");
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    output.AppendLine($"Error: {result.ErrorMessage}");
                }
            }
        }

        return output.ToString();
    }
}