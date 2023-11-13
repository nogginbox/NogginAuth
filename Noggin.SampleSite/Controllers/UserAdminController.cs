using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Extensions;
using Noggin.SampleSite.Data;
using Noggin.SampleSite.ViewModels.UserAdmin;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;

namespace Noggin.SampleSite.Controllers;

[Authorize]
public class UserAdminController : Controller
{
	private readonly IList<string> _allProviders;
	private readonly ISimpleDbContext _dbContext;

	public UserAdminController(IOptions<AuthConfigSection> authConfig, ISimpleDbContext dbContext)
	{
		_allProviders = authConfig.Value.Providers.Select(p => p.Name).ToList();
		_dbContext = dbContext;
	}

	public IActionResult Index()
	{
		var users = _dbContext.Users
			.Include(u => u.AuthAccounts)
			.OrderByDescending(u => u.LastLoggedIn)
			.Take(25);

		var userId = User.Claims.FindIntClaimValue("UserId");
		var currentUser = ((userId != null)
			? users.FirstOrDefault(u => u.Id == userId)
			: null)
			?? throw new AuthenticationException("Failed to get valid user details for logged in user");
        
		var unlinkedProviders = _allProviders.Except(currentUser.AuthAccounts.Select(a => a.Provider));

		var viewModel = new IndexViewModel
        {
			AllUsers = users.Select(u => 
                new UserViewModel(GetDisplayUserName(u, u.Id == currentUser.Id), u)
            ).ToList(),
			User = new UserViewModel(currentUser.Name, currentUser),
			UnlinkedProviders = unlinkedProviders
		};

		return View(viewModel);
	}

    /// <summary>
    /// Gets a user name to show in the admin, for privacy purposes only shows current user's full name
    /// </summary>
    private static string GetDisplayUserName(User user, bool showRealName)
    {
        if(showRealName)
        {
            return user.Name;
        }

        if(!(user.Name?.Length > 0))
        {
            return "*******";
        }

        return $"{user.Name[..1]}******";
    }
}