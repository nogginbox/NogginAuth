using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Noggin.NetCoreAuth.Providers;
using System;

namespace Noggin.NetCoreAuth.Config;

public static class ConfigExtensions
{
    public static IServiceCollection AddNogginNetCoreAuth<TLoginHandler>(this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = "NogginNetAuth") where TLoginHandler:class,ILoginHandler
    {
        services.Configure<AuthConfigSection>(configuration.GetSection(configSectionName));
        services.AddSingleton<IProviderFactory, ProviderFactory>();
        services.AddSingleton<IRestClientFactory, RestSharpClientFactory>();
        services.AddScoped<ILoginHandler, TLoginHandler>();
        return services;
    }



    public static IEndpointRouteBuilder MapNogginNetAuthRoutes(this IEndpointRouteBuilder routes, IServiceProvider services)
    {
        var providerFactory = services.GetRequiredService<IProviderFactory>();

        foreach (var provider in providerFactory.Providers)
        {
            // Todo: Check provider is supported
            // Can I set a type or class as a default
            routes.MapControllerRoute(
                name: $"NogginAuth_Redirect_{provider.Name}",
                pattern: provider.RedirectTemplate,
                defaults: new { controller = "NogginNetCoreAuth", action = "RedirectToProvider", provider = provider.Name });

            routes.MapControllerRoute(
                name: $"NogginAuth_Callback_{provider.Name}",
                pattern: provider.CallbackTemplate,
                defaults: new { controller = "NogginNetCoreAuth", action = "ProviderCallback", provider = provider.Name });
        }

        return routes;
    }
}
