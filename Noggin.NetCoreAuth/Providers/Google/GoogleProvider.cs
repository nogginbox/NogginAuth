using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers.Google.Model;
using RestSharp;

namespace Noggin.NetCoreAuth.Providers.Google
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks> Reference: https://developers.google.com/accounts/docs/OAuth2Login</remarks>
	internal class GoogleProvider : Provider
	{
		private ApiConfig _apiDetails;
        private readonly IRestClientFactory _restClientFactory;

        internal GoogleProvider(ProviderConfig config, IRestClientFactory restClientFactory, string defaultRedirectTemplate, string defaultCallbackTemplate) : base(config, defaultRedirectTemplate, defaultCallbackTemplate)
		{
			_apiDetails = config.Api;
            _restClientFactory = restClientFactory;
        }

		internal async override Task<UserInformation> AuthenticateUser(HttpRequest request, string state)
		{
			var details = GetDetails(request.Query);
			//var code = GetCode(request.Query);
			var callback = CreateCallbackUrl(request);
			var accessToken = await RetrieveAccessToken(details.code, callback);
			var userInfo = await RetrieveUserInformationAsync(accessToken);

			return userInfo;
		}

		internal override Task<(string url, string secret)> GenerateStartRequestUrl(HttpRequest request)
		{
			var callback = CreateCallbackUrl(request);
			var secret = Guid.NewGuid().ToString();

			// Todo: Find/make function to turn anonymous object into querystring
			/* Example: "https://accounts.google.com/o/oauth2/auth?
			 * client_id=587140099194.apps.googleusercontent.com&
			 * redirect_uri=http://localhost:1337/authentication/authenticatecallback?providerkey=google
			 * &response_type=code&
			 * scope=profile email&
			 * state=7f7dbb76-412e-47a2-b6b3-f596565d089f" */
			var url = $"https://accounts.google.com/o/oauth2/auth?client_id={_apiDetails.PublicKey}&scope=profile email&state={secret}&redirect_uri={callback}&response_type=code";

			// This implementation of method does not need to be async, so convert result to task
			return Task.FromResult((url, secret));
		}

		private static (string code, string state) GetDetails(IQueryCollection queryStringParameters)
		{
			if (queryStringParameters == null || !queryStringParameters.Any())
			{
				throw new ArgumentOutOfRangeException("queryStringParameters");
			}

			// Maybe we have an error?
			if (queryStringParameters.ContainsKey("error"))
			{
				var errorMessage = string.Format("Reason: {0}. Error: {1}. Description: {2}.",
												 queryStringParameters["error_reason"].ToString() ?? "-",
												 queryStringParameters["error"],
												 queryStringParameters["error_description"]);

				throw new NogginNetCoreAuthException(errorMessage);
			}

			if(!queryStringParameters.ContainsKey("state"))
			{
				throw new NogginNetCoreAuthException("No state returned by Facebook");
			}

			if (!queryStringParameters.ContainsKey("code"))
			{
				throw new NogginNetCoreAuthException("No auth code returned by Facebook");
			}

			return 
				(queryStringParameters["code"].ToString(),
				queryStringParameters["state"].ToString());
		}

		private async Task<string> RetrieveAccessToken(string authorizationCode, string callbackUrl)
		{
			var restRequest = new RestRequest("/o/oauth2/token", Method.POST);
			restRequest.AddParameter("client_id", _apiDetails.PublicKey);
			restRequest.AddParameter("client_secret", _apiDetails.PrivateKey);
			restRequest.AddParameter("redirect_uri", callbackUrl);
			restRequest.AddParameter("code", authorizationCode);
			restRequest.AddParameter("grant_type", "authorization_code");

			var restClient = _restClientFactory.Create("https://accounts.google.com");

			try
			{
				var token = await restClient.ExecuteTaskAsync<AccessTokenResult>(restRequest);

				if (token.Data.AccessToken == null)
				{
					throw new NullReferenceException("(token.Data.AccessToken");
				}
				return token.Data.AccessToken;
			}
			catch (Exception ex)
			{
				throw new NogginNetCoreAuthException("Failed to get access token from Facebook", ex);
			}
		}

		private async Task<UserInformation> RetrieveUserInformationAsync(string accessToken)
		{
			if (accessToken == null)
			{
				throw new ArgumentNullException("accessToken");
			}

			IRestResponse<UserInfoResult> response;

			try
			{
				var restRequest = new RestRequest("/plus/v1/people/me", Method.GET);
				restRequest.AddParameter("access_token", accessToken);

				var restClient = _restClientFactory.Create("https://www.googleapis.com");


				response = await restClient.ExecuteTaskAsync<UserInfoResult>(restRequest);
			}
			catch (Exception ex)
			{
				throw new NogginNetCoreAuthException("Failed to retrieve any UserInfo data from the Google API", ex);
			}

			if (response == null ||
				response.StatusCode != HttpStatusCode.OK)
			{
				var errorMessage = string.Format(
					"Failed to obtain some UserInfo data from the Google Api OR the the response was not an HTTP Status 200 OK. Response Status: {0}. Response Description: {1}. Error Message: {2}.",
					response == null ? "-- null response --" : response.StatusCode.ToString(),
					response == null ? string.Empty : response.StatusDescription,
					response == null
						? string.Empty
						: response.ErrorException == null
							  ? "--no error exception--"
							  : response.ErrorException.Message);

				throw new NogginNetCoreAuthException(errorMessage);
			}

			// Lets check to make sure we have some bare minimum data.
			if (string.IsNullOrEmpty(response.Data.Id))
			{
				const string errorMessage =
					"We were unable to retrieve the User Id from Google API, the user may have denied the authorization.";
				throw new NogginNetCoreAuthException(errorMessage);
			}

			return new UserInformation
			{
				Id = response.Data.Id,
				Gender = response.Data.Gender,
				Name = response.Data.Name.ToString(),
				Email = response.Data.Emails != null &&
						response.Data.Emails.Any()
					? response.Data.Emails.First().Value
					: null,
				Locale = response.Data.Language,
				Picture = response.Data.Image?.Url,
				UserName = response.Data.DisplayName
			};
		}
	}
}