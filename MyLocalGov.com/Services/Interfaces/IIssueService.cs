using MyLocalGov.com.ViewModels.Issues;

namespace MyLocalGov.com.Services.Interfaces
{
	/// <summary>
	/// Contract for the IssueService.
	/// 
	/// Basically, this defines what operations any "issue handling service"
	/// must provide — regardless of how it’s implemented.
	/// 
	/// By depending on this interface (instead of the concrete IssueService),
	/// we get cleaner code, easier testing, and the flexibility to swap 
	/// implementations later if needed.
	/// </summary>
	public interface IIssueService
	{
		/// <summary>
		/// Submit a new issue on behalf of a reporter.
		/// 
		/// This method takes in:
		///  - The view model with all issue details (title, description, attachments).
		///  - The user ID of the reporter (so we know who logged it).
		/// 
		/// What happens inside:
		///  1. The issue is saved to the DB (generates an IssueID).
		///  2. Any file uploads are validated, sanitized, written to disk, 
		///     and stored as IssueAttachment records.
		/// 
		/// Returns the IssueSubmissionResult from the service.
		/// </summary>
		/// <param name="viewModel">The form data / inputs from the user.</param>
		/// <param name="reporterUserId">The user who is submitting the issue.</param>
		/// <param name="ct">Optional cancellation token (lets callers cancel the operation).</param>
		/// <returns>The IssueSubmissionResult from the service.</returns>
		Task<IssueSubmissionResult> SubmitAsync(IssueViewModel viewModel, string reporterUserId, CancellationToken ct = default);

		/// <summary>
		/// Standard response wrapper for issue submission.
		/// </summary>
		public sealed record IssueSubmissionResult(
			bool Success,
			string IssueId,
			int AttachmentCount,
			string Message);
	}
}