using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mime;
using System.Numerics;

namespace MyLocalGov.com.Models
{
	public class IssueModel
	{
		[Key]
		public string IssueID { get; set; }

		// Reporter (FK to UserProfileModel)
		[Required]
		[ForeignKey(nameof(Reporter))]
		public string ReporterUserID { get; set; } = default!;

		[Required]
		public UserProfileModel Reporter { get; set; } = default!;

		// Location
		[Required]
		[MaxLength(200)]
		public string Address { get; set; } = string.Empty;

		// For value types, [Required] is redundant, keep as non-nullable doubles.
		public double Latitude { get; set; }
		public double Longitude { get; set; }

		// Category
		[Required]
		public int CategoryID { get; set; }

		// Content: align with VM's 2000 char limit
		[Required]
		[MaxLength(2000)]
		public string Description { get; set; } = string.Empty;

		// Status & priority (defaults)
		public int StatusID { get; set; } = 1; // e.g., 1 = New
		public int Priority { get; set; } = 3; // e.g., 1 High, 3 Normal, 5 Low

		// Audit
		public DateTime DateReported { get; set; }
		public DateTime LastUpdated { get; set; }

		// Attachments
		public ICollection<IssueAttachmentModel> Attachments { get; set; } = new List<IssueAttachmentModel>();

		// Default constructor
		public IssueModel()
		{
			IssueID = Guid.NewGuid().ToString();
			DateReported = DateTime.UtcNow;
			LastUpdated = DateTime.UtcNow;
		}
	}

	public class IssueAttachmentModel
	{
		[Key]
		public string AttachmentID { get; set; }

		[Required]
		[ForeignKey(nameof(Issue))]
		public string IssueID { get; set; }

		[Required]
		public IssueModel Issue { get; set; } = default!;

		[MaxLength(255)]
		public string FileName { get; set; } = string.Empty;

		// Recommended: store path to file on disk/cloud storage
		[MaxLength(500)]
		public string? FilePath { get; set; }

		// Optional: if you also store blobs in DB
		public byte[]? FileBlob { get; set; }

		[MaxLength(100)]
		public string? ContentType { get; set; }

		public long? FileSizeBytes { get; set; }

		public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

		public IssueAttachmentModel(string issueId, string filename, string filePath, string contentType, long fileSizeBytes)
		{
			AttachmentID = Guid.NewGuid().ToString();
			IssueID = issueId;
			FileName = filename;
			FilePath = filePath;
			ContentType = contentType;
			FileSizeBytes = fileSizeBytes;
			UploadedAt = DateTime.UtcNow;
		}
	}
}
