using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers.Google;
using Noggin.NetCoreAuth.Providers.Google.Model;
using NSubstitute;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Noggin.NetCoreAuth.Tests.Providers
{
    public class GoogleProviderTests : BaseForProviderTests
    {
        [Fact]
        public async Task GenerateStartRequestDoesNotCallGoogleApi()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();

            var provider = new GoogleProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();

            // Act
            var result = await provider.GenerateStartRequestUrl(http);

            // Assert
            await restClient.DidNotReceive().ExecuteTaskAsync<AccessTokenResult>(Arg.Any<IRestRequest>());
        }

        [Fact]
        public async Task GenerateStartRequestUrlReturnsToken()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();

            // Arrange - Calling Google API succeeds

            var provider = new GoogleProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();

            // Act 
            var result = await provider.GenerateStartRequestUrl(http);

            // Assert
            Assert.NotNull(result.url);
            Assert.NotNull(result.secret);
            Assert.True(Guid.TryParse(result.secret, out var guidSecret));
        }

        [Fact]
        public async Task AuthenticateUserThrowsExceptionIfGoogleApiFails()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();

            // Arrnage - Calling Google API for token succeeds
            SetupTokenResultSuccess(restClient, "token", "secret");

            // Arrange - Calling Google API fails
            var response = Substitute.For<IRestResponse<AccessTokenResult>>();
            response.IsSuccessful.Returns(false);
            restClient.ExecuteTaskAsync<AccessTokenResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(response));

            var provider = new GoogleProvider(config, restClientFactory, "url1", "url2");

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


            // Arrange - Calling Google API for token
            SetupTokenResultSuccess(restClient, "token", "secret");

            // Arrange - Calling Google for user details
            var googleResponse = Substitute.For<IRestResponse<UserInfoResult>>();
            googleResponse.IsSuccessful.Returns(true);
            googleResponse.StatusCode.Returns(HttpStatusCode.OK);
            googleResponse.Data.Returns(new UserInfoResult
            {
                Id = "TestId",
                DisplayName = "RichardG2268",
                Name = new Name { FamilyName = "Garside", GivenName = "Richard" },
                Language = "en-GB",
                Image = new Image { Url = "hotdang.jpg" }
            });
            restClient.ExecuteTaskAsync<UserInfoResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(googleResponse));

            var provider = new GoogleProvider(config, restClientFactory, "url1", "url2");

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
            Assert.Equal("hotdang.jpg", authenticatedUser.Picture);
        }

        private ProviderConfig CreateProviderConfig()
        {
            return new ProviderConfig
            {
                Name = "Google",
                Api = new ApiConfig
                {
                    PrivateKey = "privateKey",
                    PublicKey = "publicKey"
                }
            };
        }

        private void SetupTokenResultSuccess(IRestClient restClient, string token, string secret)
        {
            var googleResponse = Substitute.For<IRestResponse<AccessTokenResult>>();
            googleResponse.IsSuccessful.Returns(true);
            googleResponse.StatusCode.Returns(HttpStatusCode.OK);
            googleResponse.Data.Returns(new AccessTokenResult { AccessToken = token });
            restClient.ExecuteTaskAsync<AccessTokenResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(googleResponse));
        }
    }
}