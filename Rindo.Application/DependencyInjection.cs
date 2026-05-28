using System.Text;
using Application.Auth;
using Application.Auth.Jwt;
using Application.Interfaces.Services;
using Application.Services;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Rindo.Domain.DataObjects;
using Rindo.Domain.DTO;
using Rindo.Domain.DTO.Projects;
using Rindo.Domain.DTO.Roles;
using Task = System.Threading.Tasks.Task;

namespace Application;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IStageService, StageService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IAuthCacheService, AuthCacheService>();
        AddMappingConfigs();
    }

    public static void AddJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>();
        if (jwtOptions is null)
        {
            throw new InvalidOperationException("You haven't set JWT settings in configuration file");
        }
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies[jwtOptions.CookiesName];
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization();
        services.Configure<JwtOptions>(configuration.GetSection(nameof(JwtOptions)));
    }

    private static void AddMappingConfigs()
    {
        TypeAdapterConfig.GlobalSettings.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);
        TypeAdapterConfig<Project, ProjectDto>
            .ForType()
            .Map(dest => dest.Users, source => source.Users.Select(user => user.Adapt<UserDto>()))
            .Map(dest => dest.Roles, source => source.Roles.Select(user => user.Adapt<RoleDto>()));
        TypeAdapterConfig<Project, ProjectShortInfoDto>
            .ForType()
            .Map(dest => dest.Id, source => source.ProjectId);
    }
}