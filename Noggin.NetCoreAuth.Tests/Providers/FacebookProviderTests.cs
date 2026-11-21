using Flurl;
using Flurl.Http.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers.Facebook;
using Noggin.NetCoreAuth.Providers.Facebook.Model;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Noggin.NetCoreAuth.Tests.Providers;

public class FacebookProviderTests : BaseForProviderTests
{
    [Fact]
    public async Task GenerateStartRequestDoesNotCallFacebookApi()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        var provider = new FacebookProvider(config, "url1", "url2", NullLogger<FacebookProvider>.Instance);

        var http = Substitute.For<HttpRequest>();

        // Act
        var (url, secret) = await provider.GenerateStartRequestUrl(http);

        // Assert
        httpTest.ShouldNotHaveMadeACall();
    }

    [Fact]
    public async Task GenerateStartRequestUrlIncludesState()
    {
        // Arrange
        var config = CreateProviderConfig();

        // Arrange - Calling Facebook API succeeds

        var provider = new FacebookProvider(config, "url1", "url2", NullLogger<FacebookProvider>.Instance);

        var http = Substitute.For<HttpRequest>();

        // Act 
        var (url, secret) = await provider.GenerateStartRequestUrl(http);

        // Assert
        Assert.Equal(secret, new Url(url).QueryParams.FirstOrDefault("state"));
    }

    [Fact]
    public async Task GenerateStartRequestUrlUsesGraphApiVersion()
    {
        // Arrange
        var config = CreateProviderConfig();
        var provider = new FacebookProvider(config, "url1", "url2", NullLogger<FacebookProvider>.Instance);
        var http = Substitute.For<HttpRequest>();

        // Act
        var (url, _) = await provider.GenerateStartRequestUrl(http);

        // Assert
        Assert.StartsWith("https://www.facebook.com/v27.0/dialog/oauth?", url);
    }

    [Fact]
    public async Task AuthenticateUserThrowsExceptionIfFacebookApiFails()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        // Arrange - Calling Facebook API for token succeeds
        SetupTokenResultSuccess(httpTest, "token");

        // Arrange - Calling Facebook API fails
        var response = new MeResult
        {
            Error = new Error
            {
                Code = 666
            }
        };

        httpTest.RespondWithJson(response, 500);

        var provider = new FacebookProvider(config, "url1", "url2", NullLogger<FacebookProvider>.Instance);

        var http = Substitute.For<HttpRequest>();
        http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
            { "state", new StringValues("TestState") },
            { "code", new StringValues("TestCode") },
        }));

        // Act / Assert
        await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => provider.AuthenticateUser(http, "TestState"));
    }

    [Fact]
    public async Task AuthenticateUserReturnsUserInfo()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();


        // Arrange - Calling Facebook API for token
        SetupTokenResultSuccess(httpTest, "token");

        // Arrange - Calling Facebook for user details
        var facebookResponse = new MeResult
        {
            Id = "2268",
            Name = "Richard Garside",
            Email = "code@nogginbox.co.uk"
        };
        httpTest.RespondWithJson(facebookResponse);

        var provider = new FacebookProvider(config, "url1", "url2", NullLogger<FacebookProvider>.Instance);

        var http = Substitute.For<HttpRequest>();
        http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
            { "state", new StringValues("TestState") },
            { "code", new StringValues("TestCode") },
        }));

        // Act
        var authenticatedUser = await provider.AuthenticateUser(http, "TestState");

        // Assert
        Assert.Equal("Richard Garside", authenticatedUser.Name);
        Assert.Equal("code@nogginbox.co.uk", authenticatedUser.Email);
        Assert.Equal("https://graph.facebook.com/2268/picture", authenticatedUser.Picture);
    }

    /// <summary>
    /// Uses raw JSON in the shape Facebook's Graph API actually sends (note the id is a JSON string),
    /// rather than serialising our own model, so serializer/type mismatches are caught.
    /// </summary>
    [Fact]
    public async Task AuthenticateUserParsesRealFacebookJson()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        httpTest.RespondWith("""
            {
              "access_token": "EAAGm0PX4ZCpsBAKZCZBZBtestaccesstoken",
              "token_type": "bearer",
              "expires_in": 5183944
            }
            """);

        httpTest.RespondWith("""
            {
              "id": "10158123456789012",
              "name": "Test User",
              "email": "test.user@example.com",
              "first_name": "Test",
              "last_name": "User"
            }
            """);

        var provider = new FacebookProvider(config, "url1", "url2", NullLogger<FacebookProvider>.Instance);
        var http = CreateCallbackRequest();

        // Act
        var authenticatedUser = await provider.AuthenticateUser(http, "TestState");

        // Assert
        Assert.Equal("10158123456789012", authenticatedUser.Id);
        Assert.Equal("Test User", authenticatedUser.Name);
        Assert.Equal("test.user@example.com", authenticatedUser.Email);
        Assert.Equal("https://graph.facebook.com/10158123456789012/picture", authenticatedUser.Picture);
    }

    [Fact]
    public async Task AuthenticateUserThrowsExceptionIfFacebookIdMissing()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        SetupTokenResultSuccess(httpTest, "token");
        httpTest.RespondWith("""{ "name": "Test User", "email": "test.user@example.com" }""");

        var provider = new FacebookProvider(config, "url1", "url2", NullLogger<FacebookProvider>.Instance);
        var http = CreateCallbackRequest();

        // Act / Assert
        await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => provider.AuthenticateUser(http, "TestState"));
    }

    [Fact]
    public async Task AuthenticateUserErrorDoesNotIncludeAccessToken()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        SetupTokenResultSuccess(httpTest, "SecretAccessToken123");
        httpTest.RespondWith("""{ "error": { "message": "Invalid OAuth access token.", "code": 190 } }""", 400);

        var provider = new FacebookProvider(config, "url1", "url2", NullLogger<FacebookProvider>.Instance);
        var http = CreateCallbackRequest();

        // Act
        var ex = await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => provider.AuthenticateUser(http, "TestState"));

        // Assert - ToString includes any inner exceptions, which is what gets logged
        Assert.DoesNotContain("SecretAccessToken123", ex.ToString());
    }

    [Fact]
    public async Task AuthenticateUserErrorDoesNotIncludeClientSecretOrCode()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        httpTest.RespondWith("""{ "error": { "message": "Error validating client secret.", "code": 1 } }""", 400);

        var provider = new FacebookProvider(config, "url1", "url2", NullLogger<FacebookProvider>.Instance);
        var http = CreateCallbackRequest();

        // Act
        var ex = await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => provider.AuthenticateUser(http, "TestState"));

        // Assert - ToString includes any inner exceptions, which is what gets logged
        var logged = ex.ToString();
        Assert.Contains("1: Error validating client secret.", logged);
        Assert.DoesNotContain("privateKey", logged);
        Assert.DoesNotContain("TestCode", logged);
    }

    [Fact]
    public async Task AuthenticateUserErrorIncludesFacebookErrorDetails()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        SetupTokenResultSuccess(httpTest, "token");

        // Raw JSON in the shape Facebook's Graph API sends for errors
        httpTest.RespondWith("""
            {
              "error": {
                "message": "Invalid OAuth access token.",
                "type": "OAuthException",
                "code": 190,
                "fbtrace_id": "AbCdEfGh123"
              }
            }
            """, 400);

        var provider = new FacebookProvider(config, "url1", "url2", NullLogger<FacebookProvider>.Instance);
        var http = CreateCallbackRequest();

        // Act
        var ex = await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => provider.AuthenticateUser(http, "TestState"));

        // Assert
        Assert.Contains("190: Invalid OAuth access token.", ex.Message);
        Assert.Contains("fbtrace_id: AbCdEfGh123", ex.Message);
    }

    [Fact]
    public async Task AuthenticateUserGetsAccessTokenFromGraphApi()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        SetupTokenResultSuccess(httpTest, "token");
        httpTest.RespondWithJson(new MeResult { Id = "2268" });

        var provider = new FacebookProvider(config, "url1", "url2", NullLogger<FacebookProvider>.Instance);
        var http = CreateCallbackRequest();

        // Act
        await provider.AuthenticateUser(http, "TestState");

        // Assert
        httpTest.ShouldHaveCalled("https://graph.facebook.com/v27.0/oauth/access_token?*")
            .WithVerb(HttpMethod.Get)
            .WithQueryParam("client_id", "publicKey")
            .WithQueryParam("client_secret", "privateKey")
            .WithQueryParam("code", "TestCode")
            .WithQueryParam("redirect_uri")
            .Times(1);
    }

    [Fact]
    public async Task AuthenticateUserGetsUserInfoFromGraphApi()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        SetupTokenResultSuccess(httpTest, "token");
        httpTest.RespondWithJson(new MeResult { Id = "2268" });

        var provider = new FacebookProvider(config, "url1", "url2", NullLogger<FacebookProvider>.Instance);
        var http = CreateCallbackRequest();

        // Act
        await provider.AuthenticateUser(http, "TestState");

        // Assert
        httpTest.ShouldHaveCalled("https://graph.facebook.com/v27.0/me?*")
            .WithVerb(HttpMethod.Get)
            .WithHeader("Authorization", "Bearer token")
            .WithQueryParam("fields", "id,name,email,first_name,last_name")
            .WithoutQueryParam("access_token")
            .Times(1);
    }

    private static HttpRequest CreateCallbackRequest()
    {
        var http = Substitute.For<HttpRequest>();
        http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
            { "state", new StringValues("TestState") },
            { "code", new StringValues("TestCode") },
        }));
        return http;
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

    private static void SetupTokenResultSuccess(HttpTest httpTest, string token)
    {
        var facebookResponse = new AccessTokenResult { AccessToken = token };
        httpTest.RespondWithJson(facebookResponse);
    }
}