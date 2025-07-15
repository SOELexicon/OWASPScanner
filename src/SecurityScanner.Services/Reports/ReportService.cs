using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SecurityScanner.Core.Exceptions;
using SecurityScanner.Core.Interfaces;
using SecurityScanner.Core.Models;
using SecurityScanner.Infrastructure.Configuration;
using System.Text;

namespace SecurityScanner.Services.Reports;

public class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;
    private readonly ReportSettings _settings;

    public ReportService(
        ILogger<ReportService> logger,
        IOptions<ReportSettings> reportSettings)
    {
        _logger = logger;
        _settings = reportSettings.Value;
    }

    public async Task<string> GenerateJsonReportAsync(IEnumerable<ScanResult> scanResults)
    {
        var scanResultsList = scanResults.ToList();
        _logger.LogInformation("Generating JSON report for {DomainCount} domains", scanResultsList.Count);
        
        try
        {
            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore
            };

            var jsonReport = JsonConvert.SerializeObject(scanResultsList, jsonSettings);
            
            _logger.LogDebug("JSON report generated successfully - {Length} characters", jsonReport.Length);
            return await Task.FromResult(jsonReport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JSON report");
            throw new ScannerException("Failed to generate JSON report", ex);
        }
    }

    public async Task<string> GenerateHtmlReportAsync(IEnumerable<ScanResult> scanResults)
    {
        var scanResultsList = scanResults.ToList();
        _logger.LogInformation("Generating HTML report for {DomainCount} domains", scanResultsList.Count);
        
        try
        {
            var html = new StringBuilder();
            
            // HTML Document Structure
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine("    <title>Security Scanner Report</title>");
            html.AppendLine("    <style>");
            html.AppendLine(GetCssStyles());
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Header
            html.AppendLine("    <div class=\"header\">");
            html.AppendLine("        <h1>🛡️ Security Scanner Report</h1>");
            html.AppendLine($"        <p class=\"timestamp\">Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
            html.AppendLine("    </div>");
            
            // Calculate summary statistics
            var totalDomains = scanResultsList.Count;
            var successfulScans = scanResultsList.Count(r => r.Status == ScanStatus.Completed);
            var failedScans = scanResultsList.Count(r => r.Status == ScanStatus.Failed);
            var totalVulnerabilities = scanResultsList.Sum(r => r.VulnerabilityCount);
            var criticalVulnerabilities = scanResultsList.Sum(r => r.CriticalVulnerabilityCount);
            var highVulnerabilities = scanResultsList.Sum(r => r.HighVulnerabilityCount);
            var totalScanTime = scanResultsList.Sum(r => r.ScanDuration);
            
            // Summary Section
            html.AppendLine("    <div class=\"summary\">");
            html.AppendLine("        <h2>📊 Summary</h2>");
            html.AppendLine("        <div class=\"summary-grid\">");
            html.AppendLine($"            <div class=\"summary-item\"><span class=\"label\">Total Domains:</span> <span class=\"value\">{totalDomains}</span></div>");
            html.AppendLine($"            <div class=\"summary-item\"><span class=\"label\">Successful Scans:</span> <span class=\"value success\">{successfulScans}</span></div>");
            html.AppendLine($"            <div class=\"summary-item\"><span class=\"label\">Failed Scans:</span> <span class=\"value {(failedScans > 0 ? "failure" : "success")}\">{failedScans}</span></div>");
            html.AppendLine($"            <div class=\"summary-item\"><span class=\"label\">Total Vulnerabilities:</span> <span class=\"value\">{totalVulnerabilities}</span></div>");
            html.AppendLine($"            <div class=\"summary-item\"><span class=\"label\">Critical Issues:</span> <span class=\"value {(criticalVulnerabilities > 0 ? "critical" : "success")}\">{criticalVulnerabilities}</span></div>");
            html.AppendLine($"            <div class=\"summary-item\"><span class=\"label\">High Severity:</span> <span class=\"value {(highVulnerabilities > 0 ? "high" : "success")}\">{highVulnerabilities}</span></div>");
            html.AppendLine($"            <div class=\"summary-item\"><span class=\"label\">Scan Duration:</span> <span class=\"value\">{totalScanTime:F2}s</span></div>");
            html.AppendLine("        </div>");
            html.AppendLine("    </div>");
            
            // Detailed Results
            if (scanResultsList.Any())
            {
                html.AppendLine("    <div class=\"results\">");
                html.AppendLine("        <h2>🔍 Detailed Results</h2>");
                
                foreach (var result in scanResultsList)
                {
                    html.AppendLine(GenerateResultSection(result));
                }
                
                html.AppendLine("    </div>");
            }
            
            // Footer
            html.AppendLine("    <div class=\"footer\">");
            html.AppendLine("        <p>Generated by SecurityScanner - Comprehensive Web Security Analysis Tool</p>");
            html.AppendLine("    </div>");
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            var htmlReport = html.ToString();
            _logger.LogDebug("HTML report generated successfully - {Length} characters", htmlReport.Length);
            return await Task.FromResult(htmlReport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate HTML report");
            throw new ScannerException("Failed to generate HTML report", ex);
        }
    }

    public async Task SaveReportAsync(string report, string filePath)
    {
        _logger.LogInformation("Saving report to {FilePath}", filePath);
        
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory: {Directory}", directory);
            }

            await File.WriteAllTextAsync(filePath, report);
            _logger.LogInformation("Report saved successfully to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save report to {FilePath}", filePath);
            throw new ScannerException($"Failed to save report to {filePath}", ex);
        }
    }

    // Helper methods for working with ScanCommandResponse
    public async Task<string> GenerateJsonReportAsync(ScanCommandResponse scanResponse)
    {
        return await GenerateJsonReportAsync(scanResponse.ScanResults);
    }

    public async Task<string> GenerateHtmlReportAsync(ScanCommandResponse scanResponse)
    {
        return await GenerateHtmlReportAsync(scanResponse.ScanResults);
    }

    private string GenerateResultSection(ScanResult result)
    {
        var html = new StringBuilder();
        var statusClass = result.Status switch
        {
            ScanStatus.Completed => "success",
            ScanStatus.Failed => "failure",
            ScanStatus.InProgress => "in-progress",
            _ => "unknown"
        };

        html.AppendLine("        <div class=\"result-section\">");
        html.AppendLine($"            <h3>🌐 {result.Domain}</h3>");
        html.AppendLine("            <div class=\"result-info\">");
        html.AppendLine($"                <div class=\"info-item\"><span class=\"label\">URL:</span> <a href=\"{result.Url}\" target=\"_blank\">{result.Url}</a></div>");
        html.AppendLine($"                <div class=\"info-item\"><span class=\"label\">Status:</span> <span class=\"status {statusClass}\">{result.Status}</span></div>");
        html.AppendLine($"                <div class=\"info-item\"><span class=\"label\">Scanner:</span> {result.Scanner}</div>");
        html.AppendLine($"                <div class=\"info-item\"><span class=\"label\">Vulnerabilities:</span> {result.VulnerabilityCount}</div>");
        
        if (result.ScanDuration > 0)
        {
            html.AppendLine($"                <div class=\"info-item\"><span class=\"label\">Duration:</span> {result.ScanDuration:F2}s</div>");
        }
        
        html.AppendLine("            </div>");

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            html.AppendLine($"            <div class=\"error-message\">❌ Error: {result.ErrorMessage}</div>");
        }

        if (result.Vulnerabilities.Any())
        {
            html.AppendLine("            <div class=\"vulnerabilities\">");
            html.AppendLine("                <h4>🚨 Security Issues</h4>");
            
            foreach (var vuln in result.Vulnerabilities.OrderByDescending(v => v.Severity))
            {
                html.AppendLine(GenerateVulnerabilityItem(vuln));
            }
            
            html.AppendLine("            </div>");
        }
        else if (result.Status == ScanStatus.Completed)
        {
            html.AppendLine("            <div class=\"no-issues\">✅ No security issues found</div>");
        }

        html.AppendLine("        </div>");
        return html.ToString();
    }

    private string GenerateVulnerabilityItem(VulnerabilityReport vulnerability)
    {
        var severityClass = vulnerability.Severity switch
        {
            SeverityLevel.Critical => "critical",
            SeverityLevel.High => "high", 
            SeverityLevel.Medium => "medium",
            SeverityLevel.Low => "low",
            _ => "info"
        };

        var severityIcon = vulnerability.Severity switch
        {
            SeverityLevel.Critical => "🔴",
            SeverityLevel.High => "🟠",
            SeverityLevel.Medium => "🟡",
            SeverityLevel.Low => "🔵",
            _ => "ℹ️"
        };

        var html = new StringBuilder();
        html.AppendLine("                <div class=\"vulnerability-item\">");
        html.AppendLine($"                    <div class=\"vuln-header\">");
        html.AppendLine($"                        <span class=\"severity {severityClass}\">{severityIcon} {vulnerability.Severity}</span>");
        html.AppendLine($"                        <span class=\"vuln-name\">{vulnerability.Name}</span>");
        html.AppendLine("                    </div>");
        html.AppendLine($"                    <div class=\"vuln-category\">Category: {vulnerability.Category}</div>");
        html.AppendLine($"                    <div class=\"vuln-description\">{vulnerability.Description}</div>");
        
        if (!string.IsNullOrEmpty(vulnerability.Solution))
        {
            html.AppendLine($"                    <div class=\"vuln-solution\"><strong>Solution:</strong> {vulnerability.Solution}</div>");
        }
        
        if (!string.IsNullOrEmpty(vulnerability.Evidence))
        {
            html.AppendLine($"                    <div class=\"vuln-evidence\"><strong>Evidence:</strong> <code>{vulnerability.Evidence}</code></div>");
        }
        
        html.AppendLine("                </div>");
        return html.ToString();
    }

    private string GetCssStyles()
    {
        return @"
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f8f9fa;
        }
        
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 2rem;
            text-align: center;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        
        .header h1 {
            font-size: 2.5rem;
            margin-bottom: 0.5rem;
        }
        
        .timestamp {
            opacity: 0.9;
            font-size: 1.1rem;
        }
        
        .summary {
            background: white;
            margin: 2rem;
            padding: 2rem;
            border-radius: 10px;
            box-shadow: 0 2px 15px rgba(0,0,0,0.1);
        }
        
        .summary h2 {
            color: #4a5568;
            margin-bottom: 1.5rem;
            font-size: 1.8rem;
        }
        
        .summary-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 1rem;
        }
        
        .summary-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 1rem;
            background: #f7fafc;
            border-radius: 8px;
            border-left: 4px solid #e2e8f0;
        }
        
        .label {
            font-weight: 600;
            color: #4a5568;
        }
        
        .value {
            font-weight: bold;
            font-size: 1.1rem;
        }
        
        .value.success { color: #38a169; }
        .value.failure { color: #e53e3e; }
        .value.critical { color: #c53030; }
        .value.high { color: #dd6b20; }
        
        .results {
            margin: 2rem;
        }
        
        .results h2 {
            color: #4a5568;
            margin-bottom: 1.5rem;
            font-size: 1.8rem;
        }
        
        .result-section {
            background: white;
            margin-bottom: 2rem;
            padding: 2rem;
            border-radius: 10px;
            box-shadow: 0 2px 15px rgba(0,0,0,0.1);
        }
        
        .result-section h3 {
            color: #2d3748;
            margin-bottom: 1rem;
            font-size: 1.5rem;
        }
        
        .result-info {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 0.5rem;
            margin-bottom: 1rem;
        }
        
        .info-item {
            display: flex;
            gap: 0.5rem;
        }
        
        .status.success { color: #38a169; font-weight: bold; }
        .status.failure { color: #e53e3e; font-weight: bold; }
        .status.in-progress { color: #3182ce; font-weight: bold; }
        
        .error-message {
            background: #fed7d7;
            color: #c53030;
            padding: 1rem;
            border-radius: 8px;
            margin: 1rem 0;
            border-left: 4px solid #e53e3e;
        }
        
        .no-issues {
            background: #c6f6d5;
            color: #2f855a;
            padding: 1rem;
            border-radius: 8px;
            margin: 1rem 0;
            border-left: 4px solid #38a169;
        }
        
        .vulnerabilities {
            margin-top: 1.5rem;
        }
        
        .vulnerabilities h4 {
            color: #4a5568;
            margin-bottom: 1rem;
            font-size: 1.3rem;
        }
        
        .vulnerability-item {
            background: #fafafa;
            border: 1px solid #e2e8f0;
            border-radius: 8px;
            padding: 1rem;
            margin-bottom: 1rem;
        }
        
        .vuln-header {
            display: flex;
            align-items: center;
            gap: 1rem;
            margin-bottom: 0.5rem;
        }
        
        .severity {
            padding: 0.25rem 0.75rem;
            border-radius: 20px;
            font-size: 0.875rem;
            font-weight: bold;
        }
        
        .severity.critical { background: #fed7d7; color: #c53030; }
        .severity.high { background: #feebc8; color: #c05621; }
        .severity.medium { background: #fef5e7; color: #b7791f; }
        .severity.low { background: #e6fffa; color: #285e61; }
        .severity.info { background: #ebf8ff; color: #2b6cb0; }
        
        .vuln-name {
            font-weight: bold;
            font-size: 1.1rem;
            color: #2d3748;
        }
        
        .vuln-category {
            color: #718096;
            font-size: 0.9rem;
            margin-bottom: 0.5rem;
        }
        
        .vuln-description {
            color: #4a5568;
            margin-bottom: 0.5rem;
        }
        
        .vuln-solution {
            background: #f0fff4;
            color: #2f855a;
            padding: 0.5rem;
            border-radius: 5px;
            margin: 0.5rem 0;
            border-left: 3px solid #38a169;
        }
        
        .vuln-evidence {
            background: #f7fafc;
            padding: 0.5rem;
            border-radius: 5px;
            margin: 0.5rem 0;
        }
        
        code {
            background: #edf2f7;
            padding: 0.25rem 0.5rem;
            border-radius: 3px;
            font-family: 'Monaco', 'Consolas', monospace;
            color: #e53e3e;
        }
        
        a {
            color: #3182ce;
            text-decoration: none;
        }
        
        a:hover {
            text-decoration: underline;
        }
        
        .footer {
            text-align: center;
            padding: 2rem;
            color: #718096;
            background: #edf2f7;
        }
        
        @media (max-width: 768px) {
            .header h1 { font-size: 2rem; }
            .summary { margin: 1rem; padding: 1rem; }
            .results { margin: 1rem; }
            .result-section { padding: 1rem; }
            .summary-grid { grid-template-columns: 1fr; }
            .result-info { grid-template-columns: 1fr; }
        }";
    }
}