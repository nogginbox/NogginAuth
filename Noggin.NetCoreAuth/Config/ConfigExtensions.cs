using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers;
using System;

namespace Noggin.NetCoreAuth.Config
{
    public static class ConfigExtensions
    {
        public static IServiceCollection AddNogginNetCoreAuth(this IServiceCollection services, IConfigurationRoot configuration)
        {
            services.Configure<AuthConfigSection>(configuration.GetSection("NogginNetAuth"));
            services.AddSingleton<IProviderFactory, ProviderFactory>();
            return services;
        }

        public static IRouteBuilder MapNogginNetAuthRoutes(this IRouteBuilder routes, IServiceProvider services)
        {
            var providerFactory = services.GetRequiredService<IProviderFactory>();

            

            foreach(var provider in providerFactory.Providers)
            {
                // Todo: Check provider is supported
                // Can I set a type or class as a default
                routes.MapRoute(
                    name: $"NogginAuth_Redirect_{provider.Name}",
                    template: provider.RedirectTemplate,
                    defaults: new { controller = "NogginNetCoreAuth", action = "RedirectToProvider", provider = provider.Name });

                routes.MapRoute(
                    name: $"NogginAuth_Callback_{provider.Name}",
                    template: provider.CallbackTemplate,
                    defaults: new { controller = "NogginNetCoreAuth", action = "ProviderCallback", provider = provider.Name });
            }
            

            return routes;
        }
    }
}
