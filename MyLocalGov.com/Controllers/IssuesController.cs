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
	//   - Provides JSON (AJAX) responses when requested, enabling SweetAlert UX.
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
		/// Creates a new issue. Supports:
		///  - Standard form POST (redirect flow).
		///  - AJAX (X-Requested-With=XMLHttpRequest) returning JSON for SweetAlert.
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Consumes("multipart/form-data")]
		public async Task<IActionResult> CreateIssue(IssueViewModel vm, CancellationToken ct)
		{
			var isAjax = IsAjaxRequest();
			vm.Categories ??= GetCategories();

			if (!ModelState.IsValid)
			{
				if (!isAjax)
					return View("ReportIssue", vm);

				return BadRequest(new
				{
					success = false,
					message = "Validation failed.",
					errors = ModelState
						.Where(kvp => kvp.Value?.Errors.Count > 0)
						.Select(kvp => new
						{
							field = kvp.Key,
							messages = kvp.Value!.Errors.Select(e => e.ErrorMessage)
						})
				});
			}

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrWhiteSpace(userId))
			{
				const string failMsg = "Unable to resolve current user.";
				if (!isAjax)
				{
					ModelState.AddModelError(string.Empty, failMsg);
					return View("ReportIssue", vm);
				}
				return Unauthorized(new { success = false, message = failMsg });
			}

			try
			{
				var result = await _issueService.SubmitAsync(vm, userId, ct);

				if (!isAjax)
				{
					TempData["ReportSubmitted"] = true;
					return RedirectToAction(nameof(Index));
				}

				return Ok(new
				{
					success = result.Success,
					message = result.Message,
					issueId = result.IssueId,
					attachments = result.AttachmentCount,
					redirectUrl = Url.Action(nameof(Index))!
				});
			}
			catch (OperationCanceledException)
			{
				if (isAjax)
					return StatusCode(499, new { success = false, message = "Submission canceled." }); // Client Closed Request (non-standard but recognizable)
				ModelState.AddModelError(string.Empty, "Submission canceled.");
				return View("ReportIssue", vm);
			}
			catch (Exception ex)
			{
				if (isAjax)
					return StatusCode(500, new { success = false, message = "An unexpected error occurred.", detail = ex.Message });
				ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
				return View("ReportIssue", vm);
			}
		}

		/// <summary>
		/// Returns a static category list (replace with data store / config if needed).
		/// </summary>
		private static IEnumerable<SelectListItem> GetCategories() => _categories;
		
		private bool IsAjaxRequest() =>
				Request.Headers.TryGetValue("X-Requested-With", out var v) &&
				string.Equals(v, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
	}
}