using Flurl.Http.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers.Google;
using Noggin.NetCoreAuth.Providers.Google.Model;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Noggin.NetCoreAuth.Tests.Providers;

public class GoogleProviderTests : BaseForProviderTests
{
    [Fact]
    public async Task GenerateStartRequestDoesNotCallGoogleApi()
    {
        // Arrange
        var config = CreateProviderConfig();
        var provider = new GoogleProvider(config, "url1", "url2", NullLogger<GoogleProvider>.Instance);
        var http = Substitute.For<HttpRequest>();
        using var httpTest = new HttpTest();

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

        // Arrange - Calling Google API succeeds
        var provider = new GoogleProvider(config, "url1", "url2", NullLogger<GoogleProvider>.Instance);
        var http = Substitute.For<HttpRequest>();
        using var httpTest = new HttpTest();
        
        // Act
        var (url, secret) = await provider.GenerateStartRequestUrl(http);

        // Assert
        Assert.NotNull(url);
        Assert.NotNull(secret);
        Assert.True(Guid.TryParse(secret, out var guidSecret));
    }

    [Fact]
    public async Task AuthenticateUserThrowsExceptionIfGoogleApiFails()
    {
        // Arrange
        var config = CreateProviderConfig();

        // Arrange - Calling Google API for token succeeds
        using var httpTest = new HttpTest();
        SetupTokenResultSuccess(httpTest, "token", "secret");

        // Arrange - Calling Google API fails
        
        var tokenResponse = new AccessTokenResult();
        httpTest.RespondWithJson(tokenResponse);

        var provider = new GoogleProvider(config, "url1", "url2", NullLogger<GoogleProvider>.Instance);

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

        // Arrange - Calling Google API for token
        using var httpTest = new HttpTest();
        SetupTokenResultSuccess(httpTest, "token", "secret");

        // Arrange - Calling Google for user details
        var googleResponse = new UserInfoResult
        {
            Sub = "TestId",
            Email = "testemail@nogginbox.co.uk",
            Name = "Richard Garside",
            FamilyName = "Garside",
            GivenName = "Richard",
            Picture = "hotdang.jpg"
        };
        httpTest.RespondWithJson(googleResponse);

        var provider = new GoogleProvider(config, "url1", "url2", NullLogger<GoogleProvider>.Instance);

        var http = Substitute.For<HttpRequest>();
        http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
            { "state", new StringValues("TestState") },
            { "code", new StringValues("TestCode") },
        }));

        // Act
        var authenticatedUser = await provider.AuthenticateUser(http, "secret");

        // Assert
        Assert.Equal("Richard Garside", authenticatedUser.Name);
        Assert.Equal("testemail@nogginbox.co.uk", authenticatedUser.UserName);
        Assert.Equal("hotdang.jpg", authenticatedUser.Picture);
    }

    /// <summary>
    /// Uses raw JSON in the shape Google's OAuth and v3 userinfo endpoints actually send,
    /// rather than serialising our own model, so serializer/type mismatches are caught.
    /// </summary>
    [Fact]
    public async Task AuthenticateUserParsesRealGoogleJson()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        httpTest.RespondWith("""
            {
              "access_token": "ya29.a0AfH6SMBtestaccesstoken",
              "expires_in": 3599,
              "scope": "openid https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile",
              "token_type": "Bearer",
              "id_token": "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiIxMTAxNjk0ODQ0NzQzODYyNzYzMzQifQ.sig"
            }
            """);

        httpTest.RespondWith("""
            {
              "sub": "110169484474386276334",
              "name": "Test User",
              "given_name": "Test",
              "family_name": "User",
              "picture": "https://lh3.googleusercontent.com/a/test-picture",
              "email": "test.user@example.com",
              "email_verified": true,
              "hd": "example.com"
            }
            """);

        var provider = new GoogleProvider(config, "url1", "url2", NullLogger<GoogleProvider>.Instance);

        var http = Substitute.For<HttpRequest>();
        http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
            { "state", new StringValues("TestState") },
            { "code", new StringValues("TestCode") },
        }));

        // Act
        var authenticatedUser = await provider.AuthenticateUser(http, "secret");

        // Assert
        Assert.Equal("110169484474386276334", authenticatedUser.Id);
        Assert.Equal("Test User", authenticatedUser.Name);
        Assert.Equal("test.user@example.com", authenticatedUser.Email);
        Assert.Equal("test.user@example.com", authenticatedUser.UserName);
        Assert.Equal("https://lh3.googleusercontent.com/a/test-picture", authenticatedUser.Picture);
    }

    private static ProviderConfig CreateProviderConfig()
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

    private static void SetupTokenResultSuccess(HttpTest httpTest, string token, string secret)
    {
        var googleResponse = new AccessTokenResult { AccessToken = token };
        httpTest.RespondWithJson(googleResponse);
    }
}