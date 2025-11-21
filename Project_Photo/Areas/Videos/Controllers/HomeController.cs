using Microsoft.AspNetCore.Mvc;

namespace Project_Photo.Areas.Videos.Controllers
{
    [Area("Videos")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
