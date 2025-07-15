using Microsoft.Extensions.Logging;
using SecurityScanner.Commands.Models;
using SecurityScanner.Core.Interfaces;
using SecurityScanner.Core.Models;

namespace SecurityScanner.Commands.Handlers;

public interface IReportCommandHandler : ICommandHandler<ReportCommandRequest, ReportCommandResponse>
{
}

public class ReportCommandHandler : IReportCommandHandler
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportCommandHandler> _logger;

    public ReportCommandHandler(
        IReportService reportService,
        ILogger<ReportCommandHandler> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    public async Task<ReportCommandResponse> HandleAsync(ReportCommandRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting report generation - Format: {Format}, Input: {InputFile}", 
            request.Format, request.InputFile);

        var response = new ReportCommandResponse
        {
            Success = true
        };

        try
        {
            // Load scan results from input file
            var scanResults = await LoadScanResultsAsync(request.InputFile, cancellationToken);

            if (!scanResults.Any())
            {
                response.Success = false;
                response.ErrorMessage = "No scan results found in input file";
                return response;
            }

            _logger.LogInformation("Loaded {Count} scan results from {InputFile}", scanResults.Count(), request.InputFile);

            // Generate report based on format
            string reportContent;
            switch (request.Format.ToLowerInvariant())
            {
                case "json":
                    reportContent = await _reportService.GenerateJsonReportAsync(scanResults);
                    break;
                case "html":
                    reportContent = await _reportService.GenerateHtmlReportAsync(scanResults);
                    break;
                default:
                    response.Success = false;
                    response.ErrorMessage = $"Unsupported report format: {request.Format}. Supported formats: json, html";
                    return response;
            }

            response.ReportContent = reportContent;

            // Save report to output file if specified
            if (!string.IsNullOrEmpty(request.OutputFile))
            {
                await _reportService.SaveReportAsync(reportContent, request.OutputFile);
                response.OutputPath = request.OutputFile;
                _logger.LogInformation("Report saved to: {OutputFile}", request.OutputFile);
            }
            else
            {
                _logger.LogInformation("Report generated successfully - {Length} characters", reportContent.Length);
            }
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.ErrorMessage = $"Report generation failed: {ex.Message}";
            _logger.LogError(ex, "Report generation failed");
        }

        return response;
    }

    private async Task<IEnumerable<ScanResult>> LoadScanResultsAsync(string inputFile, CancellationToken cancellationToken)
    {
        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException($"Input file not found: {inputFile}");
        }

        var content = await File.ReadAllTextAsync(inputFile, cancellationToken);
        
        try
        {
            // Try to deserialize as ScanCommandResponse first
            var scanCommandResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<ScanCommandResponse>(content);
            if (scanCommandResponse?.ScanResults != null)
            {
                return scanCommandResponse.ScanResults;
            }
        }
        catch
        {
            // If that fails, try to deserialize as array of ScanResult
            try
            {
                var scanResults = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ScanResult>>(content);
                if (scanResults != null)
                {
                    return scanResults;
                }
            }
            catch
            {
                // Ignore and fall through to error
            }
        }

        throw new InvalidOperationException($"Unable to parse scan results from file: {inputFile}. Expected JSON format with ScanResult array or ScanCommandResponse object.");
    }
}

public class ReportCommandRequest
{
    public string InputFile { get; set; } = string.Empty;
    public string OutputFile { get; set; } = string.Empty;
    public string Format { get; set; } = "html"; // html, json
    public bool OpenInBrowser { get; set; }
}