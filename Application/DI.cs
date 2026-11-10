using Applicationl;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DI
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(IAppAssemblyMarker).Assembly);


        return services;
    }
}
