using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using MyLocalGov.com.Models;
using MyLocalGov.com.Repositories.Interfaces;
using MyLocalGov.com.Services.Interfaces;
using MyLocalGov.com.ViewModels.Issues;
using MyLocalGov.com.Mappings;

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

			ct.ThrowIfCancellationRequested();

			// Map ViewModel to Model via simple extension
			var newIssue = model.ToModel(reporterUserId);

			// Process files via a queue to ensure strict one-by-one handling.
			// TODO: In future, enqueue work items to a background processor (IHostedService/Channel) for actual storage.
			if (model.Files is { Count: > 0 })
			{
				var filesQueue = new Queue<IFormFile>(model.Files.Where(f => f != null && f.Length > 0));
				// Dictionary to ensure unique filenames (base name -> occurrence count)
				var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

				while (filesQueue.Count > 0)
				{
					ct.ThrowIfCancellationRequested();

					var file = filesQueue.Dequeue();

					var uniqueSafeName = MakeUniqueSafeFileName(file.FileName, nameCounts);
					if (string.IsNullOrEmpty(uniqueSafeName)) continue;

					newIssue.Attachments.Add(new IssueAttachmentModel
					{
						Issue = newIssue,
						FileName = uniqueSafeName,
						ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType,
						FileSizeBytes = file.Length
						// Intentionally omitting: FilePath, FileBlob until storage is implemented
					});
				}
			}

			await _unitOfWork.Issues.AddAsync(newIssue);
			await _unitOfWork.SaveAsync();

			return newIssue.IssueID;
		}

		private static string MakeUniqueSafeFileName(string? originalName, IDictionary<string, int> nameCounts)
		{
			var raw = Path.GetFileName(originalName ?? string.Empty)?.Trim() ?? string.Empty;
			if (string.IsNullOrEmpty(raw)) return string.Empty;

			// Split into name + extension
			var ext = Path.GetExtension(raw);
			var name = Path.GetFileNameWithoutExtension(raw);

			// Basic normalization
			name = name.Trim().Replace(':', '_').Replace('/', '_').Replace('\\', '_');

			// Enforce max length (255 including extension)
			var maxBaseLen = Math.Max(1, 255 - ext.Length);
			if (name.Length > maxBaseLen) name = name[..maxBaseLen];

			// Ensure uniqueness using dictionary
			if (!nameCounts.TryGetValue(name, out var count))
			{
				nameCounts[name] = 1;
				return name + ext;
			}

			count++;
			nameCounts[name] = count;

			// Append " (n)" before extension, respecting max length
			var suffix = $" ({count})";
			var allowedBaseLen = Math.Max(1, 255 - ext.Length - suffix.Length);
			var basePart = name.Length > allowedBaseLen ? name[..allowedBaseLen] : name;

			return basePart + suffix + ext;
		}
	}
}