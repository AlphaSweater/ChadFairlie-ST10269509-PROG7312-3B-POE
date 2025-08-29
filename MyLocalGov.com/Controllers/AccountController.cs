using Microsoft.AspNetCore.Mvc;
using MyLocalGov.com.ViewModels;
using MyLocalGov.com.Services.Interfaces;

namespace MyLocalGov.com.Controllers
{
	public class AccountController : Controller
	{
		private readonly IAuthService _authService;

		public AccountController(IAuthService authService)
		{
			_authService = authService;
		}

		[HttpGet]
		public IActionResult Index() => View("~/Views/Auth/Index.cshtml");

		[HttpGet]
		public IActionResult Register() => RedirectToAction("Index", new { showForm = "register" });

		[HttpPost]
		public async Task<IActionResult> Register(RegisterViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return RedirectToAction("Index", "Account", new { showForm = "register", email = model.Email });
			}

			var result = await _authService.RegisterAsync(model);

			if (result.Succeeded)
				return RedirectToAction("Login", "Account");

			return RedirectToAction("Index", "Account", new
			{
				showForm = "register",
				email = model.Email,
				registerError = result.Errors.FirstOrDefault()?.Description
			});
		}

		[HttpGet]
		public IActionResult Login() => RedirectToAction("Index", new { showForm = "login" });

		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return RedirectToAction("Index", "Account", new { showForm = "login", email = model.Email });
			}

			var result = await _authService.LoginAsync(model);

			if (result.Succeeded)
				return RedirectToAction("Dashboard", "Home");

			return RedirectToAction("Index", "Account", new
			{
				showForm = "login",
				email = model.Email,
				loginError = "Incorrect email or password used."
			});
		}

		[HttpPost]
		public async Task<IActionResult> Logout()
		{
			await _authService.LogoutAsync();
			return RedirectToAction("Index", "Account");
		}
	}
}