namespace SecurityScanner.Core.Interfaces;

public interface ICommandHandler<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

public interface ICommandMiddleware
{
    Task<TResponse> InvokeAsync<TRequest, TResponse>(
        TRequest request, 
        Func<TRequest, Task<TResponse>> next, 
        CancellationToken cancellationToken = default);
}