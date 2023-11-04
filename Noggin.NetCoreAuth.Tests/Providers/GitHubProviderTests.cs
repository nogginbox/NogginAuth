using Flurl.Http.Testing;
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

namespace Noggin.NetCoreAuth.Tests.Providers;

public class GitHubProviderTests : BaseForProviderTests
{
    [Fact]
    public async Task GenerateStartRequestDoesNotCallGitHubApi()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        var provider = new GitHubProvider(config, "url1", "url2");

        var http = Substitute.For<HttpRequest>();

        // Act
        var (url, secret) = await provider.GenerateStartRequestUrl(http);

        // Assert
        httpTest.ShouldNotHaveMadeACall();
    }

    [Fact]
    public async Task GenerateStartRequestUrlReturnsToken()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        // Arrange - Calling GitHub API succeeds

        var provider = new GitHubProvider(config, "url1", "url2");

        var http = Substitute.For<HttpRequest>();

        // Act 
        var (url, secret) = await provider.GenerateStartRequestUrl(http);

        // Assert
        Assert.NotNull(url);
    }

    [Fact]
    public async Task AuthenticateUserThrowsExceptionIfGitHubApiFails()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        // Arrange - Calling GitHub API for token succeeds
        SetupTokenResultSuccess(httpTest, "token");

        // Arrange - Calling GitHub API to get user fails
        var badUser = new UserResult { Message = "Failure for reason." };
        httpTest.RespondWithJson(badUser, 400);

        var provider = new GitHubProvider(config, "url1", "url2");

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
        using var httpTest = new HttpTest();

        // Arrange - Calling GitHub API for token
        SetupTokenResultSuccess(httpTest, "token");

        // Arrange - Calling GitHub for user details
        var gitHubUser = new UserResult
        {
            Id = 2268,
            Login = "NogginBox",
            Name = "Richard Garside",
            AvatarUrl = "lookingood.jpg"
        };
        httpTest.RespondWithJson(gitHubUser);

        var provider = new GitHubProvider(config, "url1", "url2");

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

    private static void SetupTokenResultSuccess(HttpTest httpTest, string token)
    {
        var gitHubResponse = new AccessTokenResult { AccessToken = token };
        httpTest.RespondWithJson(gitHubResponse);
    }
    
}