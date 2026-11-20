using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers;
using System;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Mvc;

public class NogginNetCoreAuthController : Controller
{
    private readonly ILoginHandler _loginHandler;
    private readonly IProviderFactory _providerFactory;

    public NogginNetCoreAuthController(ILoginHandler loginHandler, IProviderFactory providerFactory)
    {
        _loginHandler = loginHandler ?? throw new NogginNetCoreConfigException("A Login Handler (implementing ILoginHandler) has not been registered."); ;
        _providerFactory = providerFactory;
    }

    public async Task<IActionResult> RedirectToProvider(string provider)
    {
        var authProvider = _providerFactory.Get(provider);

        // Remember a random state so the callback can prove this browser started the login (prevents login CSRF)
        var state = OAuthState.Create();
        HttpContext.Session.SetString(OAuthState.SessionKey, state);

        var url = await authProvider.GenerateStartRequestUrl(Request, state);
        return Redirect(url);
    }

    public async Task<IActionResult> ProviderCallback(string provider)
    {
        var authProvider = _providerFactory.Get(provider);

        // A state can only be used once
        var expectedState = HttpContext.Session.GetString(OAuthState.SessionKey);
        HttpContext.Session.Remove(OAuthState.SessionKey);

        UserInformation user;
        try
        {
            // Check the state before using the code, so a callback this browser didn't start is rejected
            OAuthState.Validate(expectedState, Request.Query[OAuthState.QueryParameterName].ToString());
            user = await authProvider.AuthenticateUser(Request);
        }
        catch(Exception ex) when (!(ex is NogginNetCoreConfigException))
        {
            var failInfo = new AuthenticationFailInformation(ex);
            return await _loginHandler.FailedLoginFrom(provider, failInfo, HttpContext);
        }

        var loginSuccess = !string.IsNullOrEmpty(user.Id);

        if (loginSuccess)
        {
            return await _loginHandler.SuccessfulLoginFrom(provider, user, HttpContext);
        }
        else
        {
            // todo: Get better fail info from provider
            var failInfo = new AuthenticationFailInformation("Could not authenticate", user);
            return await _loginHandler.FailedLoginFrom(provider, failInfo, HttpContext);
        }
    }
}