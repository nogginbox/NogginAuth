using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers;
using Noggin.SampleSite.Data;
using System.Linq;

namespace Noggin.SampleSite
{
    public class SampleLoginHandler : ILoginHandler
    {
        private readonly IAuthorizationService _authService;
        private readonly ISimpleDbContext _dbContext;

        public SampleLoginHandler(ISimpleDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public ActionResult FailedLoginFrom(string provider, UserInformation userInfo, HttpContext context)
        {
            // Todo: Set Tempdata message and display message to user
            return new RedirectToActionResult("About", "Home", new { type = "failed" });
        }

        public ActionResult SuccessfulLoginFrom(string provider, UserInformation userInfo, HttpContext context)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.AuthAccounts.Any(a => a.Provider == provider && a.Id == userInfo.Id));
            
            // Automatically create users we've not heard of before using details from their social login
            if (user == null)
            {
                user = new User { Name = userInfo.Name };
                user.AuthAccounts.Add(new UserAuthAccount { Id = userInfo.Id, Provider = provider, UserName = userInfo.UserName });
                _dbContext.Users.Add(user);
                _dbContext.SaveChanges();
            }

            // Using Cookie Authentication without ASP.NET Core Identity
            // https://docs.microsoft.com/en-us/aspnet/core/security/authorization/
            // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/cookie?tabs=aspnetcore2x

            // Todo:
            // * Is it okay to create an empty principal in this simple case
            // * Is this simple policy okay, create as constant in class perhaps
            var principal = new SampleUserPrincipal(user);
            var policy = new OperationAuthorizationRequirement { Name = "All" };

            // https://stackoverflow.com/questions/46057109/why-doesnt-my-cookie-authentication-work-in-asp-net-core
            context.SignInAsync("NogginSampleCookieScheme", principal);

            return new RedirectToActionResult("Index", "Home", null);
        }
    }
}
