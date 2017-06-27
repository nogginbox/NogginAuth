using System.Threading.Tasks;
using Noggin.NetCoreAuth.Config;
using System;
using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Model;

namespace Noggin.NetCoreAuth.Providers
{
    public abstract class Provider
    {
        protected Provider(ProviderConfig config)
        {
            config.CheckIsValid();
        }

        internal abstract Task<(string url, string secret)> GenerateStartRequestUrl();

        internal abstract Task<UserInformation> AuthenticateUser(IQueryCollection queryStringParameters, string state, Uri callbackUri);
    }
}