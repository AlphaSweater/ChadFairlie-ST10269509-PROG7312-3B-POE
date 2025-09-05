using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace MyLocalGov.com.ViewModels.Issues
{
	public class IssueViewModel : IValidatableObject
	{
		// Combined address
		[Required(ErrorMessage = "Address is required.")]
		[Display(Name = "Address")]
		[StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
		public string Address { get; set; } = string.Empty;

		[Display(Name = "Latitude")]
		public double? Latitude { get; set; }

		[Display(Name = "Longitude")]
		public double? Longitude { get; set; }

		// Category (match Model.CategoryID)
		[Required(ErrorMessage = "Category is required.")]
		[Display(Name = "Category")]
		[Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
		public int CategoryID { get; set; }

		// Content
		[Required(ErrorMessage = "Description is required.")]
		[Display(Name = "Description")]
		[StringLength(2000, ErrorMessage = "Description is too long.")]
		public string Description { get; set; } = string.Empty;

		// Attachments
		[Display(Name = "Attachments")]
		public List<IFormFile> Files { get; set; } = new();

		// UI data
		public IEnumerable<SelectListItem> Categories { get; set; } = Enumerable.Empty<SelectListItem>();

		// IValidatableObject implementation (optional rules can be added here)
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			yield break;
		}
	}
}
