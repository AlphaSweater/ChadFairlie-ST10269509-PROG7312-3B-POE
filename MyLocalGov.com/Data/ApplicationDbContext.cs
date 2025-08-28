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

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// Reorder ApplicationUser columns
			builder.Entity<ApplicationUser>(entity =>
			{
				// Core identity column
				entity.Property(u => u.Id).HasColumnOrder(0);

				// Your custom fields next
				entity.Property(u => u.FirstName).HasColumnOrder(2);
				entity.Property(u => u.LastName).HasColumnOrder(3);

				// Then all the Identity defaults
				entity.Property(u => u.Email).HasColumnOrder(10);
				entity.Property(u => u.NormalizedEmail).HasColumnOrder(11);
				entity.Property(u => u.EmailConfirmed).HasColumnOrder(12);
				entity.Property(u => u.UserName).HasColumnOrder(13);
				entity.Property(u => u.NormalizedUserName).HasColumnOrder(14);
				entity.Property(u => u.PhoneNumber).HasColumnOrder(15);
				entity.Property(u => u.PhoneNumberConfirmed).HasColumnOrder(16);
				entity.Property(u => u.PasswordHash).HasColumnOrder(17);
				entity.Property(u => u.SecurityStamp).HasColumnOrder(18);
				entity.Property(u => u.ConcurrencyStamp).HasColumnOrder(19);
				entity.Property(u => u.TwoFactorEnabled).HasColumnOrder(20);
				entity.Property(u => u.LockoutEnd).HasColumnOrder(21);
				entity.Property(u => u.LockoutEnabled).HasColumnOrder(22);
				entity.Property(u => u.AccessFailedCount).HasColumnOrder(23);
			});
		}
	}
}