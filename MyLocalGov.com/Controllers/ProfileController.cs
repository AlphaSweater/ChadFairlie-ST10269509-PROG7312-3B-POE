using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyLocalGov.com.Controllers
{
	// ============================================================================
	// ProfileController
	// ----------------------------------------------------------------------------
	// Handles user profile information (personal details, contact info, avatar, user preferences).
	// Separate from authentication to keep responsibilities clear.
	// ----------------------------------------------------------------------------
	// Typical Actions:
	// - Index()     : Display the user’s profile page
	// - Edit()      : Edit profile details (name, email, picture, etc.)
	// ============================================================================
	[Authorize]
	public class ProfileController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}