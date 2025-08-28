using Microsoft.AspNetCore.Identity;

namespace MyLocalGov.com.Models
{
	public class ApplicationUser : IdentityUser
	{
		public string Name { get; set; }
		public string Surname { get; set; }
		public string FullName => $"{Name} {Surname}";
	}
}