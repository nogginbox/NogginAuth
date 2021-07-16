using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers.GitHub.Model;
using RestSharp;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Providers.GitHub
{
    /// <summary>
    /// GitHub Provider
    /// </summary>
    /// <remarks>
    /// Reference: https://developer.github.com/v3/guides/basics-of-authentication/
    /// </remarks>
    internal class GitHubProvider : Provider
    {
        private const string _apiUrl = "https://api.github.com/";
        private const string _oauthStartUrl = "https://github.com/login/oauth";
        private readonly IRestClientFactory _restClientFactory;

        private readonly ApiConfig _apiDetails;


        internal GitHubProvider(ProviderConfig config, IRestClientFactory restClientFactory, string defaultRedirectTemplate, string defaultCallbackTemplate) : base(config, defaultRedirectTemplate, defaultCallbackTemplate)
        {
            _apiDetails = config.Api;
            _restClientFactory = restClientFactory;
        }

        internal override Task<(string url, string secret)> GenerateStartRequestUrl(HttpRequest request)
        {
            var callback = CreateCallbackUrl(request);
            var url = $"{_oauthStartUrl}/authorize?scope=user:email%20read:user&client_id={_apiDetails.PublicKey}&redirect_uri={callback}";

			// This implementation of method does not need to be async, so convert result to tasprivatek
			return Task.FromResult((url, string.Empty));
		}

        internal override async Task<UserInformation> AuthenticateUser(HttpRequest request, string state)
        {
			var code = GetCode(request.Query);
            var accessToken = await RetrieveAccessToken(code);
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
                var errorMessage = $"{queryStringParameters["error"]}: {queryStringParameters["error_description"]}";

				throw new NogginNetCoreAuthException(errorMessage);
			}

			if (!queryStringParameters.ContainsKey("code"))
			{
				throw new NogginNetCoreAuthException("No auth code returned by GitHub");
			}

			return queryStringParameters["code"].ToString();
		}

		private async Task<string> RetrieveAccessToken(string authorizationCode)
		{
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new ArgumentNullException(nameof(authorizationCode));
			}

            var restClient = _restClientFactory.Create(_oauthStartUrl);
			var restRequest = new RestRequest("access_token");
			restRequest.AddParameter("client_id", _apiDetails.PublicKey);
			restRequest.AddParameter("client_secret", _apiDetails.PrivateKey);
			restRequest.AddParameter("code", authorizationCode);
            restRequest.AddParameter("accept", "json");

            IRestResponse<AccessTokenResult> tokenResponse;

            try
			{
				tokenResponse = await restClient.ExecuteAsync<AccessTokenResult>(restRequest);
			}
			catch(Exception ex)
			{
				throw new NogginNetCoreAuthException("Failed to get access token from GitHub", ex);
			}

            if(!tokenResponse.IsSuccessful || tokenResponse?.Data.Error != null)
            {
                var errorMessage = "Failed to get access token from GitHub";
                if (tokenResponse?.Data.Error != null) errorMessage += " - " + tokenResponse.Data.ErrorDescription;
                throw new NogginNetCoreAuthException(errorMessage);
            }

            return tokenResponse.Data.AccessToken;
        }

		protected async Task<UserInformation> RetrieveUserInformationAsync(string authToken)
		{
			IRestResponse<UserResult> response;

            var restClient = _restClientFactory.Create(_apiUrl);
            var restRequest = new RestRequest("user");
			restRequest.AddParameter("access_token", authToken);

			try
			{
                response = await restClient.ExecuteAsync<UserResult>(restRequest);
			}
			catch (Exception ex)
			{
				throw
					new NogginNetCoreAuthException("Failed to retrieve user from the GitHub API.", ex);
			}

			if (response?.StatusCode != HttpStatusCode.OK || response.Data == null)
			{
                var errorMessage = "GitHub: Failed to get user information";
                if (response?.Data?.Message != null) errorMessage += " - " + response.Data.Message;

                throw new NogginNetCoreAuthException(errorMessage);
			}

			var userInformation = new UserInformation
			{
				Id = response.Data.Id.ToString(),
				Name = response.Data.Name,
				Email = response.Data.Email,
				UserName = response.Data.Login,
				Picture = response.Data.AvatarUrl
			};

			return userInformation;
		}

    }
}