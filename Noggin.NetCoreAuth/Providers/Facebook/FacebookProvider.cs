using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers.Facebook.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Providers.Facebook;

/// <summary>
/// Facebook Login Provider
/// </summary>
/// <remarks>
/// Reference: https://developers.facebook.com/docs/facebook-login/login-flow-for-web-no-jssdk/
/// </remarks>
internal class FacebookProvider : Provider
{
	/// <summary>
	/// Graph API version used for the login dialog and all Graph API calls.
	/// See https://developers.facebook.com/docs/graph-api/changelog/ for available versions.
	/// </summary>
	internal const string GraphApiVersion = "v27.0";

	private const string _facebookUrl = $"https://www.facebook.com/{GraphApiVersion}";
	private const string _facebookGraphUrl = $"https://graph.facebook.com/{GraphApiVersion}/";

    private readonly ApiConfig _apiDetails;

    internal FacebookProvider(ProviderConfig config, string defaultRedirectTemplate, string defaultCallbackTemplate, ILogger<FacebookProvider> logger) : base(config, defaultRedirectTemplate, defaultCallbackTemplate, logger)
    {
        _apiDetails = config.Api;
    }

    private protected override string CreateAuthoriseUrl(HttpRequest request, string state)
    {
		var callback = CreateCallbackUrl(request);

		var url = _facebookUrl
			.AppendPathSegments("dialog", "oauth")
			.SetQueryParam("scope", "public_profile,email")
			.SetQueryParam("client_id", _apiDetails.PublicKey)
			.SetQueryParam("state", state)
			.SetQueryParam("redirect_uri", callback);

		return url.ToString();
	}

	private protected override async Task<string> RetrieveAccessToken(string authorizationCode, string callbackUrl)
	{
		if (string.IsNullOrEmpty(authorizationCode))
		{
			throw new ArgumentNullException(nameof(authorizationCode));
		}

		if (callbackUrl == null)
		{
			throw new ArgumentNullException(nameof(callbackUrl));
		}

		var request = _facebookGraphUrl
			.AppendPathSegments("oauth", "access_token")
			.SetQueryParams(new
			{
				client_id = _apiDetails.PublicKey,
				client_secret = _apiDetails.PrivateKey,
				code = authorizationCode,
				redirect_uri = callbackUrl
			})
			.WithHeader("User-Agent", NogginAuthUserAgentName);

        AccessTokenResult token;

        try
		{
			token = await request.GetJsonAsync<AccessTokenResult>();
		}
        catch (FlurlHttpException ex)
        {
            var error = (await ex.GetResponseJsonAsync<ErrorResult>())?.Error;
            throw new NogginNetCoreAuthException($"Failed to get access token from Facebook: {ex.Message} - {error?.Code}: {error?.Message} (fbtrace_id: {error?.FbtraceId})", ex);
        }
        catch (Exception ex)
		{
			throw new NogginNetCoreAuthException($"Failed to get access token from Facebook - {ex.Message}", ex);
		}

        return token.AccessToken;
    }

	private protected override async Task<UserInformation> RetrieveUserInformationAsync(string authToken)
	{
		MeResult me;
		
		var request = _facebookGraphUrl
			.AppendPathSegment("me")
			.SetQueryParams(new
			{
				fields = "id,name,email,first_name,last_name",
				access_token = authToken
			})
			.WithHeader("User-Agent", NogginAuthUserAgentName);

		try
		{
			me = await request.GetJsonAsync<MeResult>();
		}
        catch (FlurlHttpException ex)
        {
            var error = (await ex.GetResponseJsonAsync<ErrorResult>())?.Error;
            throw new NogginNetCoreAuthException($"Failed to get user information from Facebook: {ex.Message} - {error?.Code}: {error?.Message} (fbtrace_id: {error?.FbtraceId})", ex);
        }
        catch (Exception ex)
		{
			throw new NogginNetCoreAuthException($"Failed to retrieve any Me data from the Facebook Api - {ex.Message}", ex);
		}

		var id = me.Id;
		if (string.IsNullOrWhiteSpace(id))
		{
			throw new NogginNetCoreAuthException("Facebook Me data did not include a user id");
		}

		var userInformation = new UserInformation
		{
			Id = id,
			Name = me.Name,
			Email = me.Email,
			Picture = string.Format("https://graph.facebook.com/{0}/picture", id)
		};

		return userInformation;
	}
}