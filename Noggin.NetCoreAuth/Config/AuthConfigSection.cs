using System.Collections.Generic;

namespace Noggin.NetCoreAuth.Config;

public class AuthConfigSection
{
    public string DefaultCallbackTemplate { get; init; }
    public string DefaultRedirectTemplate { get; init; }
    public IList<ProviderConfig> Providers { get; init; }
}