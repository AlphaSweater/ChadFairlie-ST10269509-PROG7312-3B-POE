using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyLocalGov.com.Models;

namespace MyLocalGov.com.Data
{
	public class ApplicationDbContext : IdentityDbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		public DbSet<UserProfileModel> UserProfiles { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// 1-to-1 relationship between AspNetUser and UserProfile
			builder.Entity<UserProfileModel>()
				.HasOne(up => up.User)
				.WithOne()
				.HasForeignKey<UserProfileModel>(up => up.Id)
				.IsRequired();
		}
	}
}