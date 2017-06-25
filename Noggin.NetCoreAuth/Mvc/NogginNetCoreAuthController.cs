using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Noggin.NetCoreAuth.Providers.Twitter;
using Microsoft.Extensions.Options;
using System.Linq;
using Noggin.NetCoreAuth.Config;

namespace Noggin.NetCoreAuth.Mvc
{
    public class NogginNetCoreAuthController : Controller
    {
        private readonly TwitterProvider _twitterProvider;

        public NogginNetCoreAuthController(IOptions<AuthConfigSection> config)
        {
            var providerConfig = config.Value.Providers.FirstOrDefault(p => string.Equals(p.Name, "twitter", StringComparison.OrdinalIgnoreCase));
            _twitterProvider = new TwitterProvider(providerConfig);
        }

        public async Task<IActionResult> RedirectToProvider(string provider)
        {
            // Todo: Don't send secret bit
            var redirectSettings = await _twitterProvider.GenerateStartRequestUrl();

            // Store stuff so that we know we initiated this
            // Todo: Probably can't rely on session if we want this to be flexible
            HttpContext.Session.SetString("secret", redirectSettings.secret);

            return Redirect(redirectSettings.url);
            //return Redirect("https://api.twitter.com/oauth/authenticate?oauth_token=NGAVZwAAAAAANBEDAAABXD8t_vU");
        }

        public async Task<IActionResult> ProviderCallback(string provider)
        {
            var secret = HttpContext.Session.GetString("secret");
            var user = await _twitterProvider.AuthenticateUser(Request.Query, secret, null);

            // todo: Run a provided method to check user is valid
            return Content(user.UserName);
        }
    }
}
