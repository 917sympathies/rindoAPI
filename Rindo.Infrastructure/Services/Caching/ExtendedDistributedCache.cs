using Application.Interfaces.Caching;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace Rindo.Infrastructure.Services.Caching;

public class ExtendedDistributedCache: IExtendedDistributedCache
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;

    public ExtendedDistributedCache(IDistributedCache distributedCache, IConnectionMultiplexer redis)
    {
        _distributedCache = distributedCache;
        _redis = redis;
    }

    public byte[]? Get(string key)
    {
        return _distributedCache.Get(key);
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token)
    {
        return _distributedCache.GetAsync(key, token);
    }

    public void Refresh(string key)
    {
        _distributedCache.Refresh(key);
    }

    public Task RefreshAsync(string key, CancellationToken token)
    {
        return _distributedCache.RefreshAsync(key, token);
    }

    public void Remove(string key)
    {
        _distributedCache.Remove(key);
    }

    public Task RemoveAsync(string key, CancellationToken token)
    {
        return _distributedCache.RemoveAsync(key, token);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        _distributedCache.Set(key, value, options);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token)
    {
        return _distributedCache.SetAsync(key, value, options, token);
    }

    public async Task<KeyValuePair<string, string>[]> GetByKeysPrefixAsync(string prefix)
    {
        var pairs = new List<KeyValuePair<string, string>>();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var db = _redis.GetDatabase();
        
        await foreach (var key in server.KeysAsync(pattern: $"{prefix}*", pageSize: 100))
        {
            var value = await db.StringGetAsync(key);
            if (value.HasValue)
            {
                var originalKey = key.ToString();
                pairs.Add(new KeyValuePair<string, string>(originalKey, value!));
            }
        }
        
        return pairs.ToArray();
    }
}