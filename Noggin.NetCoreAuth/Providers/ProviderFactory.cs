using Microsoft.Extensions.Options;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers.Facebook;
using Noggin.NetCoreAuth.Providers.GitHub;
using Noggin.NetCoreAuth.Providers.Google;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Noggin.NetCoreAuth.Providers;

internal class ProviderFactory : IProviderFactory
{
    private readonly IList<ProviderConfig> _providerConfigs;
    private readonly IDictionary<string, Provider> _providers;
    private readonly string _defaultRedirectTemplate;
    private readonly string _defaultCallbackTemplate;

    public ProviderFactory(IOptions<AuthConfigSection> config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        _providerConfigs = config.Value?.Providers;
        _providers = new Dictionary<string, Provider>();

        _defaultRedirectTemplate = CreateDefaultTemplate(config.Value.DefaultRedirectTemplate, "auth/redirect/{provider}");
        _defaultCallbackTemplate = CreateDefaultTemplate(config.Value.DefaultCallbackTemplate, "auth/callback/{provider}");

        // Allow null/empty provider list at this stage, do not throw config error till app tries to use login
        Providers = _providerConfigs?.Select(p => Get(p.Name)).ToList() ?? new List<Provider>();

        // Thought: Lazy Dictionary, or simplify getting providers as all pre initted
    }

    public Provider Get(string name)
    {
        return name.ToLower() switch
        {
            "facebook" => Get(name, (x) => new FacebookProvider(x, _defaultRedirectTemplate, _defaultCallbackTemplate)),
            "github" => Get(name, (x) => new GitHubProvider(x, _defaultRedirectTemplate, _defaultCallbackTemplate)),
            "google" => Get(name, (x) => new GoogleProvider(x, _defaultRedirectTemplate, _defaultCallbackTemplate)),
            _ => throw new NogginNetCoreConfigException($"No provider called {name} found"),
        };
    }

    private Provider Get(string name, Func<ProviderConfig, Provider> createProvider)
    {
        if (_providers.ContainsKey(name))
        {
            return _providers[name];
        }

        var config = (_providerConfigs?.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            ?? throw new NogginNetCoreConfigException($"No provider config section found for {name}");

        var provider = createProvider(config);
        _providers[name] = provider;
        return provider;
    }

    public IList<Provider> Providers { get; }

    private static string CreateDefaultTemplate(string template1, string template2)
    {
        if (string.IsNullOrEmpty(template1)) return template2;

        if (template1.Contains("{provider}")) return template1;

        throw new NogginNetCoreAuthException("Default Url Templates must contain '{provider}' for the provider name.");
    }

    
}
