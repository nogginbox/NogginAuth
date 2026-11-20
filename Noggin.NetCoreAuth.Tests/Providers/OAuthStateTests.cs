using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers;
using System;
using System.Linq;
using Xunit;

namespace Noggin.NetCoreAuth.Tests.Providers;

public class OAuthStateTests
{
    [Fact]
    public void CreateReturnsUrlSafeValue()
    {
        var state = OAuthState.Create();

        // 32 bytes base64url encoded without padding
        Assert.Equal(43, state.Length);
        Assert.Equal(Uri.EscapeDataString(state), state);
    }

    [Fact]
    public void CreateReturnsDifferentValueEachTime()
    {
        var states = Enumerable.Range(0, 100).Select(_ => OAuthState.Create()).ToList();

        Assert.Equal(states.Count, states.Distinct().Count());
    }

    [Fact]
    public void ValidateAcceptsMatchingState()
    {
        var state = OAuthState.Create();

        var ex = Record.Exception(() => OAuthState.Validate(state, state));

        Assert.Null(ex);
    }

    [Fact]
    public void ValidateRejectsDifferentState()
    {
        Assert.Throws<NogginNetCoreAuthException>(() => OAuthState.Validate(OAuthState.Create(), OAuthState.Create()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateRejectsMissingSessionState(string? expectedState)
    {
        Assert.Throws<NogginNetCoreAuthException>(() => OAuthState.Validate(expectedState, "returned-state"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateRejectsMissingReturnedState(string? returnedState)
    {
        Assert.Throws<NogginNetCoreAuthException>(() => OAuthState.Validate("expected-state", returnedState));
    }
}
