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
			builder.Services.AddDbContext<MyLocalGovDbContext>(options =>
				options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

			// Identity setup
			builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
			{
				options.Password.RequiredLength = 6;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequireUppercase = false;
				options.Password.RequireDigit = false;
			})
			.AddEntityFrameworkStores<MyLocalGovDbContext>()
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
				var db = scope.ServiceProvider.GetRequiredService<MyLocalGovDbContext>();
				db.Database.Migrate();

				// Seed roles
				var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
				var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

				string[] roles = { "Admin", "MunicipalWorker", "Citizen" };
				foreach (var role in roles)
				{
					if (!await roleManager.RoleExistsAsync(role))
					{
						await roleManager.CreateAsync(new IdentityRole { Name = role });
					}
				}

				// Seed test data (regular user and issues)
				await TestDataSeeder.SeedAsync(db, userManager);
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