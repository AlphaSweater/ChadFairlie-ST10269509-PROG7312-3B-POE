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

		#endregion

		#region Public API

		/// <inheritdoc />
		public async Task<int> SubmitAsync(IssueViewModel viewModel, string reporterUserId, CancellationToken ct = default)
		{
			if (viewModel is null) throw new ArgumentNullException(nameof(viewModel));
			if (string.IsNullOrWhiteSpace(reporterUserId))
				throw new ArgumentException("Reporter user ID is required.", nameof(reporterUserId));

			_logger.LogInformation("SubmitAsync: Creating issue for user {UserId}", reporterUserId);
			ct.ThrowIfCancellationRequested();

			// 1) Create the issue first to get its ID
			var issue = viewModel.ToModel(reporterUserId);
			await _unitOfWork.Issues.AddAsync(issue);
			await _unitOfWork.SaveAsync(); // ensure IssueID is generated
			_logger.LogInformation("SubmitAsync: Issue {IssueId} created", issue.IssueID);

			// 2) Validate / sanitize / de‑duplicate files, building a queue of validated file entries.
			var validatedQueue = BuildValidatedQueue(viewModel.Files);
			_logger.LogInformation("SubmitAsync: {Count} validated attachment(s) queued for Issue {IssueId}", validatedQueue.Count, issue.IssueID);

			// 3) Upload (parallel, limited)
			if (validatedQueue.Count > 0)
			{
				var saved = await SaveAttachmentsAsync(issue.IssueID, validatedQueue, ct);
				_logger.LogInformation("SubmitAsync: Persisted {SavedCount} attachment(s) for Issue {IssueId}", saved, issue.IssueID);
			}
			else
			{
				_logger.LogDebug("SubmitAsync: No attachments to save for Issue {IssueId}", issue.IssueID);
			}

			return issue.IssueID;
		}

		#endregion

		#region Validation / Queue Building

		// Represents a file that has passed initial validation (non-empty, sanitized & de-duplicated name).
		private sealed record ValidatedFile(IFormFile File, string SanitizedName);

		/// <summary>
		/// Builds a queue of ValidatedFile:
		/// - Skips null or empty files.
		/// - Sanitizes names (MakeSafeFileName).
		/// - Removes logical duplicates (post-sanitize name) keeping first occurrence.
		/// All expensive or conditional checks happen here so upload phase can focus purely on IO.
		/// </summary>
		private Queue<ValidatedFile> BuildValidatedQueue(IEnumerable<IFormFile>? files)
		{
			var queue = new Queue<ValidatedFile>();
			if (files is null)
			{
				_logger.LogDebug("BuildValidatedQueue: No files provided");
				return queue;
			}

			var seenSanitized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

				if (!seenSanitized.Add(sanitized))
				{
					skippedDup++;
					continue;
				}

				queue.Enqueue(new ValidatedFile(file, sanitized));
				accepted++;
			}

			_logger.LogDebug("BuildValidatedQueue: total={Total}, accepted={Accepted}, skippedEmpty={Empty}, skippedBadName={Bad}, skippedDuplicate={Dup}",
				total, accepted, skippedEmpty, skippedBadName, skippedDup);

			return queue;
		}

		#endregion

		#region File Save (Sequential Planning → Concurrent Queue Workers)

		/*
		 * Variant: ConcurrentQueue worker model
		 * ------------------------------------
		 * Differences from previous SemaphoreSlim + per-file Task approach:
		 * 1. Planning phase still runs once (sequential) to assign final unique names → no contention later.
		 * 2. A ConcurrentQueue<UploadPlan> is populated with all planned uploads.
		 * 3. We spin up MaxParallelUploads fixed worker tasks. Each worker:
		 *       while (queue.TryDequeue(out plan)) { upload; enqueue attachment model }
		 *    This avoids spawning one Task per file (better for very large batches) and removes explicit SemaphoreSlim usage.
		 * 4. A ConcurrentQueue<IssueAttachmentModel> gathers results without locks.
		 * 5. After all workers finish, we persist all successful attachments in a single batch.
		 *
		 * Rationale:
		 * - Queue-based fan-out is a natural fit for bounded parallelism without manual semaphore bookkeeping.
		 * - Reduces synchronization points (no lock around a shared List).
		 * - Still respects cancellation cooperatively.
		 */

		private async Task<int> SaveAttachmentsAsync(int issueId, Queue<ValidatedFile> validatedQueue, CancellationToken ct)
		{
			if (issueId <= 0) throw new ArgumentOutOfRangeException(nameof(issueId));
			if (validatedQueue is null || validatedQueue.Count == 0) return 0;

			var root = _env.WebRootPath ?? "wwwroot";
			var destDir = Path.Combine(root, "uploads", "issues", issueId.ToString());
			Directory.CreateDirectory(destDir);
			_logger.LogDebug("SaveAttachmentsAsync: Destination directory {DestDir}", destDir);

			// 1) Planning pass (assign final unique names up-front, no IO)
			var existingNames = new HashSet<string>(
				Directory.EnumerateFiles(destDir).Select(Path.GetFileName)!,
				StringComparer.OrdinalIgnoreCase);

			var plannedUploads = new List<UploadPlan>(validatedQueue.Count);

			while (validatedQueue.Count > 0)
			{
				ct.ThrowIfCancellationRequested();
				var vf = validatedQueue.Dequeue();

				// Sanitized name already provided, ensure final uniqueness
				var finalName = EnsureUniqueFileName(existingNames, vf.SanitizedName);
				existingNames.Add(finalName);

				var fullPath = Path.Combine(destDir, finalName);
				var relativePath = Path.Combine("uploads", "issues", issueId.ToString(), finalName).Replace('\\', '/');

				plannedUploads.Add(new UploadPlan(vf.File, finalName, fullPath, relativePath));
			}

			if (plannedUploads.Count == 0)
			{
				_logger.LogDebug("SaveAttachmentsAsync: Nothing to upload after planning for Issue {IssueId}", issueId);
				return 0;
			}

			_logger.LogDebug("SaveAttachmentsAsync: Planned {Count} upload(s) for Issue {IssueId}", plannedUploads.Count, issueId);

			// 2) Create work queue + result queue
			var workQueue = new ConcurrentQueue<UploadPlan>(plannedUploads);
			var attachments = new ConcurrentQueue<IssueAttachmentModel>();

			// Worker logic
			async Task WorkerAsync(int workerId)
			{
				while (!ct.IsCancellationRequested && workQueue.TryDequeue(out var plan))
				{
					try
					{
						_logger.LogDebug("Worker {Worker} START {FileName} ({Bytes} bytes)",
							workerId, plan.FinalName, plan.File.Length);

						await using (var fs = new FileStream(plan.FullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
						{
							await plan.File.CopyToAsync(fs, ct);
						}

						attachments.Enqueue(new IssueAttachmentModel
						{
							IssueID = issueId,
							FileName = plan.FinalName,
							FilePath = plan.RelativePath,
							ContentType = string.IsNullOrWhiteSpace(plan.File.ContentType) ? null : plan.File.ContentType,
							FileSizeBytes = plan.File.Length,
							UploadedAt = DateTime.UtcNow
						});

						var remaining = workQueue.Count;
						_logger.LogInformation("Worker {Worker} DONE {FileName} for Issue {IssueId} (remaining: {Remaining})",
							workerId, plan.FinalName, issueId, remaining);
					}
					catch (OperationCanceledException)
					{
						_logger.LogWarning("Worker {Worker} CANCELED during upload of {FileName} (Issue {IssueId})",
							workerId, plan.FinalName, issueId);
						TryDelete(plan.FullPath);
						throw;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Worker {Worker} FAILED {FileName} (Issue {IssueId})",
							workerId, plan.FinalName, issueId);
						TryDelete(plan.FullPath);
						// Continue with next item
					}
				}
			}

			// 3) Spin up limited workers
			var workerCount = Math.Min(MaxParallelUploads, plannedUploads.Count);
			var workerTasks = new Task[workerCount];
			for (int i = 0; i < workerCount; i++)
			{
				workerTasks[i] = Task.Run(() => WorkerAsync(i + 1), ct);
			}

			try
			{
				await Task.WhenAll(workerTasks);
			}
			catch (OperationCanceledException)
			{
				_logger.LogWarning("SaveAttachmentsAsync: Batch canceled for Issue {IssueId}", issueId);
				throw;
			}

			// 4) Persist successful attachments
			if (!attachments.IsEmpty)
			{
				while (attachments.TryDequeue(out var att))
					await _unitOfWork.IssueAttachments.AddAsync(att);

				await _unitOfWork.SaveAsync();
			}

			_logger.LogInformation("SaveAttachmentsAsync: Attachments persisted for Issue {IssueId}", issueId);
			return plannedUploads.Count - workQueue.Count; // Number of processed items (success + failures); 
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

		#endregion

		#region Helpers

		// Sanitize file name (remove invalid chars, keep extension).
		private static string MakeSafeFileName(string? originalName)
		{
			var raw = Path.GetFileName(originalName ?? string.Empty).Trim();
			if (string.IsNullOrEmpty(raw)) return string.Empty;

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

		#endregion
	}
}