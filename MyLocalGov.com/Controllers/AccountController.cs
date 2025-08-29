using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyLocalGov.com.Models;
using MyLocalGov.com.ViewModels;
using MyLocalGov.com.Repositories;
using MyLocalGov.com.Repositories.Interfaces;

namespace MyLocalGov.com.Controllers
{
	public class AccountController : Controller
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly SignInManager<IdentityUser> _signInManager;
		private readonly IUnitOfWork _unitOfWork;

		public AccountController(
			UserManager<IdentityUser> userManager,
			SignInManager<IdentityUser> signInManager,
			IUnitOfWork unitOfWork)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_unitOfWork = unitOfWork;
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
				// Redirect to Index with register form visible and email pre-filled
				return RedirectToAction("Index", "Account", new { showForm = "register", email = model.Email });
			}

			var user = new IdentityUser { UserName = model.Email, Email = model.Email };
			var result = await _userManager.CreateAsync(user, model.Password);

			if (result.Succeeded)
			{
				// Assign "Citizen" role by default
				await _userManager.AddToRoleAsync(user, "Citizen");

				// Use UnitOfWork and repository for profile creation
				var profile = new UserProfileModel(model, user);
				await _unitOfWork.UserProfiles.CreateProfileForUserAsync(profile);
				await _unitOfWork.SaveAsync();

				await _signInManager.SignInAsync(user, isPersistent: false);
				return RedirectToAction("Index", "Account");
			}

			// Redirect to Index with register form visible and email pre-filled
			return RedirectToAction("Index", "Account", new { showForm = "register", email = model.Email, registerError = result.Errors.FirstOrDefault()?.Description });
		}

		[HttpGet]
		public IActionResult Login() => RedirectToAction("Index", new { showForm = "login" });

		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel model)
		{
			if (!ModelState.IsValid)
			{
				// Redirect to Index with login form visible and email pre-filled
				return RedirectToAction("Index", "Account", new { showForm = "login", email = model.Email });
			}

			var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
			if (result.Succeeded)
				return RedirectToAction("Dashboard", "Home");

			// Redirect to Index with login form visible and email pre-filled
			return RedirectToAction("Index", "Account", new { showForm = "login", email = model.Email, loginError = "Invalid login attempt." });
		}

		[HttpPost]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return RedirectToAction("Index", "Home");
		}
	}
}