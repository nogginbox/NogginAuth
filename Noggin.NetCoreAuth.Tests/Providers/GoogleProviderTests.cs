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
using Xunit;

namespace Noggin.NetCoreAuth.Tests.Providers;

public class GoogleProviderTests : BaseForProviderTests
{
    [Fact]
    public async Task GenerateStartRequestDoesNotCallGoogleApi()
    {
        // Arrange
        var config = CreateProviderConfig();
        var provider = new GoogleProvider(config, "url1", "url2");
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
        var provider = new GoogleProvider(config, "url1", "url2");
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
        var (restClientFactory, restClient) = CreateRestClientAndFactory();

        // Arrange - Calling Google API for token succeeds
        using var httpTest = new HttpTest();
        SetupTokenResultSuccess(httpTest, "token", "secret");

        // Arrange - Calling Google API fails
        
        var tokenResponse = new AccessTokenResult();
        httpTest.RespondWithJson(tokenResponse);

        var provider = new GoogleProvider(config, "url1", "url2");

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

        var provider = new GoogleProvider(config, "url1", "url2");

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