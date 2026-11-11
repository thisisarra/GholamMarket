using Infrastructure.DBContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NibFramework.Extension;

namespace Infrastructure;

public static class DI
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GholamContext>(options => options
                .UseSqlServer(configuration.GetConnectionString("GholamDBConnection")));

        services.AddNibFrameworkPersistence<GholamContext>();
        services.AddScoped<Application.Store.IPaymentGateway, Payments.DevelopmentPaymentGateway>();

        return services;
    }
}
