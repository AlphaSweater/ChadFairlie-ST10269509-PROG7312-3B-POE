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

		// Hard cap: how many files I'll let upload at once (keeps disk + memory sane)
		private const int MaxParallelUploads = 4;

		public IssueService(IUnitOfWork unitOfWork, IWebHostEnvironment env, ILogger<IssueService> logger)
		{
			_unitOfWork = unitOfWork;
			_env = env;
			_logger = logger;
		}

		#endregion

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

			// 1) First save the Issue itself so we have a generated IssueID
			var issue = viewModel.ToModel(reporterUserId);
			await _unitOfWork.Issues.AddAsync(issue);
			await _unitOfWork.SaveAsync();
			_logger.LogInformation("SubmitAsync: Issue {IssueId} created", issue.IssueID);

			// 2) Then handle attachments → validation, sanitation, upload, and persistence
			var saved = await SaveAttachmentsAsync(issue.IssueID.ToString(), viewModel.Files, ct);

			if (saved > 0)
				_logger.LogInformation("SubmitAsync: Persisted {SavedCount} attachment(s) for Issue {IssueId}", saved, issue.IssueID);
			else
				_logger.LogDebug("SubmitAsync: No attachments to save for Issue {IssueId}", issue.IssueID);

			return issue.IssueID;
		}

		#endregion

		#region File Upload Flow

		/*
         * SaveAttachmentsAsync is basically the "pipeline" for handling uploads:
         *  1. Validate and sanitize → turn raw IFormFile(s) into planned upload tasks.
         *  2. Fan them out to a number of worker tasks (bounded concurrency).
         *  3. Each worker writes files safely to the disk, collecting metadata for DB.
         *  4. When all workers finish, persist metadata in one DB batch.
         */
		private async Task<int> SaveAttachmentsAsync(string issueId, IEnumerable<IFormFile>? files, CancellationToken ct)
		{
			if (string.IsNullOrEmpty(issueId))
				throw new ArgumentOutOfRangeException(nameof(issueId));

			var root = _env.WebRootPath ?? "wwwroot";
			var destDir = Path.Combine(root, "uploads", "issues", issueId);

			// Step 1: Build the upload plan (this is where we weed out bad/duplicate files)
			var plannedUploads = BuildUploadPlans(files, issueId, destDir);
			if (plannedUploads.Count == 0)
			{
				_logger.LogDebug("SaveAttachmentsAsync: No uploads planned for Issue {IssueId}", issueId);
				return 0;
			}

			// Step 2: Setup concurrent queues
			var workQueue = new ConcurrentQueue<UploadPlan>(plannedUploads);
			var attachments = new ConcurrentQueue<IssueAttachmentModel>();
			int successCount = 0;

			// Step 3: Spin up workers (up to MaxParallelUploads)
			var workerCount = Math.Min(MaxParallelUploads, plannedUploads.Count);
			var tasks = Enumerable.Range(1, workerCount).Select(workerId => Task.Run(async () =>
			{
				while (!ct.IsCancellationRequested && workQueue.TryDequeue(out var plan))
				{
					try
					{
						// Write file asynchronously to disk
						await using var fs = new FileStream(plan.FullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
						await plan.File.CopyToAsync(fs, ct);

						// Build DB attachment record
						attachments.Enqueue(new IssueAttachmentModel(
							issueId,
							plan.FinalName,
							plan.RelativePath,
							plan.File.ContentType,
							plan.File.Length
						));

						Interlocked.Increment(ref successCount);
					}
					catch (Exception ex) when (ex is not OperationCanceledException)
					{
						// Something went wrong → log + clean up partial file
						_logger.LogError(ex, "Failed to save {FileName} for Issue {IssueId}", plan.FinalName, issueId);
						TryDelete(plan.FullPath);
					}
				}
			}, ct)).ToArray();

			// Step 4: Wait for workers to finish
			try
			{
				await Task.WhenAll(tasks);
			}
			catch (OperationCanceledException)
			{
				_logger.LogWarning("SaveAttachmentsAsync: Batch canceled for Issue {IssueId}", issueId);
				throw;
			}

			// Step 5: Persist metadata to DB in a single batch
			while (attachments.TryDequeue(out var att))
				await _unitOfWork.IssueAttachments.AddAsync(att);

			await _unitOfWork.SaveAsync();

			_logger.LogInformation("SaveAttachmentsAsync: {Success}/{Planned} attachment(s) saved for Issue {IssueId}",
				successCount, plannedUploads.Count, issueId);

			return successCount;
		}

		#endregion

		#region Planning / Validation

		/*
         * BuildUploadPlans:
         *  - This is the "validation + sanitation" stage.
         *  - We check each file (skip empty/null).
         *  - We sanitize the file name (remove invalid chars, enforce length).
         *  - We ensure uniqueness → avoid overwriting if two files share a name.
         *  - We map the file into an UploadPlan (what will eventually be written).
         */
		private List<UploadPlan> BuildUploadPlans(IEnumerable<IFormFile>? files, string issueId, string destDir)
		{
			var plans = new List<UploadPlan>();
			if (files is null)
			{
				_logger.LogDebug("BuildUploadPlans: No files provided");
				return plans;
			}

			Directory.CreateDirectory(destDir);

			// Track both already-existing + newly-planned names
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

		#endregion

		#region Helpers


		private sealed record UploadPlan(IFormFile File, string FinalName, string FullPath, string RelativePath);

		private static void TryDelete(string path)
		{
			try
			{
				if (File.Exists(path))
					File.Delete(path);
			}
			catch
			{
				// Ignore clean-up failures (best effort)
			}
		}

		// Clean up filename → remove invalid chars, keep extension intact
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

			// Enforce max length (Windows max = 255 chars, including extension)
			var maxBaseLen = Math.Max(1, 255 - ext.Length);
			var basePart = sb.Length > maxBaseLen ? sb.ToString(0, maxBaseLen) : sb.ToString();

			return basePart + ext;
		}

		// If file exists with same name, append " (1)", " (2)", etc.
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

		#endregion
	}
}