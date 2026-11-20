using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers.Google.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Providers.Google;

/// <summary>
/// Google Login Provider
/// </summary>
/// <remarks> Reference: https://developers.google.com/accounts/docs/OAuth2Login</remarks>
internal class GoogleProvider : Provider
{
	private readonly ApiConfig _apiDetails;

	private const string _googleOAuthUrl = "https://accounts.google.com/o/oauth2/";
	private const string _googleApi = "https://www.googleapis.com";

    internal GoogleProvider(ProviderConfig config, string defaultRedirectTemplate, string defaultCallbackTemplate, ILogger<GoogleProvider> logger) : base(config, defaultRedirectTemplate, defaultCallbackTemplate, logger)
	{
		_apiDetails = config.Api;
    }

	private protected override string CreateAuthoriseUrl(HttpRequest request, string state)
	{
		var callback = CreateCallbackUrl(request);

        var url = _googleOAuthUrl
			.AppendPathSegment("auth")
            .SetQueryParam("scope", "profile email")
			.SetQueryParam("state", state)
            .SetQueryParam("response_type", "code")
            .SetQueryParam("client_id", _apiDetails.PublicKey)
			.SetQueryParam("redirect_uri", callback);

        return url.ToString();
	}

	private protected override async Task<string> RetrieveAccessToken(string authorizationCode, string callbackUrl)
	{
		var url = _googleOAuthUrl
			.AppendPathSegment("token")
            .WithHeader("User-Agent", NogginAuthUserAgentName);

		var form = new
		{
			client_id = _apiDetails.PublicKey,
			client_secret = _apiDetails.PrivateKey,
			redirect_uri = callbackUrl,
			code = authorizationCode,
			grant_type = "authorization_code"
		};

		try
		{
			var tokenResponse = await url.PostUrlEncodedAsync(form);
            var token = await tokenResponse.GetJsonAsync<AccessTokenResult>();

            if (token.AccessToken == null)
			{
				throw new NullReferenceException("token.AccessToken");
			}
			return token.AccessToken;
		}
        catch (FlurlHttpException ex)
        {
            var error = await ex.GetResponseJsonAsync<GoogleError>();
            throw new NogginNetCoreAuthException($"Error {ex.StatusCode} - Failed to get access token from Google: {ex.Message} - {error?.Error}: {error?.Description}", ex);
        }
        catch (Exception ex)
		{
			throw new NogginNetCoreAuthException("Failed to get access token from Google", ex);
		}
	}

	private protected override async Task<UserInformation> RetrieveUserInformationAsync(string accessToken)
	{
		if (accessToken == null)
		{
			throw new ArgumentNullException(nameof(accessToken));
		}

		UserInfoResult user;

		try
		{
            var url = _googleApi
				.AppendPathSegments("oauth2", "v3", "userinfo")
                .WithHeader("User-Agent", NogginAuthUserAgentName);

			var form = new
			{
				access_token = accessToken
			};

			var response = await url.PostUrlEncodedAsync(form);
			user = await response.GetJsonAsync<UserInfoResult>();
		}
        catch (FlurlHttpException ex)
        {
            var error = await ex.GetResponseJsonAsync<GoogleError>();
            throw new NogginNetCoreAuthException($"Error {ex.StatusCode} - Failed to get user info from Google: {ex.Message} - {error?.Error}: {error?.Description}", ex);
        }
        catch (Exception ex)
		{
			throw new NogginNetCoreAuthException($"Failed to get UserInfo from Google - {ex.Message}", ex);
		}

		if (user == null)
		{
			throw new NogginNetCoreAuthException("Null response from Google");
		}

		// Lets check to make sure we have some bare minimum data.
		if (string.IsNullOrEmpty(user.Sub))
		{
			throw new NogginNetCoreAuthException("Could not get User Id from Google, the user may have denied the authorization.");
		}

		return new UserInformation
		{
			Id = user.Sub,
			Name = user.Name,
			Email = user.Email,
			Picture = user.Picture,
			UserName = user.Email
		};
	}
}