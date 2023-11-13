using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers.Microsoft.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Providers.Microsoft
{
    /// <summary>
    /// Microsoft Login Provider
    /// </summary>
    /// /// <remarks>
    /// OAuth Reference: https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow
    /// Graph Explorer: https://developer.microsoft.com/en-us/graph/graph-explorer
    /// </remarks>
    internal class MicrosoftProvider : Provider
    {
        private const string _msGraphApiUrl = "https://graph.microsoft.com/v1.0/";
        private const string _msOAuthUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/";
        private readonly ApiConfig _apiDetails;

        public MicrosoftProvider(ProviderConfig config, string defaultRedirectTemplate, string defaultCallbackTemplate) : base(config, defaultRedirectTemplate, defaultCallbackTemplate)
        {
            _apiDetails = config.Api;
        }

        internal async override Task<UserInformation> AuthenticateUser(HttpRequest request, string state)
        {
            var code = GetCode(request.Query);
            var callback = CreateCallbackUrl(request);
            var accessToken = await RetrieveAccessToken(code, callback);
            var userInfo = await RetrieveUserInformationAsync(accessToken);

            return userInfo;
        }

        internal override Task<(string url, string secret)> GenerateStartRequestUrl(HttpRequest request)
        {
            var callback = CreateCallbackUrl(request);
            var url = _msOAuthUrl
                .AppendPathSegment("authorize")
                .SetQueryParams(new
                {
                    client_id = _apiDetails.PublicKey,
                    response_type = "code",
                    redirect_uri = callback,
                    response_mode = "query",
                    scope = "openid https://graph.microsoft.com/user.read"
                });

            return Task.FromResult((url.ToString(), string.Empty));
        }
        private static string GetCode(IQueryCollection queryStringParameters)
        {
            if (queryStringParameters == null || !queryStringParameters.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(queryStringParameters));
            }

            // Maybe we have an error?
            if (queryStringParameters.ContainsKey("error"))
            {
                var errorMessage = $"{queryStringParameters["error"]}: {queryStringParameters["error_description"]}";

                throw new NogginNetCoreAuthException(errorMessage);
            }

            if (!queryStringParameters.ContainsKey("code"))
            {
                throw new NogginNetCoreAuthException("No auth code returned by Microsoft");
            }

            return queryStringParameters["code"].ToString();
        }

        private async Task<string> RetrieveAccessToken(string authorizationCode, string callback)
        {
            if (string.IsNullOrEmpty(authorizationCode))
            {
                throw new ArgumentNullException(nameof(authorizationCode));
            }

            var request = _msOAuthUrl
                .AppendPathSegment("token")
                .WithHeader("User-Agent", NogginAuthUserAgentName)
                .WithHeader("accept", "application/json");

            var form = new
            {
                client_id = _apiDetails.PublicKey,
                client_secret = _apiDetails.PrivateKey,
                code = authorizationCode,
                grant_type = "authorization_code",
                redirect_uri = callback,
            };

            try
            {
                var tokenResponse = await request.PostUrlEncodedAsync(form);
                var data = await tokenResponse.GetJsonAsync<AccessTokenResult>();
                return data?.AccessToken ?? throw new Exception("No token returned from Microsoft");
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseJsonAsync<ErrorResult>();
                throw new NogginNetCoreAuthException($"Failed to get access token from Microsoft: {ex.Message} - {error.Error}: {error.ErrorDescription}", ex);
            }
            catch (Exception ex)
            {
                throw new NogginNetCoreAuthException($"Failed to get access token from Microsoft: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets user information from Microsoft API
        /// </summary>
        /// <param name="authToken">Microsoft token</param>
        /// <exception cref="NogginNetCoreAuthException"></exception>
        /// <remarks>Microsoft API ref: </remarks>
        protected static async Task<UserInformation> RetrieveUserInformationAsync(string authToken)
        {
            UserResult? user;

            var request = _msGraphApiUrl
                .AppendPathSegment("me")
                .WithOAuthBearerToken(authToken)
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", NogginAuthUserAgentName);

            try
            {
                var response = await request.GetAsync();
                user = await request.GetJsonAsync<UserResult>();
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseJsonAsync<ErrorResult>();
                throw new NogginNetCoreAuthException($"Failed to get user from Microsoft: {ex.Message} - {error.Error}: {error.ErrorDescription}", ex);
            }
            catch (Exception ex)
            {
                throw new NogginNetCoreAuthException($"Failed to get user from Microsoft: {ex.Message}", ex);
            }

            if (user == null)
            {
                throw new NogginNetCoreAuthException("Failed to get user from Microsoft. No user information returned.");
            }

            var userInformation = new UserInformation
            {
                Id = user!.Id,
                Name = user.DisplayName,
                Email = user.Mail,
                UserName = user.UserPrincipalName
            };

            return userInformation;
        }
    }
}
