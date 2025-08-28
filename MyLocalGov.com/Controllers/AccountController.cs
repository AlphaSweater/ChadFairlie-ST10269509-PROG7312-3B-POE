using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyLocalGov.com.Models;
using MyLocalGov.com.ViewModels;

namespace MyLocalGov.com.Controllers
{
	public class AccountController : Controller
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;

		public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
		{
			_userManager = userManager;
			_signInManager = signInManager;
		}

		[HttpGet]
		public IActionResult Register() => View();

		[HttpPost]
		public async Task<IActionResult> Register(RegisterViewModel model)
		{
			if (!ModelState.IsValid)
			{
				// Redirect to Index with register form visible and email pre-filled
				return RedirectToAction("Index", "Home", new { showForm = "register", email = model.Email });
			}

			var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
			var result = await _userManager.CreateAsync(user, model.Password);

			if (result.Succeeded)
			{
				// Assign "User" role by default
				await _userManager.AddToRoleAsync(user, "User");

				await _signInManager.SignInAsync(user, isPersistent: false);
				return RedirectToAction("Index", "Home");
			}

			// Redirect to Index with register form visible and email pre-filled
			return RedirectToAction("Index", "Home", new { showForm = "register", email = model.Email, error = result.Errors.FirstOrDefault()?.Description });
		}

		[HttpGet]
		public IActionResult Login() => View();

		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel model)
		{
			if (!ModelState.IsValid)
			{
				// Redirect to Index with login form visible and email pre-filled
				return RedirectToAction("Index", "Home", new { showForm = "login", email = model.Email });
			}

			var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
			if (result.Succeeded)
				return RedirectToAction("Dashboard", "Home");

			// Redirect to Index with login form visible and email pre-filled
			return RedirectToAction("Index", "Home", new { showForm = "login", email = model.Email, error = "Invalid login attempt." });
		}

		[HttpPost]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return RedirectToAction("Index", "Home");
		}
	}
}