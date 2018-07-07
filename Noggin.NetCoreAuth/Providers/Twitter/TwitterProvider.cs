using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers.Serializers;
using Noggin.NetCoreAuth.Providers.Twitter.Model;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Web;

namespace Noggin.NetCoreAuth.Providers.Twitter
{
    internal class TwitterProvider : Provider
    {
        private readonly string _baseUrl;
        private readonly IRestClient _restClient;

        private readonly ApiConfig _apiDetails;

        internal const string DeniedKey = "denied";
        internal const string OAuthTokenKey = "oauth_token";
        internal const string OAuthTokenSecretKey = "oauth_token_secret";
        internal const string OAuthVerifierKey = "oauth_verifier";

        internal TwitterProvider(ProviderConfig config, IRestClientFactory restClientFactory, string defaultRedirectTemplate, string defaultCallbackTemplate) : base(config, defaultRedirectTemplate, defaultCallbackTemplate)
        {
			_baseUrl = "https://api.twitter.com";

			// Todo: If not all methods need client, perhaps don't always init it
			_restClient = restClientFactory.Create(_baseUrl);
            _restClient.AddHandler("text/html", TwitterHtmlTextSerializer.CreateDefault());

            _apiDetails = config.Api;
        }

        internal override async Task<(string url, string secret)> GenerateStartRequestUrl(HttpRequest request)
        {
			var callback = CreateCallbackUrl(request);

            _restClient.Authenticator = OAuth1Authenticator.ForRequestToken(_apiDetails.PublicKey, _apiDetails.PrivateKey, callback);
            var restRequest = new RestRequest("oauth/request_token", Method.POST);

            var response = await _restClient.ExecuteTaskAsync<TokenResult>(restRequest);

            // Grrrr, errors come back as json, correct response querystring like thing


            if(response?.IsSuccessful != true)
            {
                var error = response.Data?.Errors.FirstOrDefault();
                var errorMessage = error != null
                    ? $"Twitter Error {error.Code}: {error.Message}"
                    : "There was an issue starting a login request with Twitter";
                throw new NogginNetCoreAuthException(errorMessage);
            }

            if (response.Data.OauthToken == null || response.Data.OauthTokenSecret == null)
            {
                throw new NogginNetCoreAuthException("Missing token or secret from Twitter");
            }

            return ($"https://api.twitter.com/oauth/authenticate?oauth_token={response.Data.OauthToken}", response.Data.OauthTokenSecret);
        }

        internal override async Task<UserInformation> AuthenticateUser(HttpRequest request, string state)
        {
            // Retrieve the OAuth Verifier.
            var oAuthVerifier = RetrieveOAuthVerifier(request.Query);

            // Convert the Request Token to an Access Token, now that we have a verifier.
            var oAuthAccessToken = await RetrieveAccessToken(oAuthVerifier);

            // Grab the user information.
            var verifyCredentialsResult = await VerifyCredentials(oAuthAccessToken);

            return new UserInformation
            {
                Name = verifyCredentialsResult.Name,
                Id = verifyCredentialsResult.Id.ToString(),
                Locale = verifyCredentialsResult.Lang,
                UserName = verifyCredentialsResult.ScreenName,
                Picture = verifyCredentialsResult.ProfileImageUrl,
                AccessToken = oAuthAccessToken
            };
        }

        private static (string oAuthToken, string oAuthVerifier) RetrieveOAuthVerifier(IQueryCollection queryStringParameters)
        {
            var denied = queryStringParameters[DeniedKey];
            if (!string.IsNullOrEmpty(denied))
            {
                throw new NogginNetCoreAuthException(
                    "Failed to accept the Twitter App Authorization. Therefore, authentication didn't proceed.");
            }

            var oAuthToken = queryStringParameters[OAuthTokenKey];
            var oAuthVerifier = queryStringParameters[OAuthVerifierKey];


            if (string.IsNullOrEmpty(oAuthToken) || string.IsNullOrEmpty(oAuthVerifier))
            {
                throw new NogginNetCoreAuthException(
                    "Failed to retrieve an oauth_token and an oauth_token_secret after the client has signed and approved via Twitter.");
            }

            return (oAuthToken, oAuthVerifier);
        }

        private async Task<AccessToken> RetrieveAccessToken((string oAuthToken, string oAuthVerifier) verifierResult)
        {
            IRestResponse response;
            try
            {
                var restRequest = new RestRequest("oauth/access_token", Method.POST);
                _restClient.Authenticator = OAuth1Authenticator.ForAccessToken(_apiDetails.PublicKey, _apiDetails.PrivateKey,
                                                                              verifierResult.oAuthToken,
                                                                              null,
                                                                              verifierResult.oAuthVerifier);
                response = await _restClient.ExecuteTaskAsync(restRequest);
            }
            catch (Exception exception)
            {
                var errorMessage = "Failed to retrieve an oauth access token from Twitter.";
                throw new NogginNetCoreAuthException(errorMessage, exception);
            }

            if (response?.IsSuccessful != true || response.StatusCode != HttpStatusCode.OK)
            {
                var errorMessage = string.Format(
                    "Failed to obtain an Access Token from Twitter OR the the response was not an HTTP Status 200 OK. Response Status: {0}. Response Description: {1}. Error Content: {2}. Error Message: {3}.",
                    response == null ? "-- null response --" : response.StatusCode.ToString(),
                    response == null ? string.Empty : response.StatusDescription,
                    response == null ? string.Empty : response.Content,
                    response == null
                        ? string.Empty
                        : response.ErrorException == null
                              ? "--no error exception--"
                              : response.ErrorException.Message);


                throw new NogginNetCoreAuthException(errorMessage);
            }

            var querystringParameters = HttpUtility.ParseQueryString(response.Content);



            return new AccessToken(querystringParameters[OAuthTokenKey], querystringParameters[OAuthTokenSecretKey]);
        }

        private async Task<VerifyCredentialsResult> VerifyCredentials(AccessToken accessTokenResult)
        {
            IRestResponse<VerifyCredentialsResult> response;
            try
            {
                _restClient.Authenticator = OAuth1Authenticator.ForProtectedResource(_apiDetails.PublicKey, _apiDetails.PrivateKey,
                                                                                    accessTokenResult.PublicToken,
                                                                                    accessTokenResult.SecretToken);
                var restRequest = new RestRequest("1.1/account/verify_credentials.json");

                response = await _restClient.ExecuteAsync<VerifyCredentialsResult>(restRequest);
            }
            catch (Exception exception)
            {
                var errorMessage = "Failed to retrieve VerifyCredentials json data from the Twitter Api. Error Messages: " + exception.Message;
                throw new NogginNetCoreAuthException(errorMessage, exception);
            }

            if (response?.StatusCode != HttpStatusCode.OK)
            {
                var errorMessage = $"The Twitter API responded was not an HTTP Status {response?.StatusCode} : {response?.StatusDescription}. Error Message: {response?.ErrorException.Message}.";
                throw new NogginNetCoreAuthException(errorMessage);
            }

            if (response.Data == null)
            {
                var errorMessage = $"Could not create VerifyCredentials result from Twitter response";
                throw new NogginNetCoreAuthException(errorMessage);
            }

            return response.Data;
        }
    }
}
