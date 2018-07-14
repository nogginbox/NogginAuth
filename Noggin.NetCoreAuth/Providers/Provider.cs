using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Model;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Providers
{
	public abstract class Provider
    {
        protected Provider(ProviderConfig config, string defaultRedirectTemplate, string defaultCallbackTemplate)
        {
            config.CheckIsValid();
            Name = config.Name;

			CallbackTemplate = CreateTemplate(Name, config.CallbackTemplate, defaultCallbackTemplate);
            RedirectTemplate = CreateTemplate(Name, config.RedirectTemplate, defaultRedirectTemplate);
			CallbacksUseHttps = config.CallbackIsHttps;
        }

        public string Name { get; }
		public string CallbackTemplate { get; }
		
		/// <summary>
		/// For callbacks use Https.
		/// [null: use settings from request, false: always use HTTP, true: always use HTTPS]
		/// </summary>
		public bool? CallbacksUseHttps { get; }
        public string RedirectTemplate { get; }

        internal abstract Task<UserInformation> AuthenticateUser(HttpRequest request, string state);
        internal abstract Task<(string url, string secret)> GenerateStartRequestUrl(HttpRequest request);

        private static string CreateTemplate(string providerName, string template1, string templateTemplate)
        {
            if (!string.IsNullOrEmpty(template1)) return template1;

            return templateTemplate.Replace("{provider}", providerName.ToLower());
        }

		protected string CreateCallbackUrl(HttpRequest request)
		{
			return CreateUrl(CallbacksUseHttps ?? request.IsHttps, request.Host.Value, CallbackTemplate);
		}

		private static string CreateUrl(bool isHttps, string host, string template)
		{
			var protocal = isHttps ? "https" : "http";
			var url = $"{protocal}://{host}/{template}";
			return url;
		}
    }
}