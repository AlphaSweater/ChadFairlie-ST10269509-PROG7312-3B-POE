using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyLocalGov.com.Services.Interfaces;
using MyLocalGov.com.ViewModels.Issues;
using System.Security.Claims;

namespace MyLocalGov.com.Controllers
{
	// =================================================================================================
	// IssuesController
	// -------------------------------------------------------------------------------------------------
	// Purpose:
	//   Handles user-submitted municipal issues (incidents / complaints / service requests).
	//
	// Responsibilities:
	//   - Display the "Report Issue" form (GET).
	//   - Accept and validate posted issue data (POST).
	//   - Delegate persistence + attachment handling to IIssueService.
	//
	// Notes:
	//   - Currently redirects Index() to the Dashboard (no list view implemented here yet).
	//   - Categories are static for now; if they become data-driven, replace GetCategories().
	// =================================================================================================
	[Authorize]
	public class IssuesController : Controller
	{
		private readonly IIssueService _issueService;

		// Static, reusable category list (avoids reallocation on every request).
		private static readonly IReadOnlyList<SelectListItem> _categories = new List<SelectListItem>
		{
			new() { Value = "1", Text = "Sanitation" },
			new() { Value = "2", Text = "Roads" },
			new() { Value = "3", Text = "Water" },
			new() { Value = "4", Text = "Electricity" },
			new() { Value = "5", Text = "Parks" },
			new() { Value = "6", Text = "Waste" },
			new() { Value = "7", Text = "Utilities" },
			new() { Value = "8", Text = "Other" }
		};

		public IssuesController(IIssueService issueService)
		{
			_issueService = issueService;
		}

		/// <summary>
		/// Redirects the base Issues route to the Dashboard.
		/// (Placeholder: add a "My Issues" listing endpoint later.)
		/// </summary>
		public IActionResult Index() => RedirectToAction("Index", "Dashboard");

		/// <summary>
		/// Renders the issue reporting form.
		/// </summary>
		[HttpGet]
		public IActionResult ReportIssue()
		{
			var vm = new IssueViewModel
			{
				Categories = GetCategories()
			};
			return View("ReportIssue", vm);
		}

		/// <summary>
		/// Handles submission of a new issue.
		/// - Validates model state.
		/// - Uses authenticated user ID.
		/// - Delegates persistence to the issue service.
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateIssue(IssueViewModel vm, CancellationToken ct)
		{
			if (!ModelState.IsValid)
				return View("ReportIssue", vm);

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrWhiteSpace(userId))
			{
				// Very unlikely (Authorize attribute), but defensive.
				ModelState.AddModelError(string.Empty, "Unable to resolve current user.");
				return View("ReportIssue", vm);
			}

			// Service handles persistence + attachments. Returned ID not yet used; could route to details later.
			await _issueService.SubmitAsync(vm, userId, ct);

			TempData["ReportSubmitted"] = true;
			return RedirectToAction(nameof(Index));
		}

		/// <summary>
		/// Returns a static category list (replace with data store / config if needed).
		/// </summary>
		private static IEnumerable<SelectListItem> GetCategories() => _categories;
	}
}