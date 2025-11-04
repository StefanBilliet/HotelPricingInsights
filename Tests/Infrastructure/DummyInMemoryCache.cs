using Polly.Caching;

namespace Tests.Infrastructure;

public class DummyInMemoryCache : IAsyncCacheProvider
{
    private readonly Dictionary<string, object> _cache = new();
    
    public Task<(bool, object?)> TryGetAsync(string key, CancellationToken cancellationToken, bool continueOnCapturedContext)
    {
        return Task.FromResult(_cache.TryGetValue(key, out var value) ? (true, value) : (false, null));
    }

    public Task PutAsync(string key, object value, Ttl ttl, CancellationToken cancellationToken, bool continueOnCapturedContext)
    {
        _cache[key] = value;
        return Task.CompletedTask;
    }
}