using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyLocalGov.com.Controllers
{
	// ============================================================================
	// RequestsController
	// ----------------------------------------------------------------------------
	// Manages service requests submitted by users.
	// Focuses on tracking request status and viewing request history.
	// ----------------------------------------------------------------------------
	// Typical Actions:
	// - Index()     : Show list of all requests for the current user
	// - Details(id) : Show detailed status for a specific request
	// ============================================================================
	[Authorize]
	public class RequestsController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}