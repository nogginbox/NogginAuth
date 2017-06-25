using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Noggin.NetCoreAuth.Exceptions;

namespace Noggin.NetCoreAuth.Config
{
    public static class ConfigExtensions
    {
        public static IRouteBuilder MapNogginNetAuthRoutes(this IRouteBuilder routes, IConfigurationSection configSection)
        {
            var config = configSection.Get<AuthConfigSection>();
            var defaultRedirectTemplate = SetDefaultTemplate(config.DefaultRedirectTemplate, "auth/redirect/{provider}");
            var defaultCallbackTemplate = SetDefaultTemplate(config.DefaultCallbackTemplate, "auth/callbackback/{provider}");

            foreach(var provider in config.Providers)
            {
                // Todo: Check provider is supported
                // Can I set a type or class as a default
                routes.MapRoute(
                    name: $"NogginAuth_Redirect_{provider.Name}",
                    template: SetTemplate(provider.Name, provider.RedirectTemplate, defaultRedirectTemplate),
                    defaults: new { controller = "NogginNetCoreAuth", action = "RedirectToProvider", provider = provider.Name });

                routes.MapRoute(
                    name: $"NogginAuth_Callback_{provider.Name}",
                    template: SetTemplate(provider.Name, provider.CallbackTemplate, defaultCallbackTemplate),
                    defaults: new { controller = "NogginNetCoreAuth", action = "RedirectToProvider", provider = provider.Name });
            }
            

            return routes;
        }

        private static string SetDefaultTemplate(string template1, string template2)
        {
            if (string.IsNullOrEmpty(template1)) return template2;

            if (template1.Contains("{provider}")) return template1;

            throw new NogginNetCoreAuthException("Default Url Templates must contain '{provider}' for the provider name.");
        }

        private static string SetTemplate(string providerName, string template1, string templateTemplate)
        {
            if (!string.IsNullOrEmpty(template1)) return template1;

            return template1.Replace("{provider}", providerName);
        }
    }
}
