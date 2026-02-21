using Application.Auth.Jwt;
using Application.Interfaces.Caching;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rindo.Infrastructure.Repositories;
using Rindo.Infrastructure.Repositories.Cached;
using Rindo.Infrastructure.Services.Caching;
using Rindo.Infrastructure.Services.Transactions;
using StackExchange.Redis;

namespace Rindo.Infrastructure;

public record ConnectionStrings(string POSTGRESQL, string REDIS, string RABBITMQ);

public static class DependencyInjection
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<PostgresDbContext>();
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }
    
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dbOptions = configuration.GetSection(nameof(ConnectionStrings)).Get<ConnectionStrings>();
        if (dbOptions is null)
        {
            throw new InvalidOperationException("You must provide a connection string in configuration");
        }
        services.AddDbContext<PostgresDbContext>(options => options
                .UseNpgsql(dbOptions.POSTGRESQL, b => b.MigrationsAssembly("Rindo.API"))
                .UseSnakeCaseNamingConvention());
        
        // Redis
        services.AddStackExchangeRedisCache(redisOptions =>
        {
            redisOptions.Configuration = dbOptions.REDIS;
        });
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(dbOptions.REDIS));

        services.AddScoped<IExtendedDistributedCache>(serviceProvider =>
        {
            var cache = serviceProvider.GetRequiredService<IDistributedCache>();
            var redis = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            return new ExtendedDistributedCache(cache, redis);
        });
        
        services.AddScoped<IRedisCacheService, RedisCacheService>();
        services.AddScoped<IDataTransactionService, DataTransactionService>();
        
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ProjectRepository>();
        services.AddScoped<IProjectRepository, CachedProjectRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();
        services.AddScoped<IStageRepository, StageRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IJwtProvider, JwtProvider>();
        
        return services;
    }
}