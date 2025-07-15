using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityScanner.Core.Interfaces;
using SecurityScanner.Core.Models;
using SecurityScanner.Infrastructure.Configuration;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SecurityScanner.Services.Scanners;

public class LoadTestService : IScannerService
{
    public string ServiceName => "Load Test";
    public ScannerType ScannerType => ScannerType.LoadTest;

    private readonly HttpClient _httpClient;
    private readonly ILogger<LoadTestService> _logger;
    private readonly SecurityScanner.Infrastructure.Configuration.LoadTestSettings _settings;

    public LoadTestService(
        IHttpClientFactory httpClientFactory,
        ILogger<LoadTestService> logger,
        IOptions<ScannerSettings> scannerSettings)
    {
        _httpClient = httpClientFactory.CreateClient("LoadTest");
        _logger = logger;
        _settings = scannerSettings.Value.LoadTest;
    }

    public async Task<ScanResult> ScanAsync(DomainConfig domain, CancellationToken cancellationToken = default)
    {
        var result = new ScanResult
        {
            Domain = domain.Name,
            Url = domain.Url,
            Scanner = ServiceName,
            StartTime = DateTime.UtcNow
        };

        _logger.LogInformation("Starting load test for {Domain}", domain.Name);

        try
        {
            // Get load test settings from domain config or use defaults
            var rps = GetConfigValue<int>(domain, "LoadTestRps", _settings.RequestsPerSecond);
            var duration = GetConfigValue<int>(domain, "LoadTestDuration", _settings.DurationSeconds);
            var rampUp = GetConfigValue<int>(domain, "LoadTestRampUp", _settings.RampUpSeconds);
            var maxConcurrent = GetConfigValue<int>(domain, "LoadTestMaxConcurrent", _settings.MaxConcurrentRequests);

            var loadTestResult = await ExecuteLoadTestAsync(domain.Url, rps, duration, rampUp, maxConcurrent, cancellationToken);
            
            result.Vulnerabilities = AnalyzeLoadTestResults(loadTestResult, domain.Url);
            result.Status = ScanStatus.Completed;
            result.ScannerSpecificData["loadTestResult"] = loadTestResult;
            
            _logger.LogInformation("Completed load test for {Domain}. Found {IssueCount} issues", 
                domain.Name, result.VulnerabilityCount);
        }
        catch (Exception ex)
        {
            result.Status = ScanStatus.Failed;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Load test failed for {Domain}", domain.Name);
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }

    private async Task<LoadTestResult> ExecuteLoadTestAsync(
        string url, 
        int requestsPerSecond, 
        int durationSeconds, 
        int rampUpSeconds, 
        int maxConcurrent, 
        CancellationToken cancellationToken)
    {
        var testResult = new LoadTestResult
        {
            Url = url,
            RequestsPerSecond = requestsPerSecond,
            DurationSeconds = durationSeconds,
            RampUpSeconds = rampUpSeconds,
            MaxConcurrent = maxConcurrent,
            StartTime = DateTime.UtcNow
        };

        var responseTimes = new ConcurrentBag<double>();
        var statusCodes = new ConcurrentDictionary<int, int>();
        var errors = new ConcurrentBag<string>();
        var semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        
        var totalDuration = rampUpSeconds + durationSeconds;
        var stopwatch = Stopwatch.StartNew();
        var requestTasks = new List<Task>();

        _logger.LogInformation("Starting load test: {RPS} RPS for {Duration}s with {RampUp}s ramp-up", 
            requestsPerSecond, durationSeconds, rampUpSeconds);

        while (stopwatch.Elapsed.TotalSeconds < totalDuration && !cancellationToken.IsCancellationRequested)
        {
            // Calculate current RPS based on ramp-up
            var currentRps = rampUpSeconds > 0 && stopwatch.Elapsed.TotalSeconds < rampUpSeconds
                ? (int)(requestsPerSecond * (stopwatch.Elapsed.TotalSeconds / rampUpSeconds))
                : requestsPerSecond;

            if (currentRps > 0)
            {
                var delayBetweenRequests = 1000 / currentRps; // milliseconds

                for (int i = 0; i < currentRps && stopwatch.Elapsed.TotalSeconds < totalDuration; i++)
                {
                    var requestTask = ExecuteRequestAsync(url, semaphore, responseTimes, statusCodes, errors, cancellationToken);
                    requestTasks.Add(requestTask);

                    if (i < currentRps - 1) // Don't delay after the last request
                    {
                        await Task.Delay(delayBetweenRequests, cancellationToken);
                    }
                }
            }

            // Wait a bit before next second
            await Task.Delay(100, cancellationToken);
        }

        // Wait for all requests to complete
        await Task.WhenAll(requestTasks);
        stopwatch.Stop();

        testResult.EndTime = DateTime.UtcNow;
        testResult.ActualDurationSeconds = stopwatch.Elapsed.TotalSeconds;
        testResult.TotalRequests = responseTimes.Count;
        testResult.ResponseTimes = responseTimes.ToList();
        testResult.StatusCodes = statusCodes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        testResult.Errors = errors.ToList();

        // Calculate statistics
        if (testResult.ResponseTimes.Any())
        {
            testResult.AverageResponseTime = testResult.ResponseTimes.Average();
            testResult.MinResponseTime = testResult.ResponseTimes.Min();
            testResult.MaxResponseTime = testResult.ResponseTimes.Max();
            testResult.MedianResponseTime = CalculatePercentile(testResult.ResponseTimes, 50);
            testResult.P95ResponseTime = CalculatePercentile(testResult.ResponseTimes, 95);
            testResult.P99ResponseTime = CalculatePercentile(testResult.ResponseTimes, 99);
        }

        testResult.SuccessfulRequests = testResult.StatusCodes
            .Where(kvp => kvp.Key >= 200 && kvp.Key < 400)
            .Sum(kvp => kvp.Value);
        
        testResult.ErrorRequests = testResult.TotalRequests - testResult.SuccessfulRequests;
        testResult.ErrorRate = testResult.TotalRequests > 0 
            ? (double)testResult.ErrorRequests / testResult.TotalRequests * 100 
            : 0;

        testResult.ActualRps = testResult.ActualDurationSeconds > 0 
            ? testResult.TotalRequests / testResult.ActualDurationSeconds 
            : 0;

        return testResult;
    }

    private async Task ExecuteRequestAsync(
        string url,
        SemaphoreSlim semaphore,
        ConcurrentBag<double> responseTimes,
        ConcurrentDictionary<int, int> statusCodes,
        ConcurrentBag<string> errors,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            stopwatch.Stop();
            
            responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
            statusCodes.AddOrUpdate((int)response.StatusCode, 1, (key, value) => value + 1);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            statusCodes.AddOrUpdate(0, 1, (key, value) => value + 1); // 0 for errors
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static double CalculatePercentile(List<double> values, int percentile)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var index = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        
        if (lower == upper)
        {
            return sorted[lower];
        }
        
        var weight = index - lower;
        return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }

    private List<VulnerabilityReport> AnalyzeLoadTestResults(LoadTestResult loadTestResult, string url)
    {
        var vulnerabilities = new List<VulnerabilityReport>();

        // Check error rate
        if (loadTestResult.ErrorRate > 10)
        {
            var severity = loadTestResult.ErrorRate > 50 ? SeverityLevel.Critical :
                          loadTestResult.ErrorRate > 25 ? SeverityLevel.High : SeverityLevel.Medium;

            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "LOAD-001",
                Name = "High Error Rate Under Load",
                Severity = severity,
                Category = "Performance",
                Scanner = ServiceName,
                Url = url,
                Description = $"High error rate of {loadTestResult.ErrorRate:F1}% observed during load testing.",
                Solution = "Investigate server capacity, resource limitations, and error handling under load.",
                Evidence = $"Error Rate: {loadTestResult.ErrorRate:F1}%, Total Requests: {loadTestResult.TotalRequests}, Failed: {loadTestResult.ErrorRequests}",
                DiscoveredAt = DateTime.UtcNow
            });
        }

