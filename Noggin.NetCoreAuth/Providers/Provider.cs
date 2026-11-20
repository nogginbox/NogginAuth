using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Model;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
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
    /// Complete a login when the provider calls back: check the OAuth state, exchange the code for an access token,
    /// then use the token to get the user's details from the provider.
    /// </summary>
    /// <param name="state">The secret returned by <see cref="GenerateStartRequestUrl"/> for this browser, or null if there isn't one.</param>
    /// <exception cref="NogginNetCoreAuthException">
    /// The state is missing or doesn't match (so this browser didn't start the login), the provider returned an error, or a call to the provider failed.
    /// </exception>
    internal async Task<UserInformation> AuthenticateUser(HttpRequest request, string? state)
    {
        // Check the state before using the code, so a callback this browser didn't start is rejected (prevents login CSRF)
        ValidateState(state, request.Query["state"].ToString());

        var code = GetCode(request.Query);
        var callbackUrl = CreateCallbackUrl(request);
        var accessToken = await RetrieveAccessToken(code, callbackUrl);
        return await RetrieveUserInformationAsync(accessToken);
    }

    /// <summary>
    /// Create the provider's authorise URL. It must include the given state as the "state" query parameter.
    /// </summary>
    /// <remarks>Only overridable by providers in this library, and only called by <see cref="GenerateStartRequestUrl"/>.</remarks>
    private protected abstract string CreateAuthoriseUrl(HttpRequest request, string state);

    /// <summary>
    /// Create an unguessable state value (256 random bits, base64url encoded so it's URL safe)
    /// </summary>
    private static string CreateState() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Start a login: create a new OAuth state (the secret) and the provider URL to send the user to.
    /// </summary>
    /// <returns>
    /// The URL to redirect to, and the secret. The caller must store the secret (e.g. in session)
    /// and pass it to <see cref="AuthenticateUser"/> when the provider calls back.
    /// </returns>
    internal Task<(string url, string secret)> GenerateStartRequestUrl(HttpRequest request)
    {
        var secret = CreateState();
        var url = CreateAuthoriseUrl(request, secret);
        return Task.FromResult((url, secret));
    }

    /// <summary>
    /// Get the auth code from the callback's query string
    /// </summary>
    /// <exception cref="NogginNetCoreAuthException">The provider returned an error (e.g. the user denied access) or no code.</exception>
    private string GetCode(IQueryCollection query)
    {
        if (query.ContainsKey("error"))
        {
            var reason = query.ContainsKey("error_reason") ? $" ({query["error_reason"]})" : "";
            throw new NogginNetCoreAuthException($"{Name}: {query["error"]}{reason} - {query["error_description"]}");
        }

        var code = query["code"].ToString();
        if (string.IsNullOrEmpty(code))
        {
            throw new NogginNetCoreAuthException($"No auth code returned by {Name}");
        }

        return code;
    }

    /// <summary>
    /// Exchange the auth code for an access token using the provider's token endpoint
    /// </summary>
    /// <param name="callbackUrl">The callback URL used to start the login. Some providers require it to match.</param>
    /// <remarks>Only overridable by providers in this library, and only called by <see cref="AuthenticateUser"/> after the state has been checked.</remarks>
    private protected abstract Task<string> RetrieveAccessToken(string authorizationCode, string callbackUrl);

    /// <summary>
    /// Get the user's details from the provider's API using the access token
    /// </summary>
    /// <remarks>Only overridable by providers in this library, and only called by <see cref="AuthenticateUser"/>.</remarks>
    private protected abstract Task<UserInformation> RetrieveUserInformationAsync(string accessToken);

    private static void ValidateState(string? expectedState, string? returnedState)
    {
        if (string.IsNullOrEmpty(expectedState))
        {
            throw new NogginNetCoreAuthException("No login was started in this session. The session may have expired, or the login was started in a different browser.");
        }

        if (string.IsNullOrEmpty(returnedState))
        {
            throw new NogginNetCoreAuthException("No state was returned by the login provider.");
        }

        var isMatch = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedState),
            Encoding.UTF8.GetBytes(returnedState));

        if (!isMatch)
        {
            throw new NogginNetCoreAuthException("The state returned by the login provider does not match the one sent. The login may not have been started by this browser.");
        }
    }

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