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
			await _unitOfWork.SaveAsync(); // ensure IssueID is generated

			_logger.LogInformation("SubmitAsync: Issue {IssueId} created", issue.IssueID);

			// 2) Build a queue of validated, de-duplicated files
			var fileQueue = BuildFileQueue(viewModel.Files);
			_logger.LogInformation("SubmitAsync: {Count} attachment(s) queued for Issue {IssueId}", fileQueue.Count, issue.IssueID);

			// 3) Upload and persist attachments
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

		// Validates files and returns a queue for sequential processing.
		// - Skips empty files
		// - Sanitizes names
		// - De-duplicates by sanitized name (first occurrence wins)
		private Queue<IFormFile> BuildFileQueue(IEnumerable<IFormFile>? files)
		{
			var queue = new Queue<IFormFile>();
			if (files is null)
			{
				_logger.LogDebug("BuildFileQueue: No files provided");
				return queue;
			}

			var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var total = 0;
			var enqueued = 0;
			var skippedEmpty = 0;
			var skippedName = 0;
			var skippedDup = 0;

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

				// Only enqueue first occurrence of a logical name
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

		#region File save

		// Uploads files to wwwroot/uploads/issues/{issueId}/ and persists attachment rows,
		// sequentially dequeuing. Continues past individual failures.
		private async Task<int> SaveAttachmentsAsync(int issueId, Queue<IFormFile> fileQueue, CancellationToken ct)
		{
			if (issueId <= 0) throw new ArgumentOutOfRangeException(nameof(issueId));
			if (fileQueue is null) return 0;

			var root = _env.WebRootPath ?? "wwwroot";
			var destDir = Path.Combine(root, "uploads", "issues", issueId.ToString());
			Directory.CreateDirectory(destDir);
			_logger.LogDebug("SaveAttachmentsAsync: Destination directory {DestDir}", destDir);

			var saved = 0;

			while (fileQueue.Count > 0)
			{
				ct.ThrowIfCancellationRequested();

				var file = fileQueue.Dequeue();
				if (file is null || file.Length <= 0)
				{
					_logger.LogWarning("SaveAttachmentsAsync: Skipped a null/empty file for Issue {IssueId}", issueId);
					continue;
				}

				try
				{
					var safeName = MakeSafeFileName(file.FileName);
					if (string.IsNullOrWhiteSpace(safeName))
					{
						_logger.LogWarning("SaveAttachmentsAsync: Skipped file with invalid name. Original: {Original}", file.FileName);
						continue;
					}

					var finalName = EnsureUniqueFileName(destDir, safeName);
					var fullPath = Path.Combine(destDir, finalName);

					_logger.LogDebug(
						"SaveAttachmentsAsync: Writing {FileName} ({Bytes} bytes) to {Path}",
						finalName, file.Length, fullPath
					);

					await using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
					{
						await file.CopyToAsync(stream, ct);
					}

					var relativePath = Path.Combine("uploads", "issues", issueId.ToString(), finalName).Replace('\\', '/');

					var attachment = new IssueAttachmentModel
					{
						IssueID = issueId,
						FileName = finalName,
						FilePath = relativePath,
						ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType,
						FileSizeBytes = file.Length,
						UploadedAt = DateTime.UtcNow
					};

					await _unitOfWork.IssueAttachments.AddAsync(attachment);
					saved++;

					_logger.LogInformation(
						"SaveAttachmentsAsync: Saved attachment {FileName} for Issue {IssueId}",
						finalName, issueId
					);
				}
				catch (OperationCanceledException)
				{
					_logger.LogWarning("SaveAttachmentsAsync: Canceled while saving attachments for Issue {IssueId}", issueId);
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "SaveAttachmentsAsync: Failed saving a file for Issue {IssueId}", issueId);
					// Continue with next file
				}
			}

			if (saved > 0)
			{
				await _unitOfWork.SaveAsync();
			}

			return saved;
		}

		#endregion

		#region Helpers

		// Produces a sanitized file name safe for most filesystems.
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

		// Returns a unique file name in the target directory by appending " (n)" if needed.
		private static string EnsureUniqueFileName(string dir, string fileName)
		{
			var name = Path.GetFileNameWithoutExtension(fileName);
			var ext = Path.GetExtension(fileName);
			var attempt = 0;

			string Candidate() => attempt == 0 ? $"{name}{ext}" : $"{name} ({attempt}){ext}";

			var full = Path.Combine(dir, Candidate());
			while (File.Exists(full))
			{
				attempt++;
				full = Path.Combine(dir, Candidate());
			}
			return Path.GetFileName(full);
		}

		#endregion
	}
}