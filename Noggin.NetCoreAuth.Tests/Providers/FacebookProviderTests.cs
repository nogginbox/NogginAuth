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
using System.Threading.Tasks;
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

        var provider = new FacebookProvider(config, "url1", "url2");

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

        // Arrange - Calling Facebook API succeeds

        var provider = new FacebookProvider(config, "url1", "url2");

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
        using var httpTest = new HttpTest();

        // Arrange - Calling Facebook API for token succeeds
        SetupTokenResultSuccess(httpTest, "token");

        // Arrange - Calling Facebook API fails
        var response = new MeResult
        {
            Error = new ErrorResult
            {
                Code = 666
            }
        };

        httpTest.RespondWithJson(response, 500);

        var provider = new FacebookProvider(config, "url1", "url2");

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
        using var httpTest = new HttpTest();


        // Arrange - Calling Facebook API for token
        SetupTokenResultSuccess(httpTest, "token");

        // Arrange - Calling Facebook for user details
        var facebookResponse = new MeResult
        {
            Id = 2268,
            Username = "RichardG2268",
            Name = "Richard Garside",
            Locale = "en-GB"
        };
        httpTest.RespondWithJson(facebookResponse);

        var provider = new FacebookProvider(config, "url1", "url2");

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

    private static void SetupTokenResultSuccess(HttpTest httpTest, string token)
    {
        var facebookResponse = new AccessTokenResult { AccessToken = token };
        httpTest.RespondWithJson(facebookResponse);
    }
}