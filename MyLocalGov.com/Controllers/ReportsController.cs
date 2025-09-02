using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyLocalGov.com.Models;
using MyLocalGov.com.ViewModels.Reports;
using System.Security.Claims;

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

			// Map ViewModel -> EF Model
			var issue = new IssueModel
			{
				ReporterUserID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
				LocationText = vm.Address,
				Latitude = vm.Latitude,
				Longitude = vm.Longitude,
				CategoryID = vm.CategoryID,
				Description = vm.Description,
				StatusID = 1,
				Priority = 3,
				DateReported = DateTime.UtcNow,
				LastUpdated = DateTime.UtcNow
			};

			// Handle files (store paths; adapt to your storage)
			if (vm.Files != null && vm.Files.Count > 0)
			{
				foreach (var file in vm.Files.Where(f => f?.Length > 0))
				{
					// TODO: save to storage (e.g., /wwwroot/uploads/issues/{guid}/filename.ext)
					// Capture metadata only for now:
					issue.Attachments.Add(new IssueAttachmentModel
					{
						FileName = file.FileName,
						ContentType = file.ContentType,
						FileSizeBytes = file.Length,
						UploadedAt = DateTime.UtcNow
					});
				}
			}

			// TODO: persist using your DbContext, e.g.:
			// _db.ReportIssues.Add(issue);
			// await _db.SaveChangesAsync(ct);

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