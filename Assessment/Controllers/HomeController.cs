using Microsoft.AspNetCore.Mvc;

namespace Assessment.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
