namespace SecurityScanner.Core.Models;

public class ScanResult
{
    public string Domain { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ScanStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double ScanDuration => EndTime?.Subtract(StartTime).TotalSeconds ?? 0;
    public string? ErrorMessage { get; set; }
    public string Scanner { get; set; } = string.Empty;
    public List<VulnerabilityReport> Vulnerabilities { get; set; } = new();
    public Dictionary<string, object> ScannerSpecificData { get; set; } = new();
    public int VulnerabilityCount => Vulnerabilities.Count;
    public int CriticalVulnerabilityCount => Vulnerabilities.Count(v => v.Severity == SeverityLevel.Critical);
    public int HighVulnerabilityCount => Vulnerabilities.Count(v => v.Severity == SeverityLevel.High);
}