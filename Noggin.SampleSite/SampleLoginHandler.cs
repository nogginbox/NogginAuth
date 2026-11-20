using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Noggin.NetCoreAuth.Extensions;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers;
using Noggin.SampleSite.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Noggin.SampleSite;

public class SampleLoginHandler : ILoginHandler
{
    private readonly ISimpleDbContext _dbContext;
    private readonly ILogger<SampleLoginHandler> _logger;
    private readonly ITempDataDictionaryFactory _tempDataFactory;

    public SampleLoginHandler(ISimpleDbContext dbContext, ILogger<SampleLoginHandler> logger, ITempDataDictionaryFactory tempDataFactory)
    {
        _dbContext = dbContext;
        _logger = logger;
        _tempDataFactory = tempDataFactory;
    }

	/// <summary>
	/// Create a principal to login with containing claims with info about the user
	/// </summary>
	private static ClaimsPrincipal CreatePrincipal(User user, string provider)
	{
		var claims = new List<Claim>
		{
			new Claim("UserId", user.Id.ToString()),
			new Claim("UserName", user.Name),
			new Claim("LoginProvider", provider)
		};
		var principal = new ClaimsPrincipal();
		principal.AddIdentity(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
		return principal;
	}

	public Task<IActionResult> FailedLoginFrom(string provider, AuthenticationFailInformation failInfo, HttpContext context)
    {
        // Log the details for you, but only show the user a friendly message
        _logger.LogWarning(failInfo.Exception, "Login with {Provider} failed: {Reason}", provider, failInfo.Reason);

        // TempData keeps the message for the next request, so it can be shown after the redirect (see _Layout.cshtml)
        var tempData = _tempDataFactory.GetTempData(context);
        tempData["LoginError"] = $"Sorry, logging in with {provider} didn't work. Please try again.";

        // Nothing to await so just returning a task of our result
        return Task.FromResult<IActionResult>(new RedirectToActionResult("Index", "Home", null));
    }

    public async Task<IActionResult> SuccessfulLoginFrom(string provider, UserInformation userInfo, HttpContext httpContext)
    {
		var loginUser = _dbContext.Users
			.Include(u => u.AuthAccounts)
			.FindUserWithProvider(userInfo.Id, provider);

		// User is already logged in
		if (httpContext.User.Identity?.IsAuthenticated == true)
		{
			var loggedInUserProvider = httpContext.User.Claims.FindStringClaimValue("LoginProvider");
			if(loggedInUserProvider == provider)
			{
				// User has tried to log in twice with same provider
				return new RedirectToActionResult("Index", "Home", null);
			}

			// Modify logged in user with info from new provider login

			var loggedInUserId = httpContext.User.Claims.FindIntClaimValue("UserId");
			if(loginUser != null && loggedInUserId == loginUser.Id)
			{
				// User has tried to log in twice with two of their registered providers
				return new RedirectToActionResult("Index", "Home", null);
			}

			var loggedInUser = _dbContext.Users
				.Include(u => u.AuthAccounts)
				.FirstOrDefault(u => u.Id == loggedInUserId)
				?? throw new AuthenticationException("Failed to get valid user details for logged in user");

			loggedInUser.LastLoggedIn = DateTime.Now;

			// Add this auth account to existing user
			if (loginUser == null)
			{
				// Add new auth account to logged in user
				loggedInUser.AuthAccounts.Add(new UserAuthAccount { Id = userInfo.Id, Provider = provider, UserName = userInfo.UserName });
			}
			else
			{
				// Merge accounts (in this implementation the only useful stuff is the auth accounts)
				var mergeAuthAccounts = loginUser.AuthAccounts.Except(loggedInUser.AuthAccounts).ToList();
				loginUser.AuthAccounts.Clear();
				foreach (var authAccount in mergeAuthAccounts)
				{
					loggedInUser.AuthAccounts.Add(authAccount);
				}

				// Remove old user we've grabbed stuff from (one from latest login)
				_dbContext.Users.Remove(loginUser);
			}
				
			await _dbContext.SaveChangesAsync();
			return new RedirectToActionResult("Index", "Home", null);
		}
		// New login
		else if (loginUser == null)
        {
            loginUser = new User { Name = userInfo.Name };
            loginUser.AuthAccounts.Add(new UserAuthAccount { Id = userInfo.Id, Provider = provider, UserName = userInfo.UserName });
            _dbContext.Users.Add(loginUser);
            
        }

        loginUser.LastLoggedIn = DateTime.Now;
        await _dbContext.SaveChangesAsync();

        SignUserIn(loginUser, provider, httpContext);
        return new RedirectToActionResult("Index", "Home", null);
    }

	private static void SignUserIn(User user, string provider, HttpContext httpContext)
	{
		// Using Cookie Authentication without ASP.NET Core Identity
		// https://docs.microsoft.com/en-us/aspnet/core/security/authorization/
		// https://docs.microsoft.com/en-us/aspnet/core/security/authentication/cookie?tabs=aspnetcore2x

		// Todo:
		// * Is it okay to create an empty principal in this simple case
		// * Is this simple policy okay, create as constant in class perhaps
		var principal = CreatePrincipal(user, provider);
		var policy = new OperationAuthorizationRequirement { Name = "All" };

		// https://stackoverflow.com/questions/46057109/why-doesnt-my-cookie-authentication-work-in-asp-net-core
		httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
	}
}
