using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.DBContexts;

public class GholamContextFactory : IDesignTimeDbContextFactory<GholamContext>
{
    public GholamContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(FindSettingsDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("GholamDBConnection")
            ?? throw new InvalidOperationException("Connection string 'GholamDBConnection' not found.");

        var optionsBuilder = new DbContextOptionsBuilder<GholamContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new GholamContext(optionsBuilder.Options);
    }

    private static string FindSettingsDirectory()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (ContainsGholamConnection(current.FullName))
                return current.FullName;

            var webProject = Path.Combine(current.FullName, "GholamMarket");
            if (ContainsGholamConnection(webProject))
                return webProject;

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not find GholamMarket appsettings.json.");
    }

    private static bool ContainsGholamConnection(string directory)
    {
        var settings = Path.Combine(directory, "appsettings.json");
        return File.Exists(settings) &&
               File.ReadAllText(settings).Contains("GholamDBConnection", StringComparison.Ordinal);
    }
}
