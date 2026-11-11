using GholamMarket.Properties;

var builder = WebApplication.CreateBuilder(args);

var app = builder.ConfigureServices().ConfigurePipeline();
await app.ApplyDatabaseMigrationsAsync();

app.Run();
