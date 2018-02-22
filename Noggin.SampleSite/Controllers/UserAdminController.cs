using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Noggin.SampleSite.Data;
using System.Linq;
using Noggin.SampleSite.ViewModels.UserAdmin;
using Microsoft.EntityFrameworkCore;
using System.Security.Authentication;
using System;

namespace Noggin.SampleSite.Controllers
{
	[Authorize]
	public class UserAdminController : Controller
	{
		private readonly ISimpleDbContext _dbContext;

		public UserAdminController(ISimpleDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public IActionResult Index()
		{
			var users = _dbContext.Users
				.Include(u => u.AuthAccounts)
				//.OrderBy(u => u.Registered)
				.Take(50);

			var userId = User.Claims.FindIntClaimValue("UserId");
			var currentUser = (userId != null)
				? users.FirstOrDefault(u => u.Id == userId)
				: null;

			if (currentUser == null)
			{
				throw new AuthenticationException("Failed to get valid user details for logged in user");
			}

			var viewModel = new IndexViewModel
            {
				AllUsers = users.Select(u => new UserViewModel(u)).ToList(),
				User = new UserViewModel(currentUser)
			};

			return View(viewModel);
		}
	}
}