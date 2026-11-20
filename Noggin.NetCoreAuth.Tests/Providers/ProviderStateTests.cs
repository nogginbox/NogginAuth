using Flurl;
using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Exceptions;
using System;
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

    [Fact]
    public void StartLoginPutsStateInUrl()
    {
        var (url, state) = _provider.StartLogin(new DefaultHttpContext().Request);

        Assert.Equal(state, new Url(url).QueryParams.FirstOrDefault("state"));
    }

    [Fact]
    public void StartLoginCreatesUrlSafeState()
    {
        var (_, state) = _provider.StartLogin(new DefaultHttpContext().Request);

        // 32 random bytes, base64url encoded without padding
        Assert.Equal(43, state.Length);
        Assert.Equal(Uri.EscapeDataString(state), state);
    }

    [Fact]
    public void StartLoginCreatesDifferentStateEachTime()
    {
        var states = Enumerable.Range(0, 100)
            .Select(_ => _provider.StartLogin(new DefaultHttpContext().Request).state)
            .ToList();

        Assert.Equal(states.Count, states.Distinct().Count());
    }

    [Fact]
    public async Task CompleteLoginWithMatchingStateExchangesCode()
    {
        var user = await _provider.CompleteLogin(CreateCallback("?code=abc&state=the-state"), "the-state");

        Assert.True(_provider.ExchangeCodeForUserCalled);
        Assert.Equal("user-id", user.Id);
    }

    [Theory]
    [InlineData("?code=abc&state=wrong-state", "the-state")]   // State doesn't match
    [InlineData("?code=abc", "the-state")]                     // No state returned
    [InlineData("?code=abc&state=", "the-state")]              // Empty state returned
    [InlineData("?code=abc&state=the-state", null)]            // Login not started in this session (login CSRF)
    [InlineData("?code=abc&state=the-state", "")]
    public async Task CompleteLoginWithBadStateThrowsWithoutExchangingCode(string callbackQuery, string? expectedState)
    {
        await Assert.ThrowsAsync<NogginNetCoreAuthException>(() => _provider.CompleteLogin(CreateCallback(callbackQuery), expectedState));

        Assert.False(_provider.ExchangeCodeForUserCalled);
    }

    private static HttpRequest CreateCallback(string queryString)
    {
        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString(queryString);
        return request;
    }
}
