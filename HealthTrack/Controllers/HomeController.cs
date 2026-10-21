// =====================================================================
// FILE:    Controllers/HomeController.cs
// PURPOSE: The public pages anyone can open without signing in: the landing page
//          (Index), the privacy page and the friendly error page.
// VIEWS:   Views/Home/Index.cshtml, Views/Home/Privacy.cshtml, Views/Shared/Error.cshtml
// =====================================================================

using HealthTrack.Models;        // ErrorViewModel
using Microsoft.AspNetCore.Mvc;  // Controller, IActionResult, ResponseCache
using System.Diagnostics;        // Activity (the id of the current request)

namespace HealthTrack.Controllers
{
    // No [Authorize] attribute: these pages are public
    public class HomeController : Controller
    {
        // GET: /  (the landing page)
        public IActionResult Index()
        {
            return View();   // shows Views/Home/Index.cshtml
        }

        // GET: /Home/Privacy
        public IActionResult Privacy()
        {
            return View();   // shows Views/Home/Privacy.cshtml
        }

        // Shown when an unexpected error happens (outside development, see Program.cs)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]   // never cache an error page
        public IActionResult Error()
        {
            // Pass the request id so the problem can be found in the logs
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
