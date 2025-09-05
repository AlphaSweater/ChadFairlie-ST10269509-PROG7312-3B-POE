using System.Collections.Concurrent;
using System.Text;
using MyLocalGov.com.Models;
using MyLocalGov.com.Repositories.Interfaces;
using MyLocalGov.com.Services.Interfaces;
using MyLocalGov.com.ViewModels.Issues;
using MyLocalGov.com.Mappings;

namespace MyLocalGov.com.Services.Implementations
{
	/// <inheritdoc cref="IIssueService"/>
	public class IssueService : IIssueService
	{
		#region Fields & DI

		private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _env;
		private readonly ILogger<IssueService> _logger;

		// Upper limit on how many files can be written to disk simultaneously.
		private const int MaxParallelUploads = 4;

		public IssueService(IUnitOfWork unitOfWork, IWebHostEnvironment env, ILogger<IssueService> logger)
		{
			_unitOfWork = unitOfWork;
			_env = env;
			_logger = logger;
		}

		#endregion Fields & DI

		#region Public API

		/// <inheritdoc />
		public async Task<string> SubmitAsync(IssueViewModel viewModel, string reporterUserId, CancellationToken ct = default)
		{
			if (viewModel is null)
				throw new ArgumentNullException(nameof(viewModel));
			if (string.IsNullOrWhiteSpace(reporterUserId))
				throw new ArgumentException("Reporter user ID is required.", nameof(reporterUserId));

			_logger.LogInformation("SubmitAsync: Creating issue for user {UserId}", reporterUserId);
			ct.ThrowIfCancellationRequested();

			// 1) Create the issue first to get its ID
			var issue = viewModel.ToModel(reporterUserId);
			await _unitOfWork.Issues.AddAsync(issue);
			await _unitOfWork.SaveAsync(); // ensure IssueID is generated
			_logger.LogInformation("SubmitAsync: Issue {IssueId} created", issue.IssueID);

			// 2) Save attachments directly (validation happens inside SaveAttachmentsAsync)
			var saved = await SaveAttachmentsAsync(issue.IssueID.ToString(), viewModel.Files, ct);

			if (saved > 0)
				_logger.LogInformation("SubmitAsync: Persisted {SavedCount} attachment(s) for Issue {IssueId}", saved, issue.IssueID);
			else
				_logger.LogDebug("SubmitAsync: No attachments to save for Issue {IssueId}", issue.IssueID);

			return issue.IssueID;
		}

		#endregion Public API

		#region Validation / Plan Building

		private List<UploadPlan> BuildUploadPlans(IEnumerable<IFormFile>? files, string issueId, string destDir)
		{
			var plans = new List<UploadPlan>();
			if (files is null)
			{
				_logger.LogDebug("BuildUploadPlans: No files provided");
				return plans;
			}

			Directory.CreateDirectory(destDir);

			// Keep track of existing + planned names
			var usedNames = new HashSet<string>(
				Directory.EnumerateFiles(destDir).Select(Path.GetFileName)!,
				StringComparer.OrdinalIgnoreCase);

			int total = 0, accepted = 0, skippedEmpty = 0, skippedBadName = 0, skippedDup = 0;

			foreach (var file in files)
			{
				total++;
				if (file is null || file.Length <= 0)
				{
					skippedEmpty++;
					continue;
				}

				var sanitized = MakeSafeFileName(file.FileName);
				if (string.IsNullOrWhiteSpace(sanitized))
				{
					skippedBadName++;
					continue;
				}

				// Ensure uniqueness against already-used names
				var finalName = EnsureUniqueFileName(usedNames, sanitized);
				if (!usedNames.Add(finalName))
				{
					skippedDup++;
					continue;
				}

				var fullPath = Path.Combine(destDir, finalName);
				var relativePath = Path.Combine("uploads", "issues", issueId, finalName).Replace('\\', '/');

				plans.Add(new UploadPlan(file, finalName, fullPath, relativePath));
				accepted++;
			}

			_logger.LogDebug("BuildUploadPlans: total={Total}, accepted={Accepted}, skippedEmpty={Empty}, skippedBadName={Bad}, skippedDuplicate={Dup}",
				total, accepted, skippedEmpty, skippedBadName, skippedDup);

			return plans;
		}

		#endregion Validation / Queue Building

		#region File Save (Sequential Planning → Concurrent Queue Workers)


