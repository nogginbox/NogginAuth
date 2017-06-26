using Noggin.NetCoreAuth.Config;

namespace Noggin.NetCoreAuth.Providers
{
    public abstract class Provider
    {
        protected Provider(ProviderConfig config)
        {
            config.CheckIsValid();
        }
    }
}