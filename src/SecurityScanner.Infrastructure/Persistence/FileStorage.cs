using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SecurityScanner.Core.Models;
using SecurityScanner.Infrastructure.Configuration;

namespace SecurityScanner.Infrastructure.Persistence;

public interface IFileStorage
{
    Task SaveScanResultsAsync(IEnumerable<ScanResult> results, string fileName);
    Task<IEnumerable<ScanResult>?> LoadScanResultsAsync(string fileName);
    Task SaveReportAsync(string report, string fileName);
    Task<string?> LoadReportAsync(string fileName);
    Task<bool> FileExistsAsync(string fileName);
    Task DeleteFileAsync(string fileName);
    Task<IEnumerable<string>> ListFilesAsync(string pattern = "*");
}

public class FileStorage : IFileStorage
{
    private readonly ReportSettings _settings;
    private readonly ILogger<FileStorage> _logger;
    private readonly string _baseDirectory;

    public FileStorage(IOptions<ReportSettings> settings, ILogger<FileStorage> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _baseDirectory = Path.GetFullPath(_settings.OutputDirectory);
        
        Directory.CreateDirectory(_baseDirectory);
    }

    public async Task SaveScanResultsAsync(IEnumerable<ScanResult> results, string fileName)
    {
        try
        {
            var filePath = GetFilePath(fileName);
            EnsureDirectoryExists(filePath);

            var json = JsonConvert.SerializeObject(results, Formatting.Indented);
            
            if (_settings.CompressLargeReports && json.Length > 1024 * 1024) // 1MB
            {
                await SaveCompressedAsync(filePath, json).ConfigureAwait(false);
            }
            else
            {
                await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
            }

            _logger.LogInformation("Saved scan results to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save scan results to {FileName}", fileName);
            throw;
        }
    }

    public async Task<IEnumerable<ScanResult>?> LoadScanResultsAsync(string fileName)
    {
        try
        {
            var filePath = GetFilePath(fileName);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Scan results file not found: {FilePath}", filePath);
                return null;
            }

            string json;
            
            if (IsCompressed(filePath))
            {
                json = await LoadCompressedAsync(filePath).ConfigureAwait(false);
            }
            else
            {
                json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            }

            var results = JsonConvert.DeserializeObject<IEnumerable<ScanResult>>(json);
            _logger.LogInformation("Loaded scan results from {FilePath}", filePath);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load scan results from {FileName}", fileName);
            throw;
        }
    }

    public async Task SaveReportAsync(string report, string fileName)
    {
        try
        {
            var filePath = GetFilePath(fileName);
            EnsureDirectoryExists(filePath);

            if (_settings.CompressLargeReports && report.Length > 1024 * 1024) // 1MB
            {
                await SaveCompressedAsync(filePath, report).ConfigureAwait(false);
            }
            else
            {
                await File.WriteAllTextAsync(filePath, report).ConfigureAwait(false);
            }

            _logger.LogInformation("Saved report to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save report to {FileName}", fileName);
            throw;
        }
    }

    public async Task<string?> LoadReportAsync(string fileName)
    {
        try
        {
            var filePath = GetFilePath(fileName);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Report file not found: {FilePath}", filePath);
                return null;
            }

            string content;
            
            if (IsCompressed(filePath))
            {
                content = await LoadCompressedAsync(filePath).ConfigureAwait(false);
            }
            else
            {
                content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            }

            _logger.LogInformation("Loaded report from {FilePath}", filePath);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load report from {FileName}", fileName);
            throw;
        }
    }

    public Task<bool> FileExistsAsync(string fileName)
    {
        var filePath = GetFilePath(fileName);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task DeleteFileAsync(string fileName)
    {
        try
        {
            var filePath = GetFilePath(fileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted file: {FilePath}", filePath);
            }
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileName}", fileName);
            throw;
        }
    }

    public Task<IEnumerable<string>> ListFilesAsync(string pattern = "*")
    {
        try
        {
            var files = Directory.GetFiles(_baseDirectory, pattern, SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>();
            
            return Task.FromResult(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files with pattern {Pattern}", pattern);
            throw;
        }
    }

    private string GetFilePath(string fileName)
    {
        if (_settings.IncludeTimestamp && !fileName.Contains("-"))
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
            fileName = $"{nameWithoutExt}-{timestamp}{extension}";
        }
        
        return Path.Combine(_baseDirectory, fileName);
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static bool IsCompressed(string filePath)
    {
        return filePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SaveCompressedAsync(string filePath, string content)
    {
        var compressedPath = filePath + ".gz";
        using var fileStream = File.Create(compressedPath);
        using var gzipStream = new System.IO.Compression.GZipStream(fileStream, System.IO.Compression.CompressionMode.Compress);
        using var writer = new StreamWriter(gzipStream);
        
        await writer.WriteAsync(content).ConfigureAwait(false);
    }

    private static async Task<string> LoadCompressedAsync(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        using var gzipStream = new System.IO.Compression.GZipStream(fileStream, System.IO.Compression.CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream);
        
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }
}