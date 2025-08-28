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
			builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
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
				options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
			});

			var app = builder.Build();

			// Enforce HTTPS and HSTS
			if (!app.Environment.IsDevelopment())
			{
				app.UseHttpsRedirection();
				app.UseHsts();
			}

			// Auto-migrate database
			using (var scope = app.Services.CreateScope())
			{
				var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
				db.Database.Migrate();

				// Seed roles
				var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
				string[] roles = { "Admin", "MunicipalWorker", "Citizen" };
				foreach (var role in roles)
				{
					if (!await roleManager.RoleExistsAsync(role))
					{
						await roleManager.CreateAsync(new IdentityRole { Name = role });
					}
				}

				// Seed an admin user
				var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
				var adminEmail = "admin@test.com";
				if (await userManager.FindByEmailAsync(adminEmail) == null)
				{
					var adminUser = new IdentityUser
					{
						UserName = adminEmail,
						Email = adminEmail
					};
					await userManager.CreateAsync(adminUser, "Password123!");
					await userManager.AddToRoleAsync(adminUser, "Admin");

					// Create UserProfile for admin
					var adminProfile = new UserProfileModel
					{
						Id = adminUser.Id,
						FirstName = "Admin",
						LastName = "Guy",
						Location = "Head Office",
						PreferencesJson = "{}",
						User = adminUser
					};
					db.UserProfiles.Add(adminProfile);
					await db.SaveChangesAsync();
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