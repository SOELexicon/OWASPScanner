namespace SecurityScanner.Core.Exceptions;

public class ScannerException : Exception
{
    public string? ScannerName { get; }
    public string? Domain { get; }

    public ScannerException() { }

    public ScannerException(string message) : base(message) { }

    public ScannerException(string message, Exception innerException) : base(message, innerException) { }

    public ScannerException(string message, string scannerName, string domain) : base(message)
    {
        ScannerName = scannerName;
        Domain = domain;
    }

    public ScannerException(string message, string scannerName, string domain, Exception innerException) 
        : base(message, innerException)
    {
        ScannerName = scannerName;
        Domain = domain;
    }
}