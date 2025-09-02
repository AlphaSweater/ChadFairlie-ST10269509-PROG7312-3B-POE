using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyLocalGov.com.Controllers
{
	// ============================================================================
	// ReportsController
	// ----------------------------------------------------------------------------
	// Manages user reports (e.g., issues, incidents, complaints).
	// Allows users to create, view, and track their own reports.
	// ----------------------------------------------------------------------------
	// Typical Actions:
	// - Create()     : Display form to submit a new report
	// - MyReports()  : List all reports created by the current user
	// - Details(id)  : View details for a specific report
	// ============================================================================
	[Authorize]
	public class ReportsController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}