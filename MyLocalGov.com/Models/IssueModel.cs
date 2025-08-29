using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyLocalGov.com.Models
{
    public class IssueModel
    {
        [Key]
        public int IssueID { get; set; }

        // Foreign key to UserProfileModel
        [Required]
        [ForeignKey(nameof(Reporter))]
        public string ReporterUserID { get; set; }

        public UserProfileModel Reporter { get; set; }

        [MaxLength(200)]
        public string LocationText { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public int CategoryID { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public int StatusID { get; set; } = 1;
        public int Priority { get; set; } = 3;

        public DateTime DateReported { get; set; }
        public DateTime LastUpdated { get; set; }

        // Navigation property for attachments
        public ICollection<IssueAttachmentModel> Attachments { get; set; }
    }

    public class IssueAttachmentModel
    {
        [Key]
        public int AttachmentID { get; set; }

        [Required]
        [ForeignKey(nameof(Issue))]
        public int IssueID { get; set; }

        public IssueModel Issue { get; set; }

        [MaxLength(255)]
        public string FileName { get; set; }

        [MaxLength(500)]
        public string FilePath { get; set; } // recommended

        public byte[] FileBlob { get; set; } // optional

        [MaxLength(100)]
        public string ContentType { get; set; }

        public long? FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
