using System.Threading.Tasks;
using Noggin.NetCoreAuth.Config;
using System;
using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Model;

namespace Noggin.NetCoreAuth.Providers
{
    public abstract class Provider
    {
        protected Provider(ProviderConfig config, string defaultRedirectTemplate, string defaultCallbackTemplate)
        {
            config.CheckIsValid();
            Name = config.Name;

            RedirectTemplate = CreateTemplate(Name, config.RedirectTemplate, defaultRedirectTemplate);
            CallbackTemplate = CreateTemplate(Name, config.CallbackTemplate, defaultCallbackTemplate);
        }

        public string Name { get; }
        public string RedirectTemplate { get; }
        public string CallbackTemplate { get; }

        internal abstract Task<UserInformation> AuthenticateUser(IQueryCollection queryStringParameters, string state, Uri callbackUri);
        internal abstract Task<(string url, string secret)> GenerateStartRequestUrl(string host, bool isHttps);

        protected static string CreateTemplate(string providerName, string template1, string templateTemplate)
        {
            if (!string.IsNullOrEmpty(template1)) return template1;

            return template1.Replace("{provider}", providerName);
        }
    }
}