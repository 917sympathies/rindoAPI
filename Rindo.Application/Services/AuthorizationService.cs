using System.ComponentModel.DataAnnotations;
using Application.Auth;
using Application.Auth.Jwt;
using Application.Common.Exceptions;
using Application.Common.Mapping;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Rindo.Domain.DTO.Auth;
using Rindo.Domain.DataObjects;

namespace Application.Services;

public class AuthorizationService(IUserRepository userRepository, IJwtProvider jwtProvider, IAuthCacheService authCacheService) : IAuthorizationService
{
    public async Task<User> SignUpUser(SignUpDto signUpDto)
    {
        var isUserExist = await userRepository.GetUserByUsername(signUpDto.Username) is not null;
        if (isUserExist) throw new ValidationException("User with this name already exists");
        
        var user = signUpDto.MapToModel();
        user.Password = PasswordHandler.GetPasswordHash(signUpDto.Password);
        
        return await userRepository.CreateUser(user);
    }

    public async Task<TokenDto> AuthUser(LoginDto loginDto)
    {
        var user = await userRepository.GetUserByUsername(loginDto.Username);
        if (user is null) throw new NotFoundException("User with this username doesn't exists");
        
        var password = PasswordHandler.GetPasswordHash(loginDto.Password);
        if (!user.Password.Equals(password)) throw new ValidationException("Wrong password");
        
        return await GenerateToken(user);
    }

    public async Task<TokenDto> RefreshToken(string refreshToken, Guid userId)
    {
        var refreshTokenValue = await authCacheService.GetRefreshTokenAsync(refreshToken);
        if (refreshTokenValue is null || refreshTokenValue.ValidTo < DateTime.Now)
        {
            throw new ValidationException("Refresh token expired");
        }
        var user = await userRepository.GetUserById(userId);
        if (user is null) throw new NotFoundException("User with this username doesn't exists");

        return await GenerateToken(user);
    }

    private async Task<TokenDto> GenerateToken(User user)
    {
        var tokenResult = jwtProvider.GenerateToken(user.UserId);
        
        await authCacheService.InsertRefreshTokenAsync(tokenResult.RefreshToken, tokenResult.RefreshTokenValue, tokenResult.RefreshTokenExpires);
        
        return new TokenDto
        {
            Token = tokenResult.Token,
            RefreshToken = tokenResult.RefreshToken,
            User = user.MapToDto()
        };
    }
}