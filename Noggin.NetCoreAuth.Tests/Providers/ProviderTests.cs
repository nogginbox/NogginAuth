using Flurl;
using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Noggin.NetCoreAuth.Tests.Providers;

/// <summary>
/// Tests for the login flow that the Provider base class runs for every provider
/// </summary>
public class ProviderTests
{
    private readonly FakeProvider _provider = new();

    [Theory]
    [InlineData("?code=abc&state=wrong-state", "the-state")]   // State doesn't match
    [InlineData("?code=abc", "the-state")]                     // No state returned
    [InlineData("?code=abc&state=", "the-state")]              // Empty state returned
    [InlineData("?code=abc&state=the-state", null)]            // Login not started in this session (login CSRF)
    [InlineData("?code=abc&state=the-state", "")]
    public async Task AuthenticateUserWithBadStateThrowsWithoutUsingCode(string callbackQuery, string? secret)
    {
        await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => _provider.AuthenticateUser(CreateCallback(callbackQuery), secret));

        Assert.Null(_provider.CodeUsed);
    }

    [Fact]
    public async Task AuthenticateUserWithMatchingStateUsesCodeAndReturnsUser()
    {
        var user = await _provider.AuthenticateUser(CreateCallback("?code=abc&state=the-state"), "the-state");

        Assert.Equal("abc", _provider.CodeUsed);
        Assert.Equal("user-id", user.Id);
    }

    [Fact]
    public async Task AuthenticateUserWithNoCodeThrows()
    {
        var ex = await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => _provider.AuthenticateUser(CreateCallback("?state=the-state"), "the-state"));

        Assert.Equal("No auth code returned by fake", ex.Message);
        Assert.Null(_provider.CodeUsed);
    }

    [Theory]
    [InlineData("&error=access_denied&error_description=Permissions+error", "fake: access_denied - Permissions error")]
    [InlineData("&error=access_denied&error_reason=user_denied&error_description=Permissions+error", "fake: access_denied (user_denied) - Permissions error")]
    public async Task AuthenticateUserWithProviderErrorThrowsWithErrorDetails(string errorQuery, string expectedMessage)
    {
        var callback = CreateCallback("?state=the-state" + errorQuery);

        var ex = await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => _provider.AuthenticateUser(callback, "the-state"));

        Assert.Equal(expectedMessage, ex.Message);
        Assert.Null(_provider.CodeUsed);
    }

    private static HttpRequest CreateCallback(string queryString)
    {
        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString(queryString);
        return request;
    }

    [Fact]
    public async Task GenerateStartRequestUrlCreatesDifferentSecretEachTime()
    {
        var secrets = new List<string>();
        for (var i = 0; i < 100; i++)
        {
            var (_, secret) = await _provider.GenerateStartRequestUrl(new DefaultHttpContext().Request);
            secrets.Add(secret);
        }

        Assert.Equal(secrets.Count, secrets.Distinct().Count());
    }

    [Fact]
    public async Task GenerateStartRequestUrlCreatesUrlSafeSecret()
    {
        var (_, secret) = await _provider.GenerateStartRequestUrl(new DefaultHttpContext().Request);

        // 32 random bytes, base64url encoded without padding
        Assert.Equal(43, secret.Length);
        Assert.Equal(Uri.EscapeDataString(secret), secret);
    }

    [Fact]
    public async Task GenerateStartRequestUrlPutsSecretInUrlAsState()
    {
        var (url, secret) = await _provider.GenerateStartRequestUrl(new DefaultHttpContext().Request);

        Assert.Equal(secret, new Url(url).QueryParams.FirstOrDefault("state"));
    }
}
