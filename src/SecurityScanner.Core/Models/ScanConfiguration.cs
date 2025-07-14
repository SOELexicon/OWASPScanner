namespace SecurityScanner.Core.Models;

public class ScanConfiguration
{
    public List<DomainConfig> Domains { get; set; } = new();
    public List<ScannerType> EnabledScanners { get; set; } = new();
    public bool ParallelExecution { get; set; } = true;
    public int MaxConcurrentScans { get; set; } = 3;
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool JsonOutput { get; set; }
    public string? OutputFile { get; set; }
    public LoadTestSettings LoadTestSettings { get; set; } = new();
}

public class DomainConfig
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<ScannerType> EnabledScanners { get; set; } = new();
    public Dictionary<string, object> ScannerSettings { get; set; } = new();
}

public class LoadTestSettings
{
    public int RequestsPerSecond { get; set; } = 10;
    public int DurationSeconds { get; set; } = 60;
    public int RampUpSeconds { get; set; } = 0;
    public int MaxConcurrentRequests { get; set; } = 50;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}