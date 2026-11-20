using Flurl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Mvc;
using Noggin.NetCoreAuth.Providers;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Noggin.NetCoreAuth.Tests.Mvc;

public class NogginNetCoreAuthControllerTests
{
    private const string ProviderName = "fake";

    private readonly FakeProvider _provider = new();
    private readonly ILoginHandler _loginHandler = Substitute.For<ILoginHandler>();
    private readonly TestSession _session = new();

    public NogginNetCoreAuthControllerTests()
    {
        _loginHandler.SuccessfulLoginFrom(default!, default!, default!).ReturnsForAnyArgs(new OkResult());
        _loginHandler.FailedLoginFrom(default!, default!, default!).ReturnsForAnyArgs(new UnauthorizedResult());
    }

    [Fact]
    public async Task RedirectToProviderStoresStateInSessionAndSendsItToProvider()
    {
        var controller = CreateController();

        var result = await controller.RedirectToProvider(ProviderName);

        var redirect = Assert.IsType<RedirectResult>(result);
        var sessionState = _session.GetString(OAuthState.SessionKey);
        Assert.False(string.IsNullOrEmpty(sessionState));
        Assert.Equal(sessionState, new Url(redirect.Url).QueryParams.FirstOrDefault("state"));
    }

    [Fact]
    public async Task RedirectToProviderCreatesNewStateEachTime()
    {
        await CreateController().RedirectToProvider(ProviderName);
        var firstState = _session.GetString(OAuthState.SessionKey);

        await CreateController().RedirectToProvider(ProviderName);
        var secondState = _session.GetString(OAuthState.SessionKey);

        Assert.NotEqual(firstState, secondState);
    }

    [Fact]
    public async Task ProviderCallbackWithMatchingStateLogsUserIn()
    {
        _session.SetString(OAuthState.SessionKey, "the-state");
        var controller = CreateController("?code=abc&state=the-state");

        var result = await controller.ProviderCallback(ProviderName);

        Assert.IsType<OkResult>(result);
        Assert.True(_provider.AuthenticateUserCalled);
        await _loginHandler.ReceivedWithAnyArgs(1).SuccessfulLoginFrom(default!, default!, default!);
    }

    [Fact]
    public async Task ProviderCallbackWithWrongStateFailsWithoutUsingCode()
    {
        _session.SetString(OAuthState.SessionKey, "the-state");
        var controller = CreateController("?code=attackers-code&state=attackers-state");

        var result = await controller.ProviderCallback(ProviderName);

        AssertFailedWithoutUsingCode(result);
    }

    [Fact]
    public async Task ProviderCallbackWithNoStateReturnedFailsWithoutUsingCode()
    {
        _session.SetString(OAuthState.SessionKey, "the-state");
        var controller = CreateController("?code=attackers-code");

        var result = await controller.ProviderCallback(ProviderName);

        AssertFailedWithoutUsingCode(result);
    }

    [Fact]
    public async Task ProviderCallbackWhenLoginNotStartedInThisSessionFailsWithoutUsingCode()
    {
        // Login CSRF: the victim's browser is sent to a callback URL the attacker started
        var controller = CreateController("?code=attackers-code&state=attackers-state");

        var result = await controller.ProviderCallback(ProviderName);

        AssertFailedWithoutUsingCode(result);
    }

    [Fact]
    public async Task ProviderCallbackStateCannotBeReused()
    {
        _session.SetString(OAuthState.SessionKey, "the-state");
        await CreateController("?code=abc&state=the-state").ProviderCallback(ProviderName);
        _provider.AuthenticateUserCalled = false;
        _loginHandler.ClearReceivedCalls();

        var result = await CreateController("?code=abc&state=the-state").ProviderCallback(ProviderName);

        Assert.Null(_session.GetString(OAuthState.SessionKey));
        AssertFailedWithoutUsingCode(result);
    }

    private void AssertFailedWithoutUsingCode(IActionResult result)
    {
        Assert.IsType<UnauthorizedResult>(result);
        Assert.False(_provider.AuthenticateUserCalled);
        _loginHandler.ReceivedWithAnyArgs(1).FailedLoginFrom(default!, default!, default!);
        _loginHandler.DidNotReceiveWithAnyArgs().SuccessfulLoginFrom(default!, default!, default!);
    }

    private NogginNetCoreAuthController CreateController(string queryString = "")
    {
        var providerFactory = Substitute.For<IProviderFactory>();
        providerFactory.Get(ProviderName).Returns(_provider);

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<ISessionFeature>(new SessionFeature { Session = _session });
        httpContext.Request.QueryString = new QueryString(string.IsNullOrEmpty(queryString) ? null : queryString);

        return new NogginNetCoreAuthController(_loginHandler, providerFactory)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private class FakeProvider : Provider
    {
        public FakeProvider()
            : base(new ProviderConfig { Name = ProviderName, Api = new ApiConfig { PublicKey = "public", PrivateKey = "private" } },
                  "auth/{provider}", "auth/callback/{provider}", NullLogger.Instance)
        {
        }

        public bool AuthenticateUserCalled { get; set; }

        internal override Task<UserInformation> AuthenticateUser(HttpRequest request)
        {
            AuthenticateUserCalled = true;
            return Task.FromResult(new UserInformation { Id = "user-id" });
        }

        internal override Task<string> GenerateStartRequestUrl(HttpRequest request, string state)
        {
            return Task.FromResult($"https://provider.example/authorize?state={state}");
        }
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
