using System.Threading;
using System.Threading.Tasks;
using MyLocalGov.com.ViewModels.Issues;

namespace MyLocalGov.com.Services.Interfaces
{
	/// <summary>
	/// Service for creating an Issue (and its attachments) in one call.
	/// Flow (SubmitAsync):
	/// 1. Persist the Issue to obtain IssueID.
	/// 2. Filter incoming files: skip null/empty, sanitize names, drop duplicates.
	/// 3. Save remaining files sequentially to: wwwroot/uploads/issues/{IssueID}/
	/// 4. Create attachment records and commit.
	/// Returns the new IssueID. Throws for invalid arguments or cancellation.
	/// Individual file save failures are logged and skipped; others continue.
	/// </summary>
	public interface IIssueService
	{
		/// <summary>
		/// Creates the Issue and (if provided) saves attachments; returns IssueID.
		/// </summary>
		Task<int> SubmitAsync(IssueViewModel model, string reporterUserId, CancellationToken ct = default);
	}
}