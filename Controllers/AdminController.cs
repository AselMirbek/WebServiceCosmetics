using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebServiceCosmetics.Controllers
{
    [Authorize(Roles = "Администратор")]
    public class AdminController : Controller
    {
        public IActionResult Index() => View();

    }
}
