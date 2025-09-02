using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace MyLocalGov.com.ViewModels.Reports
{
	public class IssueViewModel : IValidatableObject
	{
		// Address parts
		[Display(Name = "Street")]
		[Required(ErrorMessage = "Street is required.")]
		[StringLength(100, ErrorMessage = "Street cannot exceed 100 characters.")]
		public string Street { get; set; } = string.Empty;

		[Display(Name = "Suburb")]
		[StringLength(100, ErrorMessage = "Suburb cannot exceed 100 characters.")]
		public string Suburb { get; set; } = string.Empty;

		[Display(Name = "City")]
		[Required(ErrorMessage = "City is required.")]
		[StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
		public string City { get; set; } = string.Empty;

		[Display(Name = "Postal code")]
		[StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters.")]
		public string PostalCode { get; set; } = string.Empty;

		// Combined address (used for persistence)
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

		// Helper to format combined address from parts
		public string GetFormattedAddress()
		{
			var parts = new[] { Street, Suburb, City, PostalCode }
				.Where(s => !string.IsNullOrWhiteSpace(s));
			return string.Join(", ", parts);
		}

		// IValidatableObject implementation (optional rules can be added here)
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			yield break;
		}
	}
}
