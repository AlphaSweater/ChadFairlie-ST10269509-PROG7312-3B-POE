using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyLocalGov.com.Models;

namespace MyLocalGov.com.Data
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options) { }

		// Optional: Add custom DbSets for other tables
		// public DbSet<Report> Reports { get; set; }
	}
}