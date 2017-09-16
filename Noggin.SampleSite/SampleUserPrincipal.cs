using Noggin.SampleSite.Data;
using System.Security.Claims;

namespace Noggin.SampleSite
{
    public class SampleUserPrincipal : ClaimsPrincipal
    {
        public SampleUserPrincipal(User user) : base()
        {
            User = user;
            AddIdentity(new ClaimsIdentity("NogginSampleCookieScheme"));
        }

        public User User { get; }
    }
}
