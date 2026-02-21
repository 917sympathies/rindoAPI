using Application.Auth;
using Microsoft.Extensions.Hosting;

namespace Application.Services.Background;

public class AuthCacheClearingBackgroundService: BackgroundService
{
    private readonly IAuthCacheService _authCacheService;

    public AuthCacheClearingBackgroundService(IAuthCacheService authCacheService)
    {
        _authCacheService = authCacheService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
        {
            var refreshTokens = await _authCacheService.GetAllRefreshTokensAsync();
            foreach (var refreshToken in refreshTokens.Where(x => x.Value.ValidTo < DateTime.Now))
            {
                await _authCacheService.RemoveRefreshTokenAsync(refreshToken.Key);
            }
        }
    }
}