using GholamMarket.Properties;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


var app = builder.ConfigureServices().ConfigurePipeline();

// Configure the HTTP request pipeline.

app.Run();
