using Flurl;
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
using Microsoft.Extensions.Logging.Abstractions;
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

        var provider = new MicrosoftProvider(config, "url1", "url2", NullLogger<MicrosoftProvider>.Instance);

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

        // Arrange - Calling Microsoft API succeeds

        var provider = new MicrosoftProvider(config, "url1", "url2", NullLogger<MicrosoftProvider>.Instance);

        var http = Substitute.For<HttpRequest>();

        // Act 
        var url = await provider.GenerateStartRequestUrl(http, "TestState");

        // Assert
        Assert.Equal("TestState", new Url(url).QueryParams.FirstOrDefault("state"));
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

        var provider = new MicrosoftProvider(config, "url1", "url2", NullLogger<MicrosoftProvider>.Instance);

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

        var provider = new MicrosoftProvider(config, "url1", "url2", NullLogger<MicrosoftProvider>.Instance);

        var http = Substitute.For<HttpRequest>();
        http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
            { "code", new StringValues("TestCode") },
        }));

        // Act
        var authenticatedUser = await provider.AuthenticateUser(http);

        // Assert
        Assert.Equal("Richard Garside", authenticatedUser.Name);
        Assert.Equal("NogginBox", authenticatedUser.UserName);
        Assert.Null(authenticatedUser.Picture);
    }

    /// <summary>
    /// Uses raw JSON in the shape Microsoft identity platform and Graph /me actually send
    /// (including @odata.context, nulls and extra fields), rather than serialising our own model,
    /// so serializer/type mismatches are caught.
    /// </summary>
    [Fact]
    public async Task AuthenticateUserParsesRealMicrosoftJson()
    {
        // Arrange
        var config = CreateProviderConfig();
        using var httpTest = new HttpTest();

        httpTest.RespondWith("""
            {
              "token_type": "Bearer",
              "scope": "openid https://graph.microsoft.com/User.Read",
              "expires_in": 3599,
              "ext_expires_in": 3599,
              "access_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.test.sig"
            }
            """);

        httpTest.RespondWith("""
            {
              "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#users/$entity",
              "businessPhones": [],
              "displayName": "Test User",
              "givenName": "Test",
              "jobTitle": null,
              "mail": "test.user@example.com",
              "mobilePhone": null,
              "officeLocation": null,
              "preferredLanguage": "en-GB",
              "surname": "User",
              "userPrincipalName": "test.user@example.com",
              "id": "48d31887-5fad-4d73-a9f5-3c356e68a038"
            }
            """);

        var provider = new MicrosoftProvider(config, "url1", "url2", NullLogger<MicrosoftProvider>.Instance);

        var http = Substitute.For<HttpRequest>();
        http.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
            { "code", new StringValues("TestCode") },
        }));

        // Act
        var authenticatedUser = await provider.AuthenticateUser(http);

        // Assert
        Assert.Equal("48d31887-5fad-4d73-a9f5-3c356e68a038", authenticatedUser.Id);
        Assert.Equal("Test User", authenticatedUser.Name);
        Assert.Equal("test.user@example.com", authenticatedUser.Email);
        Assert.Equal("test.user@example.com", authenticatedUser.UserName);
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