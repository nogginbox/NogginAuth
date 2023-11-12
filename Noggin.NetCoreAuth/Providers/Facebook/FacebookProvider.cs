using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
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
	private const string _facebookUrl = "https://www.facebook.com/v2.12";
    private const string _facebookGraphUrl = "https://graph.facebook.com/v2.12/";

    private readonly ApiConfig _apiDetails;

    internal FacebookProvider(ProviderConfig config, string defaultRedirectTemplate, string defaultCallbackTemplate) : base(config, defaultRedirectTemplate, defaultCallbackTemplate)
    {
        _apiDetails = config.Api;
    }

    internal override Task<(string url, string secret)> GenerateStartRequestUrl(HttpRequest request)
    {
		var callback = CreateCallbackUrl(request);
        var secret = Guid.NewGuid().ToString();

		var url = _facebookUrl
			.AppendPathSegments("dialog", "oauth")
			.SetQueryParam("scope", "public_profile,email")
			.SetQueryParam("client_id", _apiDetails.PublicKey)
			.SetQueryParam("state", secret)
			.SetQueryParam("redirect_uri", callback);

		// This implementation of method does not need to be async, so convert result to task
		return Task.FromResult((url.ToString(), secret));
	}

    internal override async Task<UserInformation> AuthenticateUser(HttpRequest request, string state)
    {
		var code = GetCode(request.Query);
		var callback = CreateCallbackUrl(request);
        var accessToken = await RetrieveAccessToken(code, callback);
        var userInfo = await RetrieveUserInformationAsync(accessToken);

        return userInfo;
    }

    private static string GetCode(IQueryCollection queryStringParameters)
    {
		if (queryStringParameters == null || !queryStringParameters.Any())
		{
			throw new ArgumentOutOfRangeException(nameof(queryStringParameters));
		}

		// Maybe we have an error?
		if(queryStringParameters.ContainsKey("error"))
		{
			var errorMessage = string.Format("Reason: {0}. Error: {1}. Description: {2}.",
												queryStringParameters["error_reason"].ToString() ?? "-",
												queryStringParameters["error"],
												queryStringParameters["error_description"]);

			throw new NogginNetCoreAuthException(errorMessage);
		}

		if (!queryStringParameters.ContainsKey("code"))
		{
			throw new NogginNetCoreAuthException("No auth code returned by Facebook");
		}

		return queryStringParameters["code"].ToString();
	}

	private async Task<string> RetrieveAccessToken(string authorizationCode, string callbackUrl)
	{
		if (string.IsNullOrEmpty(authorizationCode))
		{
			throw new ArgumentNullException(nameof(authorizationCode));
		}

		/*if (redirectUri == null ||
			string.IsNullOrEmpty(redirectUri.AbsoluteUri))
		{
			throw new ArgumentNullException("redirectUri");
		}*/


		var url = _facebookGraphUrl
			.AppendPathSegments("oauth", "access_token")
			.WithHeader("Content-Type", "application/json");

		var form = new
		{
            client_id = _apiDetails.PublicKey,
            client_secret = _apiDetails.PrivateKey,
            code = authorizationCode,
			redirect_uri = callbackUrl,
			format = "json"
        };

        AccessTokenResult token;

        try
		{
			var tokenResponse = await url.PostJsonAsync(form);
			token = await tokenResponse.GetJsonAsync<AccessTokenResult>();
		}
		catch(Exception ex)
		{
			throw new NogginNetCoreAuthException($"Failed to get access token from Facebook - {ex.Message}", ex);
		}

        /* Flurl always throws on non 200, move this into catch if possible
        if(!tokenResponse.IsSuccessful)
        {
            var errorMessage = "Facebook: Failed to get access token";
            if (tokenResponse.Data?.Error?.Message != null) errorMessage += " - " + tokenResponse.Data.Error.Message;

            throw new NogginNetCoreAuthException(errorMessage);
        }*/

        return token.AccessToken;
    }

	protected static async Task<UserInformation> RetrieveUserInformationAsync(string authToken)
	{
		MeResult me;
		
		var url = _facebookGraphUrl
			.AppendPathSegment("me");

		var form = new
		{
            access_token = authToken,
            fields = "id,name,email,first_name,last_name",
        };

		try
		{
			var response = await url.PostJsonAsync(form);
			var content = await response.GetStringAsync();
			me = await response.GetJsonAsync<MeResult>();
		}
		catch (Exception ex)
		{
			throw new NogginNetCoreAuthException($"Failed to retrieve any Me data from the Facebook Api - {ex.Message}", ex);
		}

		/*if (response?.StatusCode != HttpStatusCode.OK || response.Data == null)
		{
			var errorMessage = string.Format(
				"Failed to obtain some 'Me' data from the Facebook api OR the the response was not an HTTP Status 200 OK. Response Status: {0}. Response Description: {1}. Error Message: {2}.",
				response == null ? "-- null response --" : response.StatusCode.ToString(),
				response == null ? string.Empty : response.StatusDescription,
				response == null
					? string.Empty
					: response.ErrorException == null
							? "--no error exception--"
							: response.ErrorException.Message);

			throw new NogginNetCoreAuthException(errorMessage);
		}*/

		var id = me.Id < 0 ? 0 : me.Id;

		var userInformation = new UserInformation
		{
			Id = id.ToString(),
			Name = me.Name,
			Email = me.Email,
			Picture = string.Format("https://graph.facebook.com/{0}/picture", id)
		};

		return userInformation;
	}
}