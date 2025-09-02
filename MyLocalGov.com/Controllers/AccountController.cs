using Microsoft.AspNetCore.Mvc;
using MyLocalGov.com.ViewModels;
using MyLocalGov.com.Services.Interfaces;

namespace MyLocalGov.com.Controllers
{
	// ============================================================================
	// AccountController
	// ----------------------------------------------------------------------------
	// Handles all user authentication and identity-related tasks.
	// Focus: Login, registration, logout, password management.
	// ----------------------------------------------------------------------------
	// Typical Actions:
	// - Landing()       : Public landing/homepage
	// - Login()         : Show login form / process login
	// - Register()      : Show registration form / create account
	// - Logout()        : Log user out and redirect
	// - ForgotPassword(): Request password reset
	// - ResetPassword() : Handle password reset form
	// ============================================================================
	public class AccountController : Controller
	{
		// =============================================
		// Dependencies
		// =============================================
		private readonly IAuthService _authService;

		/// <summary>
		/// Constructor: injects authentication service.
		/// </summary>
		public AccountController(IAuthService authService)
		{
			_authService = authService;
		}

		// =============================================
		// Page Endpoints
		// =============================================

		/// <summary>
		/// Main authentication page (shows login/register forms).
		/// </summary>
		[HttpGet]
		public IActionResult Index() => View("~/Views/Auth/Index.cshtml");

		/// <summary>
		/// Redirect to registration form.
		/// </summary>
		[HttpGet]
		public IActionResult Register() => RedirectToAction("Index", new { showForm = "register" });

		/// <summary>
		/// Redirect to login form.
		/// </summary>
		[HttpGet]
		public IActionResult Login() => RedirectToAction("Index", new { showForm = "login" });

		// =============================================
		// Registration Logic
		// =============================================

		/// <summary>
		/// Handles user registration.
		/// </summary>
		[HttpPost]
		public async Task<IActionResult> Register(RegisterViewModel model)
		{
			// Validate input
			if (!ModelState.IsValid)
			{
				return RedirectToAction("Index", "Account", new { showForm = "register", email = model.Email });
			}

			// Attempt registration via service
			var result = await _authService.RegisterAsync(model);

			if (result.Succeeded)
				// Registration successful, redirect to login page
				return RedirectToAction("Login", "Account");

			// Registration failed, show error
			return RedirectToAction("Index", "Account", new
			{
				showForm = "register",
				email = model.Email,
				registerError = result.Errors.FirstOrDefault()?.Description
			});
		}

		// =============================================
		// Login Logic
		// =============================================

		/// <summary>
		/// Handles user login.
		/// </summary>
		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel model)
		{
			// Validate input
			if (!ModelState.IsValid)
			{
				return RedirectToAction("Index", "Account", new { showForm = "login", email = model.Email });
			}

			// Attempt login via service
			var result = await _authService.LoginAsync(model);

			if (result.Succeeded)
				// Login successful, redirect to dashboard
				return RedirectToAction("Index", "Dashboard");

			// Login failed, show error
			return RedirectToAction("Index", "Account", new
			{
				showForm = "login",
				email = model.Email,
				loginError = "Incorrect email or password used."
			});
		}

		// =============================================
		// Logout Logic
		// =============================================

		/// <summary>
		/// Handles user logout.
		/// </summary>
		[HttpPost]
		public async Task<IActionResult> Logout()
		{
			await _authService.LogoutAsync();
			return RedirectToAction("Index", "Account");
		}
	}
}