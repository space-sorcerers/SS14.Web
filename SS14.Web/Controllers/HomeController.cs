using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SS14.Web.Models;

namespace SS14.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return Redirect("https://ss14.art/privacy");
    }

    public IActionResult Contact()
    {
        return Redirect("https://ss14.art/#:~:text=%D0%9E%D1%82%D0%BC%D0%B5%D0%BD%D0%B0%20%D0%BF%D0%BE%D0%B4%D0%BF%D0%B8%D1%81%D0%BA%D0%B8-,%D0%9A%D0%9E%D0%9D%D0%A2%D0%90%D0%9A%D0%A2%D0%AB,-%D0%9F%D0%BE%D1%87%D1%82%D0%B0%3A%20support");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }

    public IActionResult MainWebsite()
    {
        return Redirect("https://ss14.art/");
    }
}
