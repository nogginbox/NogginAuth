using Noggin.NetCoreAuth.Providers;
using NSubstitute;
using RestSharp;

namespace Noggin.NetCoreAuth.Tests;

public abstract class BaseForProviderTests
{
    /// <summary>
    /// Creates a RestClientFactory that will Create the returned RestClient
    /// </summary>
    protected (IRestClientFactory, IRestClient) CreateRestClientAndFactory(IRestClient optionalRestClientReturned = null)
    {
        var client = optionalRestClientReturned ?? Substitute.For<IRestClient>();
        var factory = Substitute.For<IRestClientFactory>();
        factory.Create(Arg.Any<string>()).Returns(client);

        return (factory, client);
    }
}