        // Check response time performance
        if (loadTestResult.AverageResponseTime > 2000)
        {
            var severity = loadTestResult.AverageResponseTime > 5000 ? SeverityLevel.High :
                          loadTestResult.AverageResponseTime > 3000 ? SeverityLevel.Medium : SeverityLevel.Low;

            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "LOAD-002",
                Name = "Slow Response Times Under Load",
                Severity = severity,
                Category = "Performance",
                Scanner = ServiceName,
                Url = url,
                Description = $"Average response time of {loadTestResult.AverageResponseTime:F0}ms is slower than acceptable thresholds.",
                Solution = "Optimize application performance, database queries, and server resources.",
                Evidence = $"Average: {loadTestResult.AverageResponseTime:F0}ms, P95: {loadTestResult.P95ResponseTime:F0}ms, P99: {loadTestResult.P99ResponseTime:F0}ms",
                DiscoveredAt = DateTime.UtcNow
            });
        }

        // Check if P99 response time is significantly higher (indicates inconsistent performance)
        if (loadTestResult.P99ResponseTime > loadTestResult.AverageResponseTime * 5)
        {
            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "LOAD-003",
                Name = "Inconsistent Performance Under Load",
                Severity = SeverityLevel.Medium,
                Category = "Performance",
                Scanner = ServiceName,
                Url = url,
                Description = "Significant variance in response times indicates inconsistent performance under load.",
                Solution = "Investigate performance bottlenecks, optimize slow operations, and ensure consistent resource allocation.",
                Evidence = $"Average: {loadTestResult.AverageResponseTime:F0}ms, P99: {loadTestResult.P99ResponseTime:F0}ms (ratio: {loadTestResult.P99ResponseTime / loadTestResult.AverageResponseTime:F1}x)",
                DiscoveredAt = DateTime.UtcNow
            });
        }

        // Check for timeout errors
        var timeoutErrors = loadTestResult.Errors.Count(e => e.Contains("timeout", StringComparison.OrdinalIgnoreCase));
        if (timeoutErrors > loadTestResult.TotalRequests * 0.05) // More than 5% timeouts
        {
            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "LOAD-004",
                Name = "Request Timeouts Under Load",
                Severity = SeverityLevel.High,
                Category = "Performance",
                Scanner = ServiceName,
                Url = url,
                Description = $"High number of request timeouts ({timeoutErrors}) observed during load testing.",
                Solution = "Increase server timeout settings, optimize slow operations, or scale server capacity.",
                Evidence = $"Timeout Errors: {timeoutErrors}/{loadTestResult.TotalRequests} ({(double)timeoutErrors / loadTestResult.TotalRequests * 100:F1}%)",
                DiscoveredAt = DateTime.UtcNow
            });
        }

        // Check for server errors (5xx status codes)
        var serverErrors = loadTestResult.StatusCodes
            .Where(kvp => kvp.Key >= 500 && kvp.Key < 600)
            .Sum(kvp => kvp.Value);
        
        if (serverErrors > 0)
        {
            var serverErrorRate = (double)serverErrors / loadTestResult.TotalRequests * 100;
            var severity = serverErrorRate > 5 ? SeverityLevel.High : SeverityLevel.Medium;

            vulnerabilities.Add(new VulnerabilityReport
            {
                Id = "LOAD-005",
                Name = "Server Errors Under Load",
                Severity = severity,
                Category = "Performance",
                Scanner = ServiceName,
                Url = url,
                Description = $"Server errors (5xx) observed during load testing: {serverErrors} errors ({serverErrorRate:F1}%).",
                Solution = "Investigate server-side errors, check logs, and ensure proper error handling under load.",
                Evidence = $"Server Errors: {serverErrors}/{loadTestResult.TotalRequests} ({serverErrorRate:F1}%)",
                DiscoveredAt = DateTime.UtcNow
            });
        }

        return vulnerabilities;
    }

    private T GetConfigValue<T>(DomainConfig domain, string key, T defaultValue)
    {
        if (domain.ScannerSettings.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        // Load testing doesn't require external service, it's always available
        await Task.CompletedTask;
        return true;
    }

    public async Task<string> GetServiceVersionAsync()
    {
        // Return the internal version of our load testing service
        await Task.CompletedTask;
        return "SecurityScanner Load Test v1.0";
    }
}

public class LoadTestResult
{
    public string Url { get; set; } = string.Empty;
    public int RequestsPerSecond { get; set; }
    public int DurationSeconds { get; set; }
    public int RampUpSeconds { get; set; }
    public int MaxConcurrent { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double ActualDurationSeconds { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int ErrorRequests { get; set; }
    public double ErrorRate { get; set; }
    public double ActualRps { get; set; }
    public List<double> ResponseTimes { get; set; } = new();
    public Dictionary<int, int> StatusCodes { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public double AverageResponseTime { get; set; }
    public double MinResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
    public double MedianResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public double P99ResponseTime { get; set; }
}