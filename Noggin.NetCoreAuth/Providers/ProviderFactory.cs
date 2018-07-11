using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers.Twitter;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Extensions.Options;
using Noggin.NetCoreAuth.Providers.Facebook;
using Noggin.NetCoreAuth.Providers.Google;

namespace Noggin.NetCoreAuth.Providers
{
    internal class ProviderFactory : IProviderFactory
    {
        private readonly IList<ProviderConfig> _providerConfigs;
        private readonly IDictionary<string, Provider> _providers;
        private readonly IRestClientFactory _restClientFactory;
        private readonly string _defaultRedirectTemplate;
        private readonly string _defaultCallbackTemplate;

        public ProviderFactory(IOptions<AuthConfigSection> config, IRestClientFactory restClientFactory)
        {
            _providerConfigs = config.Value?.Providers;
            _providers = new Dictionary<string, Provider>();
            _restClientFactory = restClientFactory;

            _defaultRedirectTemplate = CreateDefaultTemplate(config.Value.DefaultRedirectTemplate, "auth/redirect/{provider}");
            _defaultCallbackTemplate = CreateDefaultTemplate(config.Value.DefaultCallbackTemplate, "auth/callbackback/{provider}");

            // Allow null/empty provider list at this stage, do not throw config error till app tries to use login
            Providers = _providerConfigs?.Select(p => Get(p.Name)).ToList() ?? new List<Provider>();

            // Thought: Lazy Dictionary, or simplyfy getting providers as all pre initted
        }

        public Provider Get(string name)
        {
            switch(name.ToLower())
            {
				case "facebook":
					return Get(name, (x) => new FacebookProvider(x, _restClientFactory, _defaultRedirectTemplate, _defaultCallbackTemplate));
				case "google":
					return Get(name, (x) => new GoogleProvider(x, _restClientFactory, _defaultRedirectTemplate, _defaultCallbackTemplate));
				case "twitter":
                    return Get(name, (x) => new TwitterProvider(x, _restClientFactory, _defaultRedirectTemplate, _defaultCallbackTemplate));
                default:
                    throw new NogginNetCoreConfigException($"No provider called {name} found");
            }
        }

        private Provider Get(string name, Func<ProviderConfig, Provider> createProvider)
        {
            if (_providers.ContainsKey(name))
            {
                return _providers[name];
            }

            var config = _providerConfigs?.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (config == null)
            {
                throw new NogginNetCoreConfigException($"No provider config section found for {name}");
            }

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

    public interface IProviderFactory
    {
        Provider Get(string name);
        IList<Provider> Providers { get; }
    }
}
