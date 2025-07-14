using Microsoft.Extensions.Logging;

namespace SecurityScanner.Infrastructure.Http;

public class RateLimitingHandler : DelegatingHandler
{
    private readonly TimeSpan _delay;
    private readonly ILogger _logger;
    private DateTime _lastRequest = DateTime.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public RateLimitingHandler(TimeSpan delay, ILogger logger)
    {
        _delay = delay;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequest;
            
            if (timeSinceLastRequest < _delay)
            {
                var waitTime = _delay - timeSinceLastRequest;
                _logger.LogDebug("Rate limiting: waiting {WaitTimeMs}ms before request to {RequestUri}", 
                    waitTime.TotalMilliseconds, request.RequestUri);
                
                await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
            }

            _lastRequest = DateTime.UtcNow;
            
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _semaphore?.Dispose();
        }
        base.Dispose(disposing);
    }
}