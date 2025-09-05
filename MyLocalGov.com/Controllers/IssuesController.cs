using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyLocalGov.com.Models;
using MyLocalGov.com.Services.Interfaces;
using MyLocalGov.com.ViewModels.Issues;
using System.Security.Claims;

namespace MyLocalGov.com.Controllers
{
	// ============================================================================
	// IssuesController
	// ----------------------------------------------------------------------------
	// Manages user Issues (e.g., issues, incidents, complaints).
	// Allows users to create, view, and track their own reports.
	// ----------------------------------------------------------------------------
	// Typical Actions:
	// - Create()     : Display form to submit a new report
	// - MyIssues()  : List all Issues created by the current user
	// - Details(id)  : View details for a specific Issue report
	// ============================================================================
	[Authorize]
	public class IssuesController : Controller
	{
		private readonly IIssueService _issueService;

		public IssuesController(IIssueService issueService)
		{
			_issueService = issueService;
		}

		public IActionResult Index()
		{
			return View();
		}

		// GET: /Reports/ReportIssue
		[HttpGet]
		public IActionResult ReportIssue()
		{
			var vm = new IssueViewModel
			{
				Categories = GetCategories()
			};
			return View("ReportIssue", vm);
		}

		// POST: /Reports/ReportIssue
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ReportIssue(IssueViewModel vm, CancellationToken ct)
		{
			vm.Categories ??= GetCategories();

			if (!ModelState.IsValid)
			{
				return View("ReportIssue", vm);
			}

			var issueId = await _issueService.SubmitAsync(vm, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);

			TempData["ReportSubmitted"] = true;
			return RedirectToAction(nameof(Index));
		}

		private static IEnumerable<SelectListItem> GetCategories()
		{
			// Replace with DB-backed categories when available
			var items = new[]
			{
				new { Id = 1, Name = "Sanitation" },
				new { Id = 2, Name = "Roads" },
				new { Id = 3, Name = "Water" },
				new { Id = 4, Name = "Electricity" },
				new { Id = 5, Name = "Parks" },
				new { Id = 6, Name = "Waste" },
				new { Id = 7, Name = "Utilities" },
				new { Id = 8, Name = "Other" }
			};

			return items.Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name });
		}
	}
 
}