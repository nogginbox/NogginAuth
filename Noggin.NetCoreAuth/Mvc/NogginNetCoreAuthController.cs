using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers;
using System;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Mvc
{
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
            var redirectSettings = await authProvider.GenerateStartRequestUrl(Request);

            // Store stuff so that we know we initiated this
            // Todo: Probably can't rely on session if we want this to be flexible
            HttpContext.Session.SetString("secret", redirectSettings.secret);

            return Redirect(redirectSettings.url);
        }

        public async Task<IActionResult> ProviderCallback(string provider)
        {
            var authProvider = _providerFactory.Get(provider);

            UserInformation user;
            try
            {
                var secret = HttpContext.Session.GetString("secret");
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
}