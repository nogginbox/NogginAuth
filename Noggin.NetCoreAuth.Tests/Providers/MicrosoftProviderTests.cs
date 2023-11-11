using Flurl.Http.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers.Microsoft;
using Noggin.NetCoreAuth.Providers.Microsoft.Model;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Noggin.NetCoreAuth.Tests.Providers;

public class MicrosoftProviderTests : BaseForProviderTests
{
    [Fact]
    public async Task GenerateStartRequestDoesNotCallMicrosoftApi()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        var provider = new MicrosoftProvider(config, "url1", "url2");

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

        // Arrange - Calling Microsoft API succeeds

        var provider = new MicrosoftProvider(config, "url1", "url2");

        var http = Substitute.For<HttpRequest>();

        // Act 
        var (url, secret) = await provider.GenerateStartRequestUrl(http);

        // Assert
        Assert.NotNull(url);
    }

    [Fact]
    public async Task AuthenticateUserThrowsExceptionIfMicrosoftApiFails()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        // Arrange - Calling Microsoft API for token succeeds
        SetupTokenResultSuccess(httpTest, "token");

        // Arrange - Calling Microsoft API to get user fails
        var errorResult = new ErrorResult { Error = "Test Error", ErrorDescription = "It'll all be okay by default." };
        httpTest.RespondWithJson(errorResult, 400);

        var provider = new MicrosoftProvider(config, "url1", "url2");

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

        // Arrange - Calling Microsoft API for token
        SetupTokenResultSuccess(httpTest, "token");

        // Arrange - Calling Microsoft for user details
        var msUser = new UserResult
        {
            Id = "msid-2268",
            UserPrincipalName = "NogginBox",
            DisplayName = "Richard Garside",
        };
        httpTest.RespondWithJson(msUser);

        var provider = new MicrosoftProvider(config, "url1", "url2");

        var http = Substitute.For<HttpRequest>();
        http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
            { "code", new StringValues("TestCode") },
        }));

        // Act
        var authenticatedUser = await provider.AuthenticateUser(http, "secret");

        // Assert
        Assert.Equal("Richard Garside", authenticatedUser.Name);
        Assert.Equal("NogginBox", authenticatedUser.UserName);
        Assert.Null(authenticatedUser.Picture);
    }

    private static ProviderConfig CreateProviderConfig()
    {
        return new ProviderConfig
        {
            Name = "Microsoft",
            Api = new ApiConfig
            {
                PrivateKey = "privateKey",
                PublicKey = "publicKey"
            }
        };
    }

    private static void SetupTokenResultSuccess(HttpTest httpTest, string token)
    {
        var msResponse = new AccessTokenResult { AccessToken = token };
        httpTest.RespondWithJson(msResponse);
    }   
}