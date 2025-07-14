namespace SecurityScanner.Core.Models;

public enum ScanStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled,
    Timeout
}