using RestSharp;

namespace Noggin.NetCoreAuth.Providers
{
    public interface IRestClientFactory
    {
        IRestClient Create(string baseUrl);
    }
}
