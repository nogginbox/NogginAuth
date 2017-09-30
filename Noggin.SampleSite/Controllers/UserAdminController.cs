using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Noggin.SampleSite.Data;
using System.Linq;
using Noggin.SampleSite.ViewModels.UserAdmin;

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
                //.OrderBy(u => u.Registered)
                .Take(50);

            var viewModel = new IndexViewModel
            {
                Users = users.Select(u => new UserViewModel(u)).ToList()
            };

            return View(viewModel);
        }
    }
}