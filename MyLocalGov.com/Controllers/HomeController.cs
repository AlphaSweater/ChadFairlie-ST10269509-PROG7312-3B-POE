using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyLocalGov.com.Data;
using MyLocalGov.com.Models;
using MyLocalGov.com.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace MyLocalGov.com.Controllers
{
	[Authorize]
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly MyLocalGovDbContext _dbContext; // Assume ApplicationDbContext is your DB context class

		public HomeController(ILogger<HomeController> logger, MyLocalGovDbContext dbContext)
		{
			_logger = logger;
			_dbContext = dbContext;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		public IActionResult test()
		{
			return View();
		}

		[AllowAnonymous]
		public IActionResult AnimationsDemo()
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