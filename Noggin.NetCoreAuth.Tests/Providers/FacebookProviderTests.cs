using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers.Facebook;
using Noggin.NetCoreAuth.Providers.Facebook.Model;
using NSubstitute;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Noggin.NetCoreAuth.Tests.Providers
{
    public class FacebookProviderTests : BaseForProviderTests
    {
        [Fact]
        public async Task GenerateStartRequestDoesNotCallFacebookApi()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();

            var provider = new FacebookProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();

            // Act
            var result = await provider.GenerateStartRequestUrl(http);

            // Assert
            await restClient.DidNotReceive().ExecuteAsync<AccessTokenResult>(Arg.Any<IRestRequest>());
        }

        [Fact]
        public async Task GenerateStartRequestUrlReturnsToken()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();

            // Arrange - Calling Facebook API succeeds

            var provider = new FacebookProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();

            // Act 
            var result = await provider.GenerateStartRequestUrl(http);

            // Assert
            Assert.NotNull(result.url);
            Assert.NotNull(result.secret);
            Assert.True(Guid.TryParse(result.secret, out var guidSecret));
        }

        [Fact]
        public async Task AuthenticateUserThrowsExceptionIfFacebookApiFails()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();

            // Arrnage - Calling Facebook API for token succeeds
            SetupTokenResultSuccess(restClient, "token");

            // Arrange - Calling Facebook API fails
            var response = Substitute.For<IRestResponse<MeResult>>();
            response.IsSuccessful.Returns(false);
            restClient.ExecuteAsync<MeResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(response));

            var provider = new FacebookProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();
            http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
                { "state", new StringValues("TestState") },
                { "code", new StringValues("TestCode") },
            }));

            // Act / Assert
            await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => provider.AuthenticateUser(http, "secret"));
        }

        [Fact]
        public async Task AuthenticateUserReturnsUserInfo()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();


            // Arrange - Calling Facebook API for token
            SetupTokenResultSuccess(restClient, "token");

            // Arrange - Calling Facebook for user details
            var facebookResponse = Substitute.For<IRestResponse<MeResult>>();
            facebookResponse.IsSuccessful.Returns(true);
            facebookResponse.StatusCode.Returns(HttpStatusCode.OK);
            facebookResponse.Data.Returns(new MeResult
            {
                Id = 2268,
                Username = "RichardG2268",
                Name = "Richard Garside",
                Locale = "en-GB"
            });
            restClient.ExecuteAsync<MeResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(facebookResponse));

            var provider = new FacebookProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();
            http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
                { "state", new StringValues("TestState") },
                { "code", new StringValues("TestCode") },
            }));

            // Act
            var authenticatedUser = await provider.AuthenticateUser(http, "secret");

            // Assert
            Assert.Equal("Richard Garside", authenticatedUser.Name);
            Assert.Equal("RichardG2268", authenticatedUser.UserName);
            Assert.Equal("https://graph.facebook.com/2268/picture", authenticatedUser.Picture);
        }

        private static ProviderConfig CreateProviderConfig()
        {
            return new ProviderConfig
            {
                Name = "Facebook",
                Api = new ApiConfig
                {
                    PrivateKey = "privateKey",
                    PublicKey = "publicKey"
                }
            };
        }

        private static void SetupTokenResultSuccess(IRestClient restClient, string token)
        {
            var facebookResponse = Substitute.For<IRestResponse<AccessTokenResult>>();
            facebookResponse.IsSuccessful.Returns(true);
            facebookResponse.StatusCode.Returns(HttpStatusCode.OK);
            facebookResponse.Data.Returns(new AccessTokenResult { AccessToken = token });
            restClient.ExecuteAsync<AccessTokenResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(facebookResponse));
        }
    }
}