using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers.Facebook.Model;
using RestSharp;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Providers.Facebook
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// Reference: https://developers.facebook.com/docs/facebook-login/login-flow-for-web-no-jssdk/
	/// </remarks>
	internal class FacebookProvider : Provider
    {
        private const string _baseUrl = "https://graph.facebook.com/v2.12/";
        private readonly IRestClient _restClient;

        private readonly ApiConfig _apiDetails;

        private const string DeniedKey = "denied";
        private const string OAuthTokenKey = "oauth_token";
        private const string OAuthTokenSecretKey = "oauth_token_secret";
        private const string OAuthVerifierKey = "oauth_verifier";

        internal FacebookProvider(ProviderConfig config, IRestClientFactory restClientFactory, string defaultRedirectTemplate, string defaultCallbackTemplate) : base(config, defaultRedirectTemplate, defaultCallbackTemplate)
        {
            // Todo: If not all methods need client, perhaps don't always init it
            _restClient = restClientFactory.Create(_baseUrl);

            _apiDetails = config.Api;
        }

        internal override Task<(string url, string secret)> GenerateStartRequestUrl(HttpRequest request)
        {
			var callback = CreateCallbackUrl(request);
            var secret = Guid.NewGuid().ToString();

			var url = $"https://www.facebook.com/v2.12/dialog/oauth?client_id={_apiDetails.PublicKey}&scope=public_profile,email&state={secret}&redirect_uri={callback}";

			// This implementation of method does not need to be async, so convert result to task
			return Task.FromResult((url, secret));
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
				throw new ArgumentOutOfRangeException("queryStringParameters");
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
				throw new ArgumentNullException("authorizationCode");
			}

			/*if (redirectUri == null ||
				string.IsNullOrEmpty(redirectUri.AbsoluteUri))
			{
				throw new ArgumentNullException("redirectUri");
			}*/

			var restRequest = new RestRequest("oauth/access_token");
			restRequest.AddParameter("client_id", _apiDetails.PublicKey);
			restRequest.AddParameter("client_secret", _apiDetails.PrivateKey);
			restRequest.AddParameter("code", authorizationCode);

			// Send same callbackUri as a security measure to check we're the same person that made original request
			restRequest.AddParameter("redirect_uri", callbackUrl);
			restRequest.AddHeader("Content-Type", "application/json");
			restRequest.AddParameter("format", "json");

            IRestResponse<AccessTokenResult> tokenResponse;

            try
			{
				tokenResponse = await _restClient.ExecuteAsync<AccessTokenResult>(restRequest);
			}
			catch(Exception ex)
			{
				throw new NogginNetCoreAuthException("Failed to get access token from Facebook", ex);
			}

            if(!tokenResponse.IsSuccessful)
            {
                var errorMessage = tokenResponse.Data.Error?.Message ?? "Failed to get access token from Facebook";
                throw new NogginNetCoreAuthException(errorMessage);
            }

            return tokenResponse.Data.AccessToken;
        }

		protected async Task<UserInformation> RetrieveUserInformationAsync(string authToken)
		{
			IRestResponse<MeResult> response;

			try
			{
				var restRequest = new RestRequest("me");
				restRequest.AddParameter("access_token", authToken);
				restRequest.AddParameter("fields", "name,email,first_name,last_name,locale,gender,link");


				response = await _restClient.ExecuteAsync<MeResult>(restRequest);
			}
			catch (Exception ex)
			{
				throw
					new NogginNetCoreAuthException("Failed to retrieve any Me data from the Facebook Api.", ex);
			}

			if (response?.StatusCode != HttpStatusCode.OK || response.Data == null)
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
			}

			var id = response.Data.Id < 0 ? 0 : response.Data.Id;
			var name = (string.IsNullOrEmpty(response.Data.FirstName)
							? string.Empty
							: response.Data.FirstName) + " " +
					   (string.IsNullOrEmpty(response.Data.LastName)
							? string.Empty
							: response.Data.LastName).Trim();

			var userInformation = new UserInformation
			{
				Id = id.ToString(),
				Name = name,
				Email = response.Data.Email,
				Locale = response.Data.Locale,
				UserName = response.Data.Username,
				Gender = response.Data.Gender,
				Picture = string.Format("https://graph.facebook.com/{0}/picture", id)
			};

			return userInformation;
		}
	}
}