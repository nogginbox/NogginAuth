using Flurl;
using Flurl.Http.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers.GitHub;
using Noggin.NetCoreAuth.Providers.GitHub.Model;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
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

        var provider = new GitHubProvider(config, "url1", "url2", NullLogger<GitHubProvider>.Instance);

        var http = Substitute.For<HttpRequest>();

        // Act
        var url = await provider.GenerateStartRequestUrl(http, "TestState");

        // Assert
        httpTest.ShouldNotHaveMadeACall();
    }

    [Fact]
    public async Task GenerateStartRequestUrlIncludesState()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        // Arrange - Calling GitHub API succeeds

        var provider = new GitHubProvider(config, "url1", "url2", NullLogger<GitHubProvider>.Instance);

        var http = Substitute.For<HttpRequest>();

        // Act 
        var url = await provider.GenerateStartRequestUrl(http, "TestState");

        // Assert
        Assert.Equal("TestState", new Url(url).QueryParams.FirstOrDefault("state"));
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

        var provider = new GitHubProvider(config, "url1", "url2", NullLogger<GitHubProvider>.Instance);

        var http = Substitute.For<HttpRequest>();
        http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
            { "code", new StringValues("TestCode") }
        }));

        // Act / Assert
        await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => provider.AuthenticateUser(http));
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

        var provider = new GitHubProvider(config, "url1", "url2", NullLogger<GitHubProvider>.Instance);

        var http = Substitute.For<HttpRequest>();
        http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
            { "code", new StringValues("TestCode") },
        }));

        // Act
        var authenticatedUser = await provider.AuthenticateUser(http);

        // Assert
        Assert.Equal("Richard Garside", authenticatedUser.Name);
        Assert.Equal("NogginBox", authenticatedUser.UserName);
        Assert.Equal("lookingood.jpg", authenticatedUser.Picture);
    }

    /// <summary>
    /// Uses raw JSON in the shape GitHub's API actually sends (numeric id, nulls, extra fields),
    /// rather than serialising our own model, so serializer/type mismatches are caught.
    /// </summary>
    [Fact]
    public async Task AuthenticateUserParsesRealGitHubJson()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        httpTest.RespondWith("""
            {
              "access_token": "gho_16C7e42F292c6912E7710c838347Ae178B4a",
              "scope": "read:user,user:email",
              "token_type": "bearer"
            }
            """);

        httpTest.RespondWith("""
            {
              "login": "testuser",
              "id": 1234567,
              "node_id": "MDQ6VXNlcjEyMzQ1Njc=",
              "avatar_url": "https://avatars.githubusercontent.com/u/1234567?v=4",
              "gravatar_id": "",
              "html_url": "https://github.com/testuser",
              "type": "User",
              "site_admin": false,
              "name": "Test User",
              "company": null,
              "blog": "",
              "location": "Leeds",
              "email": "test.user@example.com",
              "hireable": null,
              "bio": null,
              "public_repos": 42,
              "followers": 10,
              "following": 3,
              "created_at": "2010-01-14T04:33:35Z",
              "updated_at": "2026-01-01T12:00:00Z"
            }
            """);

        var provider = new GitHubProvider(config, "url1", "url2", NullLogger<GitHubProvider>.Instance);

        var http = Substitute.For<HttpRequest>();
        http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
            { "code", new StringValues("TestCode") },
        }));

        // Act
        var authenticatedUser = await provider.AuthenticateUser(http);

        // Assert
        Assert.Equal("1234567", authenticatedUser.Id);
        Assert.Equal("Test User", authenticatedUser.Name);
        Assert.Equal("testuser", authenticatedUser.UserName);
        Assert.Equal("test.user@example.com", authenticatedUser.Email);
        Assert.Equal("https://avatars.githubusercontent.com/u/1234567?v=4", authenticatedUser.Picture);
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