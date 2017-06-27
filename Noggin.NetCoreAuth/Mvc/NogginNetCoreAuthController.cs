using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Noggin.NetCoreAuth.Providers.Twitter;
using Microsoft.Extensions.Options;
using System.Linq;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Providers;

namespace Noggin.NetCoreAuth.Mvc
{
    public class NogginNetCoreAuthController : Controller
    {
        private readonly IProviderFactory _providerFactory;

        public NogginNetCoreAuthController(IProviderFactory providerFactory)
        {
            _providerFactory = providerFactory;
        }

        public async Task<IActionResult> RedirectToProvider(string provider)
        {
            var authProvider = _providerFactory.Get(provider);

            // Todo: Don't send secret bit
            var redirectSettings = await authProvider.GenerateStartRequestUrl();

            // Store stuff so that we know we initiated this
            // Todo: Probably can't rely on session if we want this to be flexible
            HttpContext.Session.SetString("secret", redirectSettings.secret);

            return Redirect(redirectSettings.url);
        }

        public async Task<IActionResult> ProviderCallback(string provider)
        {
            var authProvider = _providerFactory.Get(provider);

            var secret = HttpContext.Session.GetString("secret");
            var user = await authProvider.AuthenticateUser(Request.Query, secret, null);

            // todo: Run a provided method to check user is valid
            return Content(user.UserName);
        }
    }
}
