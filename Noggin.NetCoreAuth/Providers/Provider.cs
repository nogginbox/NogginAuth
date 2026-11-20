using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Model;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Providers;

public abstract class Provider
{
	protected const string NogginAuthUserAgentName = "NogginAuth";

    protected Provider(ProviderConfig config, string defaultRedirectTemplate, string defaultCallbackTemplate, ILogger logger)
    {
        config.CheckIsValid();
        Logger = logger;
        Name = config.Name;

		CallbackTemplate = CreateTemplate(Name, config.CallbackTemplate, defaultCallbackTemplate);
        RedirectTemplate = CreateTemplate(Name, config.RedirectTemplate, defaultRedirectTemplate);

		CallbacksUseHttps = config.CallbackIsHttps;
        CallbackUrlHost = config.CallbackUrlHost;
    }

    protected ILogger Logger { get; }

    public string Name { get; }
	public string CallbackTemplate { get; }
		
	/// <summary>
	/// For callbacks use Https.
	/// [null: use settings from request, false: always use HTTP, true: always use HTTPS]
	/// </summary>
	public bool? CallbacksUseHttps { get; }

    /// <summary>
    /// The host and port part of your callback URL.
    /// ie. "coreauth.nogginbox.co.uk"
    /// If null, the host URL and port will be autodetected.
    /// (Autodetection may not work if your site is hosted on a reverse proxy like YARP or NGINX)
    /// </summary>
    public string? CallbackUrlHost { get; }

    public string RedirectTemplate { get; }

    /// <summary>
    /// Exchange the code in the callback request for the user's details.
    /// The caller must have already validated the OAuth state (see <see cref="OAuthState"/>).
    /// </summary>
    internal abstract Task<UserInformation> AuthenticateUser(HttpRequest request);

    /// <summary>
    /// Create the provider URL to send the user to, including the given OAuth state.
    /// </summary>
    internal abstract Task<string> GenerateStartRequestUrl(HttpRequest request, string state);

    private static string CreateTemplate(string providerName, string template1, string templateTemplate)
    {
        if (!string.IsNullOrEmpty(template1)) return template1;

        return templateTemplate.Replace("{provider}", providerName.ToLower());
    }

	protected string CreateCallbackUrl(HttpRequest request)
	{
        bool useHttps = CallbacksUseHttps ?? request.IsHttps;
        string host = string.IsNullOrEmpty(CallbackUrlHost)
            ? request.Host.Value
            : CallbackUrlHost;

		return CreateUrl(useHttps, host, CallbackTemplate);
	}

	private static string CreateUrl(bool isHttps, string host, string template)
	{
		var protocal = isHttps ? "https" : "http";
		var url = $"{protocal}://{host}/{template}";
		return url;
	}
}