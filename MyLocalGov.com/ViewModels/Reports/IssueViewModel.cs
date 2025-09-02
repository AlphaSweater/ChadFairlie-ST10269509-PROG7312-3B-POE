using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyLocalGov.com.ViewModels.Reports
{
	public class IssueViewModel : IValidatableObject
	{
		// Location
		[Required, Display(Name = "Address")]
		[StringLength(200)]
		public string Address { get; set; } = string.Empty;

		[Display(Name = "Latitude")]
		public double? Latitude { get; set; }

		[Display(Name = "Longitude")]
		public double? Longitude { get; set; }

		// Category (match Model.CategoryID)
		[Required, Display(Name = "Category")]
		[Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
		public int CategoryID { get; set; }

		// Content
		[Required, Display(Name = "Description")]
		[StringLength(2000, ErrorMessage = "Description is too long.")]
		public string Description { get; set; } = string.Empty;

		// Attachments
		[Display(Name = "Attachments")]
		public List<IFormFile> Files { get; set; } = new();

		// UI data
		public IEnumerable<SelectListItem> Categories { get; set; } = Enumerable.Empty<SelectListItem>();

		// Cross-field validation (optional): ensure at least some location info
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(Address) && (Latitude == null || Longitude == null))
			{
				yield return new ValidationResult(
					"Provide an address or coordinates.",
					new[] { nameof(Address), nameof(Latitude), nameof(Longitude) }
				);
			}
		}
	}
}
