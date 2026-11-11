using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Raya.Auth.Configuration;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Reflection;

namespace GholamMarket.Properties;

public static class DI
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizeFolder("/Account");
            options.Conventions.AllowAnonymousToPage("/Account/Login");
            options.Conventions.AllowAnonymousToPage("/Account/Register");
            options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
            options.Conventions.AllowAnonymousToPage("/Account/Logout");
            options.Conventions.AuthorizeFolder("/Checkout");
            options.Conventions.AuthorizeAreaFolder("Admin", "/", "AdminOnly");
        });

        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole(DefaultRoles.Admin));

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
        });

        services.PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
        });

        services.AddEndpointsApiExplorer();

        services.AddCors(options =>
        {
            options.AddPolicy("gholamPolicy", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        services.AddSwaggerGen(option =>
        {
            option.ParameterFilter<EnumParameterFilter>();
            option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            option.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
     
        return services;
    }
    public class EnumParameterFilter : IParameterFilter
    {
        public void Apply(
        OpenApiParameter parameter,
        ParameterFilterContext context)
        {
            var type = context.ApiParameterDescription.Type;

            if (type == null)
                return;

            var enumType = Nullable.GetUnderlyingType(type) ?? type;

            if (!enumType.IsEnum)
                return;

            var enumNames = new OpenApiArray();
            var enumDescriptions = new OpenApiArray();
            var descriptionLines = new List<string>();

            foreach (var value in Enum.GetValues(enumType))
            {
                var name = Enum.GetName(enumType, value);

                if (name == null)
                    continue;

                var field = enumType.GetField(name);

                var description = field?
                    .GetCustomAttribute<DescriptionAttribute>()?
                    .Description ?? name;

                enumNames.Add(new OpenApiString(name));
                enumDescriptions.Add(new OpenApiString(description));

                descriptionLines.Add(
                    $"{name} : {Convert.ToInt32(value)}");
            }

            parameter.Schema.Extensions["x-enumNames"] = enumNames;
            parameter.Schema.Extensions["x-enumDescriptions"] = enumDescriptions;

            parameter.Description = string.Join(
                " | ",
                descriptionLines);
        }
    }
}
