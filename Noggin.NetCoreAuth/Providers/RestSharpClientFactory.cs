using RestSharp;

namespace Noggin.NetCoreAuth.Providers
{
    internal class RestSharpClientFactory : IRestClientFactory
    {
        public IRestClient Create(string baseUrl)
        {
            return new RestClient(baseUrl);
        }
    }
}