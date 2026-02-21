using Microsoft.Extensions.Caching.Distributed;

namespace Application.Interfaces.Caching;

public interface IExtendedDistributedCache: IDistributedCache
{
    Task<KeyValuePair<string, string>[]> GetByKeysPrefixAsync(string prefix);
}