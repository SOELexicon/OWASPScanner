using SecurityScanner.Core.Models;

namespace SecurityScanner.Commands.Models;

public abstract class CommandRequest
{
    public bool JsonOutput { get; set; }
    public string? OutputFile { get; set; }
    public bool Verbose { get; set; }
}

public class ScanCommandRequest : CommandRequest
{
    public List<string> Domains { get; set; } = new();
    public List<ScannerType> Tools { get; set; } = new() { ScannerType.SecurityHeaders };
    public string? InputFile { get; set; }
    public bool ScanAll { get; set; }
    public int MaxConcurrent { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 300;
    public bool ParallelExecution { get; set; } = true;
    
    // Load test specific options
    public int LoadTestRps { get; set; } = 10;
    public int LoadTestDuration { get; set; } = 60;
    public int LoadTestRampUp { get; set; } = 0;
    public int LoadTestMaxConcurrent { get; set; } = 50;
}

public class ReportCommandRequest : CommandRequest
{
    public string InputFile { get; set; } = string.Empty;
    public string Format { get; set; } = "json";
}

public class ConfigCommandRequest : CommandRequest
{
    public string Action { get; set; } = "list"; // list, validate, test
    public string? ConfigPath { get; set; }
}

public class HealthCommandRequest : CommandRequest
{
    public List<ScannerType>? SpecificScanners { get; set; }
}