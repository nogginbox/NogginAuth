using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Model;
using System;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Providers.Microsoft
{
    /// <summary>
    /// Microsoft Login Provider
    /// </summary>
    /// /// <remarks>
    /// Reference: https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow
    /// </remarks>
    internal class MicrosoftProvider : Provider
    {
        public MicrosoftProvider(ProviderConfig config, string defaultRedirectTemplate, string defaultCallbackTemplate) : base(config, defaultRedirectTemplate, defaultCallbackTemplate)
        {
                
        }

        internal override Task<UserInformation> AuthenticateUser(HttpRequest request, string state)
        {
            throw new NotImplementedException();
        }

        internal override Task<(string url, string secret)> GenerateStartRequestUrl(HttpRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
