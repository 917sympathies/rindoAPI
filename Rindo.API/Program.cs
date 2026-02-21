using Application;
using Application.Interfaces.Access;
using Application.Services.Background;
using NLog;
using NLog.Web;
using Rindo.API.Common;
using Rindo.API.Middleware.Authentication;
using Rindo.API.Middleware.Exceptions;
using Rindo.API.Middleware.Logging;
using Rindo.Chat;
using Rindo.Infrastructure;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Starting app");

try
{
    
    var builder = WebApplication.CreateBuilder(args);
    
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();
    
    builder.Services
        .AddControllers(options =>
        {
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        })
        .AddNewtonsoftJson(options => 
        { 
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore; 
        });
    builder.Services.AddHostedService<AuthCacheClearingBackgroundService>(); 
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Configuration
        .AddJsonFile("appsettings.db.json", optional: false)
        .AddJsonFile("appsettings.auth.json", optional: false);
    
    builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
    builder.Services.AddCors(options =>
        options.AddPolicy("CorsPolicy",
            conf => conf
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .SetIsOriginAllowed(_ => true)));
    
    builder.Services
        .AddInfrastructure(builder.Configuration)
        .AddHttpContextAccessor()
        .AddRepositories()
        .AddApplication();
    
    builder.Services.AddScoped<IDataAccessController, DataAccessController>();
    
    builder.Services.AddJwt(builder.Configuration);
    builder.Services.AddSignalR();
    
    var app = builder.Build();
    
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        // app.ApplyMigrations();    
    }

    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<ExceptionHandlerMiddleware>();
    
    app.UseCors("CorsPolicy"); 
    
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.UseMiddleware<AuthenticationMiddleware>();
    
    app.MapControllers();
    
    app.MapHub<ChatHub>("/chat");
    
    app.Run();
}
catch (Exception exception)
{
    // catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    LogManager.Shutdown();
}