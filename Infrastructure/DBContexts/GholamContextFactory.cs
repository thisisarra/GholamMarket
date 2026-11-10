using Infrastructure.DBContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Pinex.Data.EF.DBContexts;

public class GholamContextFactory : IDesignTimeDbContextFactory<GholamContext>
{
    public GholamContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(),  "..", "4.Presentation", "Pinex");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("GholamDBConnection")
            ?? throw new InvalidOperationException("Connection string 'GholamDBConnection' not found.");

        var optionsBuilder = new DbContextOptionsBuilder<GholamContext>();
        optionsBuilder
            .UseSqlServer(connectionString);

        return new GholamContext(optionsBuilder.Options);
    }
}
