using SecurityScanner.Core.Models;

namespace SecurityScanner.Commands.Models;

public abstract class CommandResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ScanCommandResponse : CommandResponse
{
    public List<ScanResult> ScanResults { get; set; } = new();
    public ScanSummary Summary { get; set; } = new();
}

public class ScanSummary
{
    public int TotalDomains { get; set; }
    public int SuccessfulScans { get; set; }
    public int FailedScans { get; set; }
    public int TotalVulnerabilities { get; set; }
    public int CriticalVulnerabilities { get; set; }
    public int HighVulnerabilities { get; set; }
    public double TotalScanTimeSeconds { get; set; }
}

public class ReportCommandResponse : CommandResponse
{
    public string ReportContent { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
}

public class ConfigCommandResponse : CommandResponse
{
    public string ConfigData { get; set; } = string.Empty;
    public List<string> ValidationErrors { get; set; } = new();
}

public class HealthCommandResponse : CommandResponse
{
    public List<ScannerHealthStatus> ScannerStatuses { get; set; } = new();
    public bool AllScannersAvailable { get; set; }
}

public class ScannerHealthStatus
{
    public string ScannerName { get; set; } = string.Empty;
    public ScannerType ScannerType { get; set; }
    public bool IsAvailable { get; set; }
    public string? Version { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
}