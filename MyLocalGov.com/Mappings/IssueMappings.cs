using Microsoft.AspNetCore.Mvc.Rendering;
using MyLocalGov.com.Models;
using MyLocalGov.com.ViewModels.Issues;

namespace MyLocalGov.com.Mappings
{
	public static class IssueMappings
	{
		public static IssueModel ToModel(this IssueViewModel vm, string reporterUserId)
		{
			if (vm is null) throw new ArgumentNullException(nameof(vm));
			if (string.IsNullOrWhiteSpace(reporterUserId))
				throw new ArgumentException("Reporter user ID is required.", nameof(reporterUserId));

			return new IssueModel
			{
				ReporterUserID = reporterUserId,
				Address = vm.Address,
				Latitude = vm.Latitude!.Value,
				Longitude = vm.Longitude!.Value,
				CategoryID = vm.CategoryID,
				Description = vm.Description,
				StatusID = 1,
				Priority = 3,
				DateReported = DateTime.UtcNow,
				LastUpdated = DateTime.UtcNow
			};
		}

		public static IssueViewModel ToViewModel(this IssueModel model, IEnumerable<SelectListItem>? categories = null)
		{
			if (model is null) throw new ArgumentNullException(nameof(model));

			return new IssueViewModel
			{
				Address = model.Address,
				Latitude = model.Latitude,
				Longitude = model.Longitude,
				CategoryID = model.CategoryID,
				Description = model.Description,
				Categories = categories ?? Enumerable.Empty<SelectListItem>()
			};
		}
	}
}