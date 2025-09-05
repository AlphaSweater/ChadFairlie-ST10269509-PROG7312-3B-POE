using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace MyLocalGov.com.ViewModels.Issues
{
	public class IssueViewModel
	{
		// Combined address
		[Required(ErrorMessage = "Address is required.")]
		[Display(Name = "Address")]
		[StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
		public string Address { get; set; } = string.Empty;

		[Required(ErrorMessage = "Latitude is required.")]
		[Display(Name = "Latitude")]
		[Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
		public double? Latitude { get; set; }

		[Required(ErrorMessage = "Longitude is required.")]
		[Display(Name = "Longitude")]
		[Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
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
	}
}
