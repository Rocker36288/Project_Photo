using Microsoft.AspNetCore.Mvc;

namespace Project_Photo.Areas.PhotographerBooking.Controllers
{
    [Area("PhotographerBooking")]
    public class PhotographerHomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        //public IActionResult OnSelve()
        //{
        //    return View();
        //}

        //public IActionResult OffSelve()
        //{
        //    return View();
        //}
    }
}
