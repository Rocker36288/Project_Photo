using Microsoft.AspNetCore.Mvc;

namespace Project_Photo.Areas.buyer.Controllers
{
    public class ShoppingHomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
