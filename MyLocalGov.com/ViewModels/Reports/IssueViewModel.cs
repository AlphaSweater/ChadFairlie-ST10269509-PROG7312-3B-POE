using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyLocalGov.com.ViewModels.Reports
{
	public class IssueViewModel : IValidatableObject
	{
		// Address parts
		[Display(Name = "Street")]
		[StringLength(100)]
		public string Street { get; set; } = string.Empty;

		[Display(Name = "Suburb")]
		[StringLength(100)]
		public string Suburb { get; set; } = string.Empty;

		[Display(Name = "City")]
		[StringLength(100)]
		public string City { get; set; } = string.Empty;

		[Display(Name = "Postal code")]
		[StringLength(20)]
		public string PostalCode { get; set; } = string.Empty;

		// Combined address (used for persistence)
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

		// Helper to format combined address from parts
		public string GetFormattedAddress()
		{
			var parts = new[] { Street, Suburb, City, PostalCode }
				.Where(s => !string.IsNullOrWhiteSpace(s));
			return string.Join(", ", parts);
		}

		// Cross-field validation: require minimum location info (Street+City) or coordinates
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			bool hasCoords = Latitude != null && Longitude != null;
			bool hasMinAddress = !string.IsNullOrWhiteSpace(Street) && !string.IsNullOrWhiteSpace(City);

			if (!hasCoords && !hasMinAddress)
			{
				yield return new ValidationResult(
					"Provide a location: Street and City or coordinates.",
					new[] { nameof(Street), nameof(City), nameof(Latitude), nameof(Longitude) }
				);
			}

			// Ensure Address is populated server-side if missing
			if (string.IsNullOrWhiteSpace(Address))
			{
				Address = GetFormattedAddress();
			}
		}
	}
}
