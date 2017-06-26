using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers.Twitter;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Noggin.NetCoreAuth.Providers
{
    public class ProviderFactory : IProviderFactory
    {
        private readonly IList<ProviderConfig> _providerConfigs;
        private readonly IDictionary<string, Provider> _providers;

        public ProviderFactory(IOptions<AuthConfigSection> config)
        {
            _providerConfigs = config.Value.Providers;
            _providers = new Dictionary<string, Provider>();
        }

        public Provider Get(string name)
        {
            switch(name.ToLower())
            {
                case "twitter":
                    return Get(name, (x) => new TwitterProvider(x));
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

            var config = _providerConfigs.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            var provider = createProvider(config);
            _providers[name] = provider;
            return provider;
        }
    }

    public interface IProviderFactory
    {
        Provider Get(string name);
    }
}
