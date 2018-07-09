using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers.Twitter;
using Noggin.NetCoreAuth.Providers.Twitter.Model;
using NSubstitute;
using RestSharp;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Noggin.NetCoreAuth.Tests
{
    public class TwitterProviderTests : BaseForProviderTests
    {
        [Fact]
        public async Task GenerateStartRequestUrlThrowsExceptionIfTwitterApiFails()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();

            // Arrange - Calling Twitter API fails
            var response = Substitute.For<IRestResponse<TokenResult>>();
            response.IsSuccessful.Returns(false);
            restClient.ExecuteTaskAsync<TokenResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(response));

            var provider = new TwitterProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();

            // Act / Assert
            await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => provider.GenerateStartRequestUrl(http)); 
        }

        [Fact]
        public async Task GenerateStartRequestUrlReturnsToken()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();

            // Arrange - Calling Twitter API succeeds
            SetupTokenResultSuccess(restClient, "TestToken", "TestSecret");

            var provider = new TwitterProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();

            // Act 
            var result = await provider.GenerateStartRequestUrl(http);

            // Assert
            Assert.Contains("=TestToken", result.url);
            Assert.Equal("TestSecret", result.secret);
        }

        [Fact]
        public async Task AuthenticateUserThrowsExceptionIfTwitterApiFails()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();

            // Arrnage - Calling Twitter API for token succeeds
            SetupTokenResultSuccess(restClient, "token", "secret");

            // Arrange - Calling Twitter API fails
            var response = Substitute.For<IRestResponse<TokenResult>>();
            response.IsSuccessful.Returns(false);
            restClient.ExecuteTaskAsync<TokenResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(response));

            var provider = new TwitterProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();
            http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
                { TwitterProvider.OAuthTokenKey, new StringValues("TestTokenKey") },
                { TwitterProvider.OAuthVerifierKey, new StringValues("TestVerifierKey") },
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


            // Arrange - Calling twitter API for token
            SetupTokenResultSuccess(restClient, "token", "secret");

            // Arrange - Calling Twitter for user details
            var twitterResponse = Substitute.For<IRestResponse<VerifyCredentialsResult>>();
            twitterResponse.IsSuccessful.Returns(true);
            twitterResponse.StatusCode.Returns(HttpStatusCode.OK);
            twitterResponse.Data.Returns(new VerifyCredentialsResult
            {
                Name = "Richard Garside",
                ScreenName = "_richardg",
                ProfileImageUrl = "hotdang.jpg"
            });
            restClient.ExecuteTaskAsync<VerifyCredentialsResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(twitterResponse));

            var provider = new TwitterProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();
            http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
                { TwitterProvider.OAuthTokenKey, new StringValues("TestTokenKey") },
                { TwitterProvider.OAuthVerifierKey, new StringValues("TestVerifierKey") },
            }));

            // Act
            var authenticatedUser = await provider.AuthenticateUser(http, "secret");

            // Assert
            Assert.Equal("Richard Garside", authenticatedUser.Name);
            Assert.Equal("_richardg", authenticatedUser.UserName);
            Assert.Equal("hotdang.jpg", authenticatedUser.Picture);
        }

        private ProviderConfig CreateProviderConfig()
        {
            return new ProviderConfig
            {
                Name = "Twitter",
                Api = new ApiConfig
                {
                    PrivateKey = "privateKey",
                    PublicKey = "publicKey"
                }
            };
        }

        private void SetupTokenResultSuccess(IRestClient restClient, string token, string secret)
        {
            var twitterResponse = Substitute.For<IRestResponse<TokenResult>>();
            twitterResponse.IsSuccessful.Returns(true);
            twitterResponse.StatusCode.Returns(HttpStatusCode.OK);
            twitterResponse.Data.Returns(new TokenResult { OauthToken = token, OauthTokenSecret = secret });
            restClient.ExecuteTaskAsync<TokenResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(twitterResponse));
        }
    }
}