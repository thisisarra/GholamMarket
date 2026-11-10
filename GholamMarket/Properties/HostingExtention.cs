using Applicationl;
using NibFramework;
using NibFramework.Extension;
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
            typeof(IRayaAuthAssemblyMarker).Assembly
        ];

        builder.Services.AddFrameworkModule(configuration, assemblies);


        return builder.Build();
    }
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
    private static void AddCustomJsonConfiguration(ConfigurationManager configuration, IWebHostEnvironment environment)
    {
        var configDirectory = Path.Combine(environment.WebRootPath, "config");

        configuration.AddJsonFile(Path.Combine(configDirectory, "RayaAuthSettings.json"), optional: false, reloadOnChange: true);
        configuration.AddJsonFile(Path.Combine(configDirectory, "MailServerSettings.json"), optional: false, reloadOnChange: true);

    }
}
