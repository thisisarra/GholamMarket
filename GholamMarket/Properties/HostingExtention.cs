using Application;
using Applicationl;
using Domain.Entities.Users;
using FluentValidation;
using Infrastructure;
using Infrastructure.DBContexts;
using Infrastructure.Store;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NibFramework;
using NibFramework.Extension;
using NibFramework.Model;
using Pinex.Domain.Entities.Users;
using Raya.Auth;
using System.Reflection;

namespace GholamMarket.Properties;

public static class HostingExtention
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        AddCustomJsonConfiguration(configuration, environment);

        builder.Services.Configure<FrameworkAppSetting>(configuration);

        List<Assembly> assemblies =
        [
            typeof(Program).Assembly,
            typeof(IAppAssemblyMarker).Assembly,
            typeof(IInfraAssemblyMarker).Assembly,
            typeof(IRayaAuthAssemblyMarker).Assembly
        ];

        builder.Services
            .AddFrameworkModule(configuration, assemblies)
            .AddApplication()
            .AddInfrastructure(configuration)
            .AddIdentityAndJwtServices<GholamContext, User, Role, UserRole>(
                    configuration,
                    registerDbContext: false)
            .AddApi();

        return builder.Build();
    }
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/NotFound");
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapControllers();
        app.MapRazorPages();

        return app;
    }
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GholamMarket.Database");

        logger.LogInformation("Applying database migrations and seed data.");

        var context = scope.ServiceProvider.GetRequiredService<GholamContext>();
        await context.Database.MigrateAsync();
        await DevelopmentSeeder.SeedAsync(scope.ServiceProvider);

        logger.LogInformation("Database migrations and seed data are up to date.");
    }
    private static void AddCustomJsonConfiguration(ConfigurationManager configuration, IWebHostEnvironment environment)
    {
        var configDirectory = Path.Combine(environment.WebRootPath, "config");

        configuration.AddJsonFile(Path.Combine(configDirectory, "RayaAuthSettings.json"), optional: false, reloadOnChange: true);
    }
}
