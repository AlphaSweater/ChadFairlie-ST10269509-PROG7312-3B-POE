using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyLocalGov.com.Data;
using MyLocalGov.com.Repositories.Implementations;
using MyLocalGov.com.Repositories.Interfaces;
using MyLocalGov.com.Services.Implementations;
using MyLocalGov.com.Services.Interfaces;
using MyLocalGov.com.Options;

namespace MyLocalGov.com
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Load environment-specific and local settings
			builder.Configuration
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
				.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

			// Bind Google Maps options
			builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection("GoogleMaps"));

			// ============================================
			// 1. Configure Services (Dependency Injection)
			// ============================================

			// Add Razor Pages and MVC Controllers
			builder.Services.AddControllersWithViews();

			// Register DbContext (SQLite)
			builder.Services.AddDbContext<MyLocalGovDbContext>(options =>
				options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

			// Register Identity (User & Role Management)
			builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
			{
				options.Password.RequiredLength = 8;
				options.Password.RequireNonAlphanumeric = true;
				options.Password.RequireUppercase = true;
				options.Password.RequireDigit = true;
			})
			.AddEntityFrameworkStores<MyLocalGovDbContext>()
			.AddDefaultTokenProviders();

			// Configure Authentication Cookie
			builder.Services.ConfigureApplicationCookie(options =>
			{
				options.LoginPath = "/Account/Login";
				options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
				options.Cookie.IsEssential = true;
				options.Cookie.HttpOnly = true;
				options.Cookie.SameSite = SameSiteMode.Strict;
				options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Session timeout
				options.SlidingExpiration = true;
				options.Cookie.MaxAge = null; // Session-based cookie
			});

			// HttpClient
			builder.Services.AddHttpClient();

			// Register Repositories
			builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
			builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

			builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
			builder.Services.AddScoped<IIssueRepository, IssueRepository>();
			builder.Services.AddScoped<IIssueAttachmentRepository, IssueAttachmentRepository>();

			// Register Services
			builder.Services.AddScoped<IAuthService, AuthService>();
			builder.Services.AddScoped<IIssueService, IssueService>();

			// Maps service
			builder.Services.AddScoped<IMapsService, MapsService>();

			// ============================================
			// 2. Build Application
			// ============================================
			var app = builder.Build();

			// ============================================
			// 3. Database Migration & Data Seeding
			// ============================================
			using (var scope = app.Services.CreateScope())
			{
				var db = scope.ServiceProvider.GetRequiredService<MyLocalGovDbContext>();
				db.Database.Migrate();

				// Seed Roles
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

				// Seed Test Data (Users & Issues)
				var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
				await TestDataSeeder.SeedAsync(unitOfWork, userManager);
			}

			// ============================================
			// 4. Configure Middleware Pipeline
			// ============================================

			// Error Handling & Security
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			// Authentication & Authorization
			app.UseAuthentication(); // Must be before UseAuthorization
			app.UseAuthorization();

			// ============================================
			// 5. Endpoint Routing
			// ============================================
			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Account}/{action=Index}/{id?}");

			// ============================================
			// 6. Run Application
			// ============================================
			app.Run();
		}
	}
}