using Flurl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Session;
using Noggin.NetCoreAuth.Mvc;
using Noggin.NetCoreAuth.Providers;
using NSubstitute;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Noggin.NetCoreAuth.Tests.Mvc;

/// <summary>
/// The controller stores the OAuth state in session and gives it back to the provider.
/// (The state itself is created and checked by the Provider base class, see ProviderStateTests.)
/// </summary>
public class NogginNetCoreAuthControllerTests
{
    private readonly FakeProvider _provider = new();
    private readonly ILoginHandler _loginHandler = Substitute.For<ILoginHandler>();
    private readonly TestSession _session = new();

    public NogginNetCoreAuthControllerTests()
    {
        _loginHandler.SuccessfulLoginFrom(default!, default!, default!).ReturnsForAnyArgs(new OkResult());
        _loginHandler.FailedLoginFrom(default!, default!, default!).ReturnsForAnyArgs(new UnauthorizedResult());
    }

    private NogginNetCoreAuthController CreateController(string queryString = "")
    {
        var providerFactory = Substitute.For<IProviderFactory>();
        providerFactory.Get(FakeProvider.ProviderName).Returns(_provider);

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<ISessionFeature>(new SessionFeature { Session = _session });
        httpContext.Request.QueryString = new QueryString(string.IsNullOrEmpty(queryString) ? null : queryString);

        return new NogginNetCoreAuthController(_loginHandler, providerFactory)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public async Task ProviderCallbackStateCannotBeReused()
    {
        _session.SetString(NogginNetCoreAuthController.StateSessionKey, "the-state");
        await CreateController("?code=abc&state=the-state").ProviderCallback(FakeProvider.ProviderName);
        _provider.ExchangeCodeForUserCalled = false;

        var result = await CreateController("?code=abc&state=the-state").ProviderCallback(FakeProvider.ProviderName);

        Assert.Null(_session.GetString(NogginNetCoreAuthController.StateSessionKey));
        Assert.IsType<UnauthorizedResult>(result);
        Assert.False(_provider.ExchangeCodeForUserCalled);
    }

    [Fact]
    public async Task ProviderCallbackWhenLoginNotStartedInThisSessionCallsFailedLogin()
    {
        // Login CSRF: the victim's browser is sent to a callback URL the attacker started
        var result = await CreateController("?code=attackers-code&state=attackers-state").ProviderCallback(FakeProvider.ProviderName);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.False(_provider.ExchangeCodeForUserCalled);
        await _loginHandler.DidNotReceiveWithAnyArgs().SuccessfulLoginFrom(default!, default!, default!);
    }

    [Fact]
    public async Task ProviderCallbackWithStateFromSessionLogsUserIn()
    {
        _session.SetString(NogginNetCoreAuthController.StateSessionKey, "the-state");

        var result = await CreateController("?code=abc&state=the-state").ProviderCallback(FakeProvider.ProviderName);

        Assert.IsType<OkResult>(result);
        await _loginHandler.ReceivedWithAnyArgs(1).SuccessfulLoginFrom(default!, default!, default!);
    }

    [Fact]
    public async Task RedirectToProviderStoresStateInSession()
    {
        var result = await CreateController().RedirectToProvider(FakeProvider.ProviderName);

        var redirect = Assert.IsType<RedirectResult>(result);
        var sessionState = _session.GetString(NogginNetCoreAuthController.StateSessionKey);
        Assert.False(string.IsNullOrEmpty(sessionState));
        Assert.Equal(sessionState, new Url(redirect.Url).QueryParams.FirstOrDefault("state"));
    }

    private class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id => "test-session";
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }
}
