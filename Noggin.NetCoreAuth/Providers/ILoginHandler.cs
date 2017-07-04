using Microsoft.AspNetCore.Mvc;
using Noggin.NetCoreAuth.Model;

namespace Noggin.NetCoreAuth.Providers
{
    public interface ILoginHandler
    {
        RedirectResult SuccessfulLoginFrom(string provider, UserInformation user);
        RedirectResult FailedLoginFrom(string provider, UserInformation user);
    }
}