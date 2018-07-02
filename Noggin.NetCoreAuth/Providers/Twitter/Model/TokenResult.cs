using System.Collections.Generic;

namespace Noggin.NetCoreAuth.Providers.Twitter.Model
{
    internal class TokenResult
    {
        public string OauthToken { get; set; }

        public string OauthTokenSecret { get; set; }

        public List<ErrorResult> Errors { get; set; }
    }
}