		private async Task<int> SaveAttachmentsAsync(string issueId, IEnumerable<IFormFile>? files, CancellationToken ct)
		{
			if (string.IsNullOrEmpty(issueId))
				throw new ArgumentOutOfRangeException(nameof(issueId));

			var root = _env.WebRootPath ?? "wwwroot";
			var destDir = Path.Combine(root, "uploads", "issues", issueId);

			// Build final list of planned uploads (already validated + unique)
			var plannedUploads = BuildUploadPlans(files, issueId, destDir);
			if (plannedUploads.Count == 0)
			{
				_logger.LogDebug("SaveAttachmentsAsync: No uploads planned for Issue {IssueId}", issueId);
				return 0;
			}

			_logger.LogDebug("SaveAttachmentsAsync: Planned {Count} upload(s) for Issue {IssueId}", plannedUploads.Count, issueId);

			// 2) Create work queue + result queue
			var workQueue = new ConcurrentQueue<UploadPlan>(plannedUploads);
			var attachments = new ConcurrentQueue<IssueAttachmentModel>();
			int successCount = 0;

			var workerCount = Math.Min(MaxParallelUploads, plannedUploads.Count);
			var tasks = Enumerable.Range(1, workerCount).Select(workerId => Task.Run(async () =>
			{
				while (!ct.IsCancellationRequested && workQueue.TryDequeue(out var plan))
				{
					try
					{
						await using var fs = new FileStream(plan.FullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
						await plan.File.CopyToAsync(fs, ct);

						attachments.Enqueue(new IssueAttachmentModel(issueId, plan.FinalName, plan.RelativePath, plan.File.ContentType, plan.File.Length));
						Interlocked.Increment(ref successCount);
					}
					catch (Exception ex) when (ex is not OperationCanceledException)
					{
						_logger.LogError(ex, "Failed to save {FileName} for Issue {IssueId}", plan.FinalName, issueId);
						TryDelete(plan.FullPath);
					}
				}
			}, ct)).ToArray();

			try
			{
				await Task.WhenAll(tasks);
			}
			catch (OperationCanceledException)
			{
				_logger.LogWarning("SaveAttachmentsAsync: Batch canceled for Issue {IssueId}", issueId);
				throw;
			}

			// Persist saved attachments
			while (attachments.TryDequeue(out var att))
				await _unitOfWork.IssueAttachments.AddAsync(att);

			await _unitOfWork.SaveAsync();

			_logger.LogInformation("SaveAttachmentsAsync: {Success}/{Planned} attachment(s) saved for Issue {IssueId}",
				successCount, plannedUploads.Count, issueId);

			return successCount;
		}

		private sealed record UploadPlan(IFormFile File, string FinalName, string FullPath, string RelativePath);

		private static void TryDelete(string path)
		{
			try
			{
				if (File.Exists(path))
					File.Delete(path);
			}
			catch { /* swallow */ }
		}

		#endregion File Save (Sequential Planning → Concurrent Queue Workers)

		#region Helpers

		// Sanitize file name (remove invalid chars, keep extension).
		private static string MakeSafeFileName(string? originalName)
		{
			var raw = Path.GetFileName(originalName ?? string.Empty).Trim();
			if (string.IsNullOrEmpty(raw))
				return string.Empty;

			var ext = Path.GetExtension(raw);
			var name = Path.GetFileNameWithoutExtension(raw);

			var invalid = Path.GetInvalidFileNameChars();
			var sb = new StringBuilder(name.Length);
			foreach (var c in name)
				sb.Append(invalid.Contains(c) ? '_' : c);

			var maxBaseLen = Math.Max(1, 255 - ext.Length);
			var basePart = sb.Length > maxBaseLen ? sb.ToString(0, maxBaseLen) : sb.ToString();

			return basePart + ext;
		}

		// Ensures uniqueness against an in-memory set of already-used final names.
		private static string EnsureUniqueFileName(HashSet<string> usedNames, string fileName)
		{
			var name = Path.GetFileNameWithoutExtension(fileName);
			var ext = Path.GetExtension(fileName);
			var attempt = 0;

			string Candidate() => attempt == 0 ? $"{name}{ext}" : $"{name} ({attempt}){ext}";

			var candidate = Candidate();
			while (usedNames.Contains(candidate))
			{
				attempt++;
				candidate = Candidate();
			}
			return candidate;
		}

		#endregion Helpers
	}
}