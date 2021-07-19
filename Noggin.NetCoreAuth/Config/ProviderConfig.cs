using Noggin.NetCoreAuth.Exceptions;

namespace Noggin.NetCoreAuth.Config
{
	public class ProviderConfig
	{
		public string Name { get; init; }

		public string RedirectTemplate { get; init; }

		public bool? CallbackIsHttps { get; init; }

		public string CallbackTemplate { get; init; }

		public ApiConfig Api { get; init; }

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