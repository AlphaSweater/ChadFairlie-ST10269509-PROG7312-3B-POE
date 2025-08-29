using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyLocalGov.com.ViewModels;

namespace MyLocalGov.com.Models
{
	public class UserProfileModel
	{
		[Key, ForeignKey(nameof(User))]
		[Required]
		public string UserID { get; set; } // PK and FK to AspNetUsers

		[Required]
		public IdentityUser User { get; set; }

		[MaxLength(100)]
		public string FirstName { get; set; }

		[MaxLength(100)]
		public string LastName { get; set; }

		[NotMapped]
		public string FullName => $"{FirstName} {LastName}";

		// Address fields (optional)
		[MaxLength(200)]
		public string? DefaultAddressLine { get; set; }

		[MaxLength(100)]
		public string? DefaultSuburb { get; set; }

		[MaxLength(100)]
		public string? DefaultCity { get; set; }

		[MaxLength(20)]
		public string? DefaultPostalCode { get; set; }

		public double? DefaultLatitude { get; set; }
		public double? DefaultLongitude { get; set; }

		// Status and reputation
		public int ReputationPoints { get; set; } = 0;
		public bool IsActive { get; set; } = true;

		// Preferences (JSON for extensibility)
		public string PreferencesJson { get; set; }

		// Constructor for mapping RegisterViewModel
		public UserProfileModel(RegisterViewModel model, IdentityUser user)
		{
			UserID = user.Id;
			User = user;
			FirstName = model.FirstName;
			LastName = model.Surname;
			PreferencesJson = "{}";
		}

		// Parameterless constructor for EF Core
		public UserProfileModel() { }
	}
}