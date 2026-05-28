using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DistributedCacheEntryOptions = Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions;

namespace Application.Auth;

public interface IAuthCacheService
{
    Task InsertRefreshTokenAsync(string refreshToken, JwtSecurityToken refreshTokenValue, TimeSpan refreshTokenExpires);
    Task<JwtSecurityToken?> GetRefreshTokenAsync(string refreshToken);
    Task RemoveRefreshTokenAsync(string tokenKey);
}

public class AuthCacheService(IDistributedCache distributedCache, ILogger<AuthCacheService> logger): IAuthCacheService
{
    private const string Prefix = "auth";

    public async Task InsertRefreshTokenAsync(string refreshToken, JwtSecurityToken refreshTokenValue, TimeSpan refreshTokenExpires)
    {
        var settings = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = refreshTokenExpires,
        };
        
        await distributedCache.SetStringAsync($"{Prefix}-refresh-${refreshToken}", JsonConvert.SerializeObject(refreshTokenValue), settings);
    }

    public async Task<JwtSecurityToken?> GetRefreshTokenAsync(string refreshToken)
    {
        var redisValue = await distributedCache.GetStringAsync($"{Prefix}-refresh-${refreshToken}");
        return redisValue is null ? null : JsonConvert.DeserializeObject<JwtSecurityToken>(redisValue);
    }

    public async Task RemoveRefreshTokenAsync(string tokenKey)
    {
        await distributedCache.RemoveAsync($"{Prefix}-refresh-${tokenKey}");
    }
}