using Microsoft.AspNetCore.Identity;

namespace MyLocalGov.com.Models
{
	public class ApplicationUser : IdentityUser
	{
		// Optional extra fields
		public string FullName { get; set; }
	}
}