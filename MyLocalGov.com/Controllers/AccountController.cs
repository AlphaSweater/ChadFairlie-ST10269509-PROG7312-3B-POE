using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyLocalGov.com.Data;
using MyLocalGov.com.Models;
using MyLocalGov.com.ViewModels;

namespace MyLocalGov.com.Controllers
{
	public class AccountController : Controller
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly SignInManager<IdentityUser> _signInManager;

		public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
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

			var user = new IdentityUser { UserName = model.Email, Email = model.Email };
			var result = await _userManager.CreateAsync(user, model.Password);

			if (result.Succeeded)
			{
				// Assign "Citizen" role by default
				await _userManager.AddToRoleAsync(user, "Citizen");

				// Create UserProfile for the new user
				using (var scope = HttpContext.RequestServices.CreateScope())
				{
					var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
					var profile = new UserProfileModel
					{
						Id = user.Id,
						FirstName = model.FirstName,
						LastName = model.Surname,
						Location = "",
						PreferencesJson = "{}",
						User = user
					};
					db.UserProfiles.Add(profile);
					await db.SaveChangesAsync();
				}

				await _signInManager.SignInAsync(user, isPersistent: false);
				return RedirectToAction("Index", "Home");
			}

			// Redirect to Index with register form visible and email pre-filled
			return RedirectToAction("Index", "Home", new { showForm = "register", email = model.Email, registerError = result.Errors.FirstOrDefault()?.Description });
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
			return RedirectToAction("Index", "Home", new { showForm = "login", email = model.Email, loginError = "Invalid login attempt." });
		}

		[HttpPost]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return RedirectToAction("Index", "Home");
		}
	}
}