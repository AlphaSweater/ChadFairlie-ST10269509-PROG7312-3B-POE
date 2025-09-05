using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
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

		// Tune this to control parallelism of the physical uploads.
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

			// 1) Create the issue
			var issue = viewModel.ToModel(reporterUserId);
			await _unitOfWork.Issues.AddAsync(issue);
			await _unitOfWork.SaveAsync(); // ensure IssueID
			_logger.LogInformation("SubmitAsync: Issue {IssueId} created", issue.IssueID);

			// 2) Build queue (sequential validation + de-dupe)
			var fileQueue = BuildFileQueue(viewModel.Files);
			_logger.LogInformation("SubmitAsync: {Count} attachment(s) queued for Issue {IssueId}", fileQueue.Count, issue.IssueID);

			// 3) Parallel upload with sequential validation already done
			if (fileQueue.Count > 0)
			{
				var saved = await SaveAttachmentsAsync(issue.IssueID, fileQueue, ct);
				_logger.LogInformation("SubmitAsync: Saved {SavedCount} attachment(s) for Issue {IssueId}", saved, issue.IssueID);
			}
			else
			{
				_logger.LogDebug("SubmitAsync: No attachments to save for Issue {IssueId}", issue.IssueID);
			}

			return issue.IssueID;
		}

		#endregion

		#region Queue building

		private Queue<IFormFile> BuildFileQueue(IEnumerable<IFormFile>? files)
		{
			var queue = new Queue<IFormFile>();
			if (files is null)
			{
				_logger.LogDebug("BuildFileQueue: No files provided");
				return queue;
			}

			var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int total = 0, enqueued = 0, skippedEmpty = 0, skippedName = 0, skippedDup = 0;

			foreach (var file in files)
			{
				total++;

				if (file is null || file.Length <= 0)
				{
					skippedEmpty++;
					continue;
				}

				var safeName = MakeSafeFileName(file.FileName);
				if (string.IsNullOrWhiteSpace(safeName))
				{
					skippedName++;
					continue;
				}

				if (seenNames.Add(safeName))
				{
					queue.Enqueue(file);
					enqueued++;
				}
				else
				{
					skippedDup++;
				}
			}

			_logger.LogDebug(
				"BuildFileQueue: total={Total}, enqueued={Enqueued}, skippedEmpty={Empty}, skippedBadName={Bad}, skippedDuplicate={Dup}",
				total, enqueued, skippedEmpty, skippedName, skippedDup
			);

			return queue;
		}

		#endregion

		#region File save (sequential validation → parallel upload)

		// New approach: sequentially determine unique target names, then upload in parallel.
		private async Task<int> SaveAttachmentsAsync(int issueId, Queue<IFormFile> fileQueue, CancellationToken ct)
		{
			if (issueId <= 0) throw new ArgumentOutOfRangeException(nameof(issueId));
			if (fileQueue is null || fileQueue.Count == 0) return 0;

			var root = _env.WebRootPath ?? "wwwroot";
			var destDir = Path.Combine(root, "uploads", "issues", issueId.ToString());
			Directory.CreateDirectory(destDir);
			_logger.LogDebug("SaveAttachmentsAsync: Destination directory {DestDir}", destDir);

			// 1) Sequential pass: assign final unique names (no IO yet)
			var existingNames = new HashSet<string>(
				Directory.EnumerateFiles(destDir).Select(Path.GetFileName) ?? Array.Empty<string>(),
				StringComparer.OrdinalIgnoreCase);

			var planned = new List<UploadPlan>(fileQueue.Count);

			while (fileQueue.Count > 0)
			{
				ct.ThrowIfCancellationRequested();

				var file = fileQueue.Dequeue();
				if (file is null || file.Length <= 0)
				{
					_logger.LogWarning("SaveAttachmentsAsync: Skipped null/empty file during planning for Issue {IssueId}", issueId);
					continue;
				}

				var safeName = MakeSafeFileName(file.FileName);
				if (string.IsNullOrWhiteSpace(safeName))
				{
					_logger.LogWarning("SaveAttachmentsAsync: Skipped invalid filename (planning). Original={Original}", file.FileName);
					continue;
				}

				var finalName = EnsureUniqueFileName(existingNames, safeName);
				existingNames.Add(finalName);

				var fullPath = Path.Combine(destDir, finalName);
				var relativePath = Path.Combine("uploads", "issues", issueId.ToString(), finalName).Replace('\\', '/');

				planned.Add(new UploadPlan(file, finalName, fullPath, relativePath));
			}

			if (planned.Count == 0)
			{
				_logger.LogDebug("SaveAttachmentsAsync: Nothing to upload after planning for Issue {IssueId}", issueId);
				return 0;
			}

			_logger.LogDebug("SaveAttachmentsAsync: Planned {Count} upload(s) for Issue {IssueId}", planned.Count, issueId);

			// 2) Parallel upload (bounded)
			var semaphore = new SemaphoreSlim(MaxParallelUploads);
			var attachmentModels = new List<IssueAttachmentModel>(planned.Count);
			var uploadTasks = new List<Task>(planned.Count);
			int completed = 0;

			foreach (var plan in planned)
			{
				ct.ThrowIfCancellationRequested();
				await semaphore.WaitAsync(ct);

				uploadTasks.Add(Task.Run(async () =>
				{
					try
					{
						_logger.LogDebug("Upload START {FileName} ({Bytes} bytes) -> {Path}", plan.FinalName, plan.File.Length, plan.FullPath);

						await using (var stream = new FileStream(plan.FullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
						{
							await plan.File.CopyToAsync(stream, ct);
						}

						lock (attachmentModels)
						{
							attachmentModels.Add(new IssueAttachmentModel
							{
								IssueID = issueId,
								FileName = plan.FinalName,
								FilePath = plan.RelativePath,
								ContentType = string.IsNullOrWhiteSpace(plan.File.ContentType) ? null : plan.File.ContentType,
								FileSizeBytes = plan.File.Length,
								UploadedAt = DateTime.UtcNow
							});
						}

						var done = Interlocked.Increment(ref completed);
						_logger.LogInformation("Upload DONE {FileName} for Issue {IssueId} ({Done}/{Total})",
							plan.FinalName, issueId, done, planned.Count);
					}
					catch (OperationCanceledException)
					{
						_logger.LogWarning("Upload CANCELED {FileName} for Issue {IssueId}", plan.FinalName, issueId);
						TryDelete(plan.FullPath);
						throw;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Upload FAILED {FileName} for Issue {IssueId}", plan.FinalName, issueId);
						TryDelete(plan.FullPath);
						// swallow to allow other uploads to continue
					}
					finally
					{
						semaphore.Release();
					}
				}, ct));
			}

			try
			{
				await Task.WhenAll(uploadTasks);
			}
			catch (OperationCanceledException)
			{
				_logger.LogWarning("SaveAttachmentsAsync: Upload batch canceled for Issue {IssueId}", issueId);
				throw;
			}

			// 3) Persist successful attachments
			if (attachmentModels.Count > 0)
			{
				foreach (var att in attachmentModels)
					await _unitOfWork.IssueAttachments.AddAsync(att);

				await _unitOfWork.SaveAsync();
			}

			_logger.LogInformation("SaveAttachmentsAsync: {Saved}/{Planned} attachment(s) persisted for Issue {IssueId}",
				attachmentModels.Count, planned.Count, issueId);

			return attachmentModels.Count;
		}

		private sealed record UploadPlan(IFormFile File, string FinalName, string FullPath, string RelativePath);

		private static void TryDelete(string path)
		{
			try
			{
				if (File.Exists(path))
					File.Delete(path);
			}
			catch { /* ignore */ }
		}

		#endregion

		#region Helpers

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

		// Overload that ensures uniqueness using an existing in-memory set (no disk race).
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