using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Noggin.NetCoreAuth.Providers;
using Noggin.NetCoreAuth.Model;
using System.Security.Authentication;
using Noggin.NetCoreAuth.Exceptions;

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
            var redirectSettings = await authProvider.GenerateStartRequestUrl(Request.Host.Value, Request.IsHttps);

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
                user = await authProvider.AuthenticateUser(Request.Query, secret, null);
            }
            catch(AuthenticationException e)
            {
                return _loginHandler.FailedLoginFrom(provider, null, HttpContext);
            }

            // todo: Do we need to check for user info, perhaps each provider should deal with this (** IMPORTANT **)
            var loginSuccess = !string.IsNullOrEmpty(user.Id); 

            return (loginSuccess)
                ? _loginHandler.SuccessfulLoginFrom(provider, user, HttpContext)
                : _loginHandler.FailedLoginFrom(provider, user, HttpContext);
        }
    }
}