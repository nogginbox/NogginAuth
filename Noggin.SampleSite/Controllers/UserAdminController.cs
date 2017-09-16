using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Noggin.SampleSite.Data;
using System.Linq;

namespace Noggin.SampleSite.Controllers
{
    [Authorize]
    public class UserAdminController : Controller
    {
        private readonly ISimpleDbContext _dbContext;

        public UserAdminController(ISimpleDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        public IActionResult Index()
        {
            var users = _dbContext.Users.Take(50);
            return Content("It worked");
        }
    }
}