using Noggin.NetCoreAuth.Providers;
using System;
using Microsoft.AspNetCore.Mvc;
using Noggin.NetCoreAuth.Model;

namespace Noggin.SampleSite
{
    public class SampleLoginHandler : ILoginHandler
    {
        public RedirectResult FailedLoginFrom(string provider, UserInformation user)
        {
            throw new NotImplementedException("FailedLoginFrom");
        }

        public RedirectResult SuccessfulLoginFrom(string provider, UserInformation user)
        {
            throw new NotImplementedException("SuccessfulLoginFrom");
        }
    }
}
