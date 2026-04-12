using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Model;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Providers;

public abstract class Provider
{
	protected const string NogginAuthUserAgentName = "NogginAuth";

    protected Provider(ProviderConfig config, string defaultRedirectTemplate, string defaultCallbackTemplate)
    {
        config.CheckIsValid();
        Name = config.Name;

		CallbackTemplate = CreateTemplate(Name, config.CallbackTemplate, defaultCallbackTemplate);
        RedirectTemplate = CreateTemplate(Name, config.RedirectTemplate, defaultRedirectTemplate);

		CallbacksUseHttps = config.CallbackIsHttps;
        CallbackUrlHost = config.CallbackUrlHost;
    }

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

    internal abstract Task<UserInformation> AuthenticateUser(HttpRequest request, string state);
    internal abstract Task<(string url, string secret)> GenerateStartRequestUrl(HttpRequest request);

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