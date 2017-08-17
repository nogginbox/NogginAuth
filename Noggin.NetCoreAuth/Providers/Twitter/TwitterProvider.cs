using System;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers.Twitter.Model;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Extensions.MonoHttp;
using Noggin.NetCoreAuth.Config;

namespace Noggin.NetCoreAuth.Providers.Twitter
{
    internal class TwitterProvider : Provider
    {
        private readonly string _baseUrl;
        private readonly IRestClient _restClient;

        private readonly ApiConfig _apiDetails;

        private const string DeniedKey = "denied";
        private const string OAuthTokenKey = "oauth_token";
        private const string OAuthTokenSecretKey = "oauth_token_secret";
        private const string OAuthVerifierKey = "oauth_verifier";

        internal TwitterProvider(ProviderConfig config, string defaultRedirectTemplate, string defaultCallbackTemplate) : base(config, defaultRedirectTemplate, defaultCallbackTemplate)
        {
            _baseUrl = "https://api.twitter.com";

            // Todo: If not all methods need client, perhaps don't always init it
            _restClient = new RestClient(_baseUrl);

            _apiDetails = config.Api;
        }

        internal override async Task<(string url, string secret)> GenerateStartRequestUrl(string host, bool isHttps)
        {
            // Work out callback URL
            var protocal = isHttps ? "https" : "http";
            var uri = $"{protocal}://{host}/{CallbackTemplate}";

            _restClient.Authenticator = OAuth1Authenticator.ForRequestToken(_apiDetails.PublicKey, _apiDetails.PrivateKey, uri);
            var restRequest = new RestRequest("oauth/request_token", Method.POST);

            var response = await _restClient.ExecuteAsync(restRequest);

            var query = QueryHelpers.ParseQuery(response.Content);
            var token = query["oauth_token"];

            return ($"https://api.twitter.com/oauth/authenticate?oauth_token={token}", query["oauth_token_secret"]);

            // Todo: Catch exceptions

        }

        internal override async Task<UserInformation> AuthenticateUser(IQueryCollection queryStringParameters, string state, Uri callbackUri)
        {
            // Retrieve the OAuth Verifier.
            var oAuthVerifier = RetrieveOAuthVerifier(queryStringParameters);

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
                throw new AuthenticationException(
                    "Failed to accept the Twitter App Authorization. Therefore, authentication didn't proceed.");
            }

            var oAuthToken = queryStringParameters[OAuthTokenKey];
            var oAuthVerifier = queryStringParameters[OAuthVerifierKey];


            if (string.IsNullOrEmpty(oAuthToken) || string.IsNullOrEmpty(oAuthVerifier))
            {
                throw new AuthenticationException(
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
                response = await _restClient.ExecuteAsync(restRequest);
            }
            catch (Exception exception)
            {
                var errorMessage = "Failed to retrieve an oauth access token from Twitter.";
                throw new AuthenticationException(errorMessage, exception);
            }

            if (response == null ||
                response.StatusCode != HttpStatusCode.OK)
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


                throw new AuthenticationException(errorMessage);
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
                throw new AuthenticationException(errorMessage, exception);
            }

            if (response?.StatusCode != HttpStatusCode.OK)
            {
                var errorMessage = $"The Twitter API responded was not an HTTP Status {response?.StatusCode} : {response?.StatusDescription}. Error Message: {response?.ErrorException.Message}.";
                throw new AuthenticationException(errorMessage);
            }

            if (response.Data == null)
            {
                var errorMessage = $"Could not create VerifyCredentials result from Twitter response";
                throw new AuthenticationException(errorMessage);
            }

            return response.Data;
        }
    }
}
