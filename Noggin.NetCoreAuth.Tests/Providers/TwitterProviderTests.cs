using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers;
using Noggin.NetCoreAuth.Providers.Twitter;
using Noggin.NetCoreAuth.Providers.Twitter.Model;
using NSubstitute;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
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

            // Arrange - Calling Twitter API fails
            var twitterResponse = Substitute.For<IRestResponse<TokenResult>>();
            twitterResponse.IsSuccessful.Returns(true);
            twitterResponse.Data.Returns(new TokenResult { OauthToken = "TestToken", OauthTokenSecret = "TestSecret" });
            restClient.ExecuteTaskAsync<TokenResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(twitterResponse));

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
    }
}