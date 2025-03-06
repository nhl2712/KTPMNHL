using System.Diagnostics;
using ASC.Utilities;
using ASC_Web.Configuration;
using ASC_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ASC_Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private IOptions<ApplicationSetting> _settings;
        public HomeController(ILogger<HomeController> logger, IOptions<ApplicationSetting> setting)
        {
            _logger = logger;
            _settings = setting;
        }
        public HomeController(IOptions<ApplicationSetting> setting)
        {
            _settings = setting;
        }
        public IActionResult Index()
        {
            HttpContext.Session.SetSession("Test", _settings.Value);
            var settings = HttpContext.Session.GetSession<ApplicationSetting>("Test");
            ViewBag.Title = _settings.Value.ApplicationTitle;
            return View();
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
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
