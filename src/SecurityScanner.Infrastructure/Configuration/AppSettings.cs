using SecurityScanner.Core.Models;

namespace SecurityScanner.Infrastructure.Configuration;

public class AppSettings
{
    public ScannerSettings ScannerSettings { get; set; } = new();
    public ReportSettings ReportSettings { get; set; } = new();
    public NotificationSettings NotificationSettings { get; set; } = new();
    public PerformanceSettings PerformanceSettings { get; set; } = new();
    public LoggingSettings LoggingSettings { get; set; } = new();
}

public class ScannerSettings
{
    public OwaspZapSettings OwaspZap { get; set; } = new();
    public SslLabsSettings SslLabs { get; set; } = new();
    public SecurityHeadersSettings SecurityHeaders { get; set; } = new();
    public LoadTestSettings LoadTest { get; set; } = new();
}

public class OwaspZapSettings
{
    public string ApiUrl { get; set; } = "http://localhost:8080";
    public string? ApiKey { get; set; }
    public TimeSpan ScanTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public bool EnableActiveScan { get; set; } = true;
    public bool EnablePassiveScan { get; set; } = true;
}

public class SslLabsSettings
{
    public string ApiUrl { get; set; } = "https://api.ssllabs.com/api/v3";
    public TimeSpan AnalysisTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public bool FromCache { get; set; } = false;
    public int MaxAge { get; set; } = 24;
}

public class SecurityHeadersSettings
{
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan DelayBetweenRequests { get; set; } = TimeSpan.FromMilliseconds(100);
    public int MaxRedirects { get; set; } = 5;
}

public class ReportSettings
{
    public string OutputDirectory { get; set; } = "reports";
    public bool IncludeTimestamp { get; set; } = true;
    public string DefaultFormat { get; set; } = "json";
    public bool CompressLargeReports { get; set; } = true;
}

public class NotificationSettings
{
    public bool Enabled { get; set; } = false;
    public EmailSettings Email { get; set; } = new();
    public SlackSettings Slack { get; set; } = new();
    public TeamsSettings Teams { get; set; } = new();
}

public class EmailSettings
{
    public bool Enabled { get; set; } = false;
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public List<string> ToAddresses { get; set; } = new();
}

public class SlackSettings
{
    public bool Enabled { get; set; } = false;
    public string? WebhookUrl { get; set; }
    public string Channel { get; set; } = "#security";
}

public class TeamsSettings
{
    public bool Enabled { get; set; } = false;
    public string? WebhookUrl { get; set; }
}

public class PerformanceSettings
{
    public int MaxConcurrentScans { get; set; } = 3;
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableRetries { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}

public class LoggingSettings
{
    public string LogLevel { get; set; } = "Information";
    public bool LogToFile { get; set; } = true;
    public string LogDirectory { get; set; } = "logs";
    public bool IncludeScopes { get; set; } = true;
    public int MaxLogFileSizeMB { get; set; } = 10;
}