using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Noggin.NetCoreAuth.Model;
using System;

namespace Noggin.NetCoreAuth.Providers
{
    public interface ILoginHandler
    {
        /// <summary>
        /// Your implementation of this will be called when the user has successfuly authenticated with their provider.
        /// </summary>
        /// <param name="provider">The name of the provider they authenticated with.</param>
        /// <param name="user">Information from the chosen provider about the user.</param>
        /// <param name="httpContext">The http context of the login controller.</param>
        /// <returns>You will normally want to return a redirect result to the page you want the user to go to after logging in.</returns>
        ActionResult SuccessfulLoginFrom(string provider, UserInformation user, HttpContext httpContext);

        /// <summary>
        /// Your implementation of this will be called if the users fails to authenticate for any reason.
        /// </summary>
        /// <param name="provider">The name of the provider they tried to authenticated with.</param>
        /// <param name="user">This will probably be blank, but if the authentication failed late in the process it may have some information about the user.</param>
        /// <param name="httpContext">The http context of the login controller.</param>
        /// <param name="failInfo">More information about why authenticating with the provider failed</param>
        /// <returns>You will normally want to return a redirect result to the page the user started on and show them an error message.</returns>
        ActionResult FailedLoginFrom(string provider, UserInformation user, HttpContext httpContext, AuthenticationFailInformation failInfo);
    }
}