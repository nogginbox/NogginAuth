using RestSharp;
using System;

namespace Noggin.NetCoreAuth.Providers;

[Obsolete("Working to remove RestSharp from this project.")]
public interface IRestClientFactory
{
    IRestClient Create(string baseUrl);
}
