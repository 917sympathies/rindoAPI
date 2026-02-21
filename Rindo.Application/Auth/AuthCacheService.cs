using System.IdentityModel.Tokens.Jwt;
using Application.Interfaces.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DistributedCacheEntryOptions = Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions;

namespace Application.Auth;


public class RedisKeyValue<T>
{
    public string Key { get; set; }
    public T Value { get; set; }
}

public interface IAuthCacheService
{
    Task InsertRefreshTokenAsync(string refreshToken, JwtSecurityToken refreshTokenValue, TimeSpan refreshTokenExpires);
    Task<JwtSecurityToken?> GetRefreshTokenAsync(string refreshToken);
    Task<IEnumerable<RedisKeyValue<JwtSecurityToken>>> GetAllRefreshTokensAsync();
    Task RemoveRefreshTokenAsync(string tokenKey);
}

public class AuthCacheService(IExtendedDistributedCache extendedDistributedCache, ILogger logger): IAuthCacheService
{
    private const string Prefix = "auth";

    public async Task InsertRefreshTokenAsync(string refreshToken, JwtSecurityToken refreshTokenValue, TimeSpan refreshTokenExpires)
    {
        var settings = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = refreshTokenExpires,
        };
        
        await extendedDistributedCache.SetStringAsync($"{Prefix}-refresh-${refreshToken}", JsonConvert.SerializeObject(refreshTokenValue), settings);
    }

    public async Task<JwtSecurityToken?> GetRefreshTokenAsync(string refreshToken)
    {
        var redisValue = await extendedDistributedCache.GetStringAsync($"{Prefix}-refresh-${refreshToken}");
        return redisValue is null ? null : JsonConvert.DeserializeObject<JwtSecurityToken>(redisValue);
    }

    public async Task<IEnumerable<RedisKeyValue<JwtSecurityToken>>> GetAllRefreshTokensAsync()
    {
        var redisKeyValuePairs = await extendedDistributedCache.GetByKeysPrefixAsync("auth-");
        var result = new List<RedisKeyValue<JwtSecurityToken>>();
        if (redisKeyValuePairs.Any())
        {
            foreach (var pair in redisKeyValuePairs)
            {
                try
                {
                    result.Add(new RedisKeyValue<JwtSecurityToken>
                    {
                        Key = pair.Key, 
                        Value = JsonConvert.DeserializeObject<JwtSecurityToken>(pair.Value)
                    });
                }
                catch(NullReferenceException)
                {
                    logger.LogError($"Error while reading refresh tokens from redis: {pair.Key}");
                }
            }
        }
        return result;
    }

    public async Task RemoveRefreshTokenAsync(string tokenKey)
    {
        await extendedDistributedCache.RemoveAsync($"{Prefix}-refresh-${tokenKey}");
    }
}