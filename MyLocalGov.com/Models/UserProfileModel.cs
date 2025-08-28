using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyLocalGov.com.Models
{
	public class UserProfileModel
	{
		[Key, ForeignKey(nameof(User))]
		public string Id { get; set; } // PK and FK to AspNetUsers

		public IdentityUser User { get; set; }

		[MaxLength(100)]
		public string FirstName { get; set; }

		[MaxLength(100)]
		public string LastName { get; set; }

		[NotMapped]
		public string FullName => $"{FirstName} {LastName}";

		[MaxLength(200)]
		public string Location { get; set; }

		public string PreferencesJson { get; set; }
	}
}