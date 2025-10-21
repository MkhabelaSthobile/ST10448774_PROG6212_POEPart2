using System.Diagnostics;
using CMCS_App.Models;
using Microsoft.AspNetCore.Mvc;

namespace CMCS_App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Redirect based on user role in a real application
            // For prototype, show role selection
            return View();
        }

        public IActionResult Login(string role)
        {
            // In a real application, this would handle actual authentication
            // For prototype, simply redirect to the appropriate dashboard
            return role?.ToLower() switch
            {
                "lecturer" => RedirectToAction("Index", "Lecturer"),
                "coordinator" => RedirectToAction("Index", "ProgrammeCoordinator"),
                "manager" => RedirectToAction("Index", "AcademicManager"),
                _ => RedirectToAction("Index")
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}