using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers.GitHub;
using Noggin.NetCoreAuth.Providers.GitHub.Model;
using NSubstitute;
using RestSharp;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Noggin.NetCoreAuth.Tests.Providers
{
    public class GitHubProviderTests : BaseForProviderTests
    {
        [Fact]
        public async Task GenerateStartRequestDoesNotCallGitHubApi()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();

            var provider = new GitHubProvider(config, restClientFactory, "url1", "url2");

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

            // Arrange - Calling GitHub API succeeds

            var provider = new GitHubProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();

            // Act 
            var result = await provider.GenerateStartRequestUrl(http);

            // Assert
            Assert.NotNull(result.url);
        }

        [Fact]
        public async Task AuthenticateUserThrowsExceptionIfGitHubApiFails()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();

            // Arrnage - Calling GitHub API for token succeeds
            SetupTokenResultSuccess(restClient, "token");

            // Arrange - Calling GitHub API to get user fails
            var response = Substitute.For<IRestResponse<UserResult>>();
            response.IsSuccessful.Returns(false);
            restClient.ExecuteTaskAsync<UserResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(response));

            var provider = new GitHubProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();
            http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
                { "code", new StringValues("TestCode") }
            }));

            // Act / Assert
            await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => provider.AuthenticateUser(http, ""));
        }

        [Fact]
        public async Task AuthenticateUserReturnsUserInfo()
        {
            // Arrange
            var config = CreateProviderConfig();
            var (restClientFactory, restClient) = CreateRestClientAndFactory();

            // Arrange - Calling GitHub API for token
            SetupTokenResultSuccess(restClient, "token");

            // Arrange - Calling GitHub for user details
            var gitHubResponse = Substitute.For<IRestResponse<UserResult>>();
            gitHubResponse.IsSuccessful.Returns(true);
            gitHubResponse.StatusCode.Returns(HttpStatusCode.OK);
            gitHubResponse.Data.Returns(new UserResult
            {
                Id = 2268,
                Login = "NogginBox",
                Name = "Richard Garside",
                AvatarUrl = "lookingood.jpg"
            });
            restClient.ExecuteTaskAsync<UserResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(gitHubResponse));

            var provider = new GitHubProvider(config, restClientFactory, "url1", "url2");

            var http = Substitute.For<HttpRequest>();
            http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
                { "code", new StringValues("TestCode") },
            }));

            // Act
            var authenticatedUser = await provider.AuthenticateUser(http, "secret");

            // Assert
            Assert.Equal("Richard Garside", authenticatedUser.Name);
            Assert.Equal("NogginBox", authenticatedUser.UserName);
            Assert.Equal("lookingood.jpg", authenticatedUser.Picture);
        }

        private static ProviderConfig CreateProviderConfig()
        {
            return new ProviderConfig
            {
                Name = "GitHub",
                Api = new ApiConfig
                {
                    PrivateKey = "privateKey",
                    PublicKey = "publicKey"
                }
            };
        }

        private static void SetupTokenResultSuccess(IRestClient restClient, string token)
        {
            var gitHubResponse = Substitute.For<IRestResponse<AccessTokenResult>>();
            gitHubResponse.IsSuccessful.Returns(true);
            gitHubResponse.StatusCode.Returns(HttpStatusCode.OK);
            gitHubResponse.Data.Returns(new AccessTokenResult { AccessToken = token });
            restClient.ExecuteTaskAsync<AccessTokenResult>(Arg.Any<RestRequest>()).Returns(Task.FromResult(gitHubResponse));
        }
    }
}