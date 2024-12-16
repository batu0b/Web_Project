using Microsoft.AspNetCore.Mvc;

namespace Odev.Controllers
{
    public class HomeController1 : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
