using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyLocalGov.com.Controllers
{
	// ============================================================================
	// EventsController
	// ----------------------------------------------------------------------------
	// Handles community events and announcements.
	// Provides users with a list of upcoming/local events.
	// ----------------------------------------------------------------------------
	// Typical Actions:
	// - Index()     : Show list of events & announcements
	// - Details(id) : View details for a specific event/announcement
	// ============================================================================
	[Authorize]
	public class EventsController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}