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

    /// <summary>Session key that holds the OAuth state between the redirect and the callback</summary>
    internal const string StateSessionKey = "NogginNetCoreAuth.State";

    public async Task<IActionResult> RedirectToProvider(string provider)
    {
        var authProvider = _providerFactory.Get(provider);
        var (url, secret) = await authProvider.GenerateStartRequestUrl(Request);

        // Remember the secret so the provider can check it on the callback
        HttpContext.Session.SetString(StateSessionKey, secret);

        return Redirect(url);
    }

    public async Task<IActionResult> ProviderCallback(string provider)
    {
        var authProvider = _providerFactory.Get(provider);

        // A secret can only be used once
        var secret = HttpContext.Session.GetString(StateSessionKey);
        HttpContext.Session.Remove(StateSessionKey);

        UserInformation user;
        try
        {
            user = await authProvider.AuthenticateUser(Request, secret);
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