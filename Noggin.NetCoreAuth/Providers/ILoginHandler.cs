using Microsoft.AspNetCore.Mvc;
using Noggin.NetCoreAuth.Model;

namespace Noggin.NetCoreAuth.Providers
{
    public interface ILoginHandler
    {
        ActionResult SuccessfulLoginFrom(string provider, UserInformation user);
        ActionResult FailedLoginFrom(string provider, UserInformation user);
    }
}