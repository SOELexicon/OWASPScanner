using SecurityScanner.Core.Models;

namespace SecurityScanner.Core.Interfaces;

public interface IScannerService
{
    string ServiceName { get; }
    ScannerType ScannerType { get; }
    
    Task<ScanResult> ScanAsync(DomainConfig domain, CancellationToken cancellationToken = default);
    Task<bool> IsServiceAvailableAsync();
    Task<string> GetServiceVersionAsync();
}