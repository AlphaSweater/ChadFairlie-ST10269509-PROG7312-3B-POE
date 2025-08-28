using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyLocalGov.com.Data;
using MyLocalGov.com.Models;

namespace MyLocalGov.com
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddControllersWithViews();

			// SQLite Database
			builder.Services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

			// Identity setup
			builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
			{
				options.Password.RequiredLength = 6;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequireUppercase = false;
				options.Password.RequireDigit = false;
			})
			.AddEntityFrameworkStores<ApplicationDbContext>()
			.AddDefaultTokenProviders();

			builder.Services.ConfigureApplicationCookie(options =>
			{
				options.LoginPath = "/Account/Login";
			});

			var app = builder.Build();

			// Auto-migrate database
			using (var scope = app.Services.CreateScope())
			{
				var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
				db.Database.Migrate();

				// Seed roles if not exist
				var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
				string[] roles = { "Admin", "User" };
				foreach (var role in roles)
				{
					if (!await roleManager.RoleExistsAsync(role))
					{
						await roleManager.CreateAsync(new ApplicationRole { Name = role });
					}
				}

				// Optionally seed an admin user
				var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
				var adminEmail = "admin@test.com";
				if (await userManager.FindByEmailAsync(adminEmail) == null)
				{
					var adminUser = new ApplicationUser
					{
						UserName = adminEmail,
						Email = adminEmail,
						FirstName = "Admin",
						LastName = "Guy"
					};
					await userManager.CreateAsync(adminUser, "Password123!");
					await userManager.AddToRoleAsync(adminUser, "Admin");
				}
			}

			// Middleware
			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthentication(); // Must be before Authorization
			app.UseAuthorization();

			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Home}/{action=Index}/{id?}");

			app.Run();
		}
	}
}