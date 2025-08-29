using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyLocalGov.com.Models;

namespace MyLocalGov.com.Data
{
	public class MyLocalGovDbContext : IdentityDbContext
	{
		public MyLocalGovDbContext(DbContextOptions<MyLocalGovDbContext> options)
			: base(options)
		{
		}

		public DbSet<UserProfileModel> UserProfiles { get; set; }
		public DbSet<IssueModel> Issues { get; set; }
		public DbSet<IssueAttachmentModel> IssueAttachments { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// 1-to-1 relationship between AspNetUser and UserProfileModel
			builder.Entity<UserProfileModel>()
				.HasOne(up => up.User)
				.WithOne()
				.HasForeignKey<UserProfileModel>(up => up.UserID)
				.IsRequired();

			// 1-to-many relationship: UserProfileModel -> IssueModel
			builder.Entity<IssueModel>()
				.HasOne(i => i.Reporter)
				.WithMany()
				.HasForeignKey(i => i.ReporterUserID)
				.IsRequired();

			// 1-to-many relationship: IssueModel -> IssueAttachmentModel
			builder.Entity<IssueAttachmentModel>()
				.HasOne(a => a.Issue)
				.WithMany(i => i.Attachments)
				.HasForeignKey(a => a.IssueID)
				.IsRequired();
		}
	}
}