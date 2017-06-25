using Noggin.NetCoreAuth.Exceptions;

namespace Noggin.NetCoreAuth.Config
{
    public class ProviderConfig
    {
        public string Name { get; set; }
        public string RedirectTemplate { get; set; }
        public string CallbackTemplate { get; set; }

        public ApiConfig Api { get; set; }

        internal void CheckIsValid()
        {
            if (Api == null)
            {
                throw new NogginNetCoreConfigException($"Provider {Name} has no Api section");
            }

            if (string.IsNullOrWhiteSpace(Api.PrivateKey) || string.IsNullOrWhiteSpace(Api.PublicKey))
            {
                throw new NogginNetCoreConfigException($"Provider {Name} needs Api.PublicKey and Api.PrivateKey");
            }
        }
    }
}