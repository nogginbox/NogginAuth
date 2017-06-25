using System.Collections.Generic;

namespace Noggin.NetCoreAuth.Config
{
    public class AuthConfigSection
    {
        public string DefaultCallbackTemplate { get; set; }
        public string DefaultRedirectTemplate { get; set; }
        public IList<ProviderConfig> Providers { get; set; }
    }
}