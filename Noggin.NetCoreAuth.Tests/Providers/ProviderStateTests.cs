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
/// Tests for the OAuth state handling that the Provider base class does for every provider
/// </summary>
public class ProviderStateTests
{
    private readonly FakeProvider _provider = new();

    [Theory]
    [InlineData("?code=abc&state=wrong-state", "the-state")]   // State doesn't match
    [InlineData("?code=abc", "the-state")]                     // No state returned
    [InlineData("?code=abc&state=", "the-state")]              // Empty state returned
    [InlineData("?code=abc&state=the-state", null)]            // Login not started in this session (login CSRF)
    [InlineData("?code=abc&state=the-state", "")]
    public async Task AuthenticateUserWithBadStateThrowsWithoutExchangingCode(string callbackQuery, string? secret)
    {
        await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => _provider.AuthenticateUser(CreateCallback(callbackQuery), secret));

        Assert.False(_provider.ExchangeCodeForUserCalled);
    }

    [Fact]
    public async Task AuthenticateUserWithMatchingStateExchangesCode()
    {
        var user = await _provider.AuthenticateUser(CreateCallback("?code=abc&state=the-state"), "the-state");

        Assert.True(_provider.ExchangeCodeForUserCalled);
        Assert.Equal("user-id", user.Id);
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
