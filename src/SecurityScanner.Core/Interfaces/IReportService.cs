using SecurityScanner.Core.Models;

namespace SecurityScanner.Core.Interfaces;

public interface IReportService
{
    Task<string> GenerateJsonReportAsync(IEnumerable<ScanResult> scanResults);
    Task<string> GenerateHtmlReportAsync(IEnumerable<ScanResult> scanResults);
    Task SaveReportAsync(string report, string filePath);
}