using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rindo.Domain.DataObjects;

namespace Application.Auth.Jwt;

public record TokenResult(string Token, string RefreshToken, JwtSecurityToken RefreshTokenValue, TimeSpan RefreshTokenExpires);

public interface IJwtProvider
{
    TokenResult GenerateToken(Guid userId);
}

public class JwtProvider(IOptions<JwtOptions> options) : IJwtProvider
{
    private readonly JwtOptions _options = options.Value;

    public TokenResult GenerateToken(Guid userId)
    {
        Claim[] claims =
        [
            new ("userId", userId.ToString())
        ];
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)), SecurityAlgorithms.HmacSha256);
        var tokenValue = new JwtSecurityToken(claims: claims, signingCredentials: signingCredentials, expires: DateTime.UtcNow.AddHours(_options.ExpiresMinutes));
        var token = new JwtSecurityTokenHandler().WriteToken(tokenValue);
        var refreshTokenValue = new JwtSecurityToken(claims: claims, signingCredentials: signingCredentials, expires: DateTime.UtcNow.AddDays(_options.RefreshTokenExpiresDays));
        var refreshToken = new JwtSecurityTokenHandler().WriteToken(refreshTokenValue);
        return new TokenResult(token, refreshToken, refreshTokenValue, TimeSpan.FromDays(_options.RefreshTokenExpiresDays));
    }
}