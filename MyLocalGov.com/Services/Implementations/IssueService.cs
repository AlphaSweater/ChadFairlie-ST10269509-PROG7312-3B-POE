using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyLocalGov.com.Models;
using MyLocalGov.com.Repositories.Interfaces;
using MyLocalGov.com.Services.Interfaces;
using MyLocalGov.com.ViewModels.Issues;

namespace MyLocalGov.com.Services.Implementations
{
	/// <summary>
	/// Handles creation of issue reports using repositories via UnitOfWork.
	/// </summary>
	public class IssueService : IIssueService
	{
		private readonly IUnitOfWork _unitOfWork;

		public IssueService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		/// <inheritdoc />
		public async Task<int> SubmitAsync(IssueViewModel model, string reporterUserId, CancellationToken ct = default)
		{
			if (model is null)
				throw new ArgumentNullException(nameof(model));
			if (string.IsNullOrWhiteSpace(reporterUserId))
				throw new ArgumentException("Reporter user ID is required.", nameof(reporterUserId));

			var issue = new IssueModel
			{
				ReporterUserID = reporterUserId,
				LocationText = model.Address,
				Latitude = model.Latitude,
				Longitude = model.Longitude,
				CategoryID = model.CategoryID,
				Description = model.Description,
				StatusID = 1, // New
				Priority = 3, // Normal
				DateReported = DateTime.UtcNow,
				LastUpdated = DateTime.UtcNow
			};

			// Capture attachment metadata (storage can be added later)
			if (model.Files != null && model.Files.Count > 0)
			{
				foreach (var file in model.Files.Where(f => f != null && f.Length > 0))
				{
					issue.Attachments.Add(new IssueAttachmentModel
					{
						Issue = issue,
						FileName = file.FileName,
						ContentType = file.ContentType,
						FileSizeBytes = file.Length,
						UploadedAt = DateTime.UtcNow
					});
				}
			}

			await _unitOfWork.Issues.AddAsync(issue);
			await _unitOfWork.SaveAsync();

			return issue.IssueID;
		}
	}
}