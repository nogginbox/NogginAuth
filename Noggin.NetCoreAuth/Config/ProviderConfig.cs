using Noggin.NetCoreAuth.Exceptions;

namespace Noggin.NetCoreAuth.Config;

public class ProviderConfig
{
	public ApiConfig? Api { get; init; }

	public bool? CallbackIsHttps { get; init; }

	public string CallbackTemplate { get; init; }

    /// <summary>
    /// The host and port part of your callback URL.
	/// ie. "coreauth.nogginbox.co.uk"
    /// If null, the host URL and port will be autodetected.
    /// (Autodetection may not work if your site is hosted on a reverse proxy like YARP or NGINX)
    /// </summary>
    public string? CallbackUrlHost { get; init; }

    public string Name { get; init; }

	public string RedirectTemplate { get; init; }

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