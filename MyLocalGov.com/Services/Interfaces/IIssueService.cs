using System.Threading;
using System.Threading.Tasks;
using MyLocalGov.com.ViewModels.Reports;

namespace MyLocalGov.com.Services.Interfaces
{
	/// <summary>
	/// Service for creating and retrieving issue reports.
	/// </summary>
	public interface IIssueService
	{
		/// <summary>
		/// Creates a new issue from the provided view model and reporter user ID.
		/// Returns the created IssueID.
		/// </summary>
		Task<int> SubmitAsync(IssueViewModel model, string reporterUserId, CancellationToken ct = default);
	}
}