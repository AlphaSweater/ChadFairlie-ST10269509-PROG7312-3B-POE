using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyLocalGov.com.Data;
using MyLocalGov.com.Models;
using MyLocalGov.com.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace MyLocalGov.com.Controllers
{
	[Authorize]
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly MyLocalGovDbContext _dbContext; // Assume ApplicationDbContext is your DB context class

		public HomeController(ILogger<HomeController> logger, MyLocalGovDbContext dbContext)
		{
			_logger = logger;
			_dbContext = dbContext;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		public IActionResult test()
		{
			return View();
		}

		public IActionResult Dashboard()
		{
			// Get the logged-in user's ID
			string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Query the database for the user's details
			// (Assuming you have injected ApplicationDbContext via DI)
			var user = _dbContext.UserProfiles.FirstOrDefault(u => u.UserID == userId);

			var viewModel = new DashboardViewModel
			{
				User = new UserInfo
				{
					Name = user != null ? $"{user.FullName}" : "Citizen User",
					IdNumber = "9012345678901", // Replace with your actual property
					Avatar = user != null ? $"{user.FirstName[0]}{user.LastName[0]}" : "CU",
					Municipality = "City of Cape Town", // Replace with your actual property
					Ward = "Ward 15" // Replace with your actual property
				},
				Stats = new DashboardStats
				{
					ActiveRequests = 3,
					CompletedIssues = 7,
					UpcomingEvents = 2,
					UrgentNotices = 1
				},
				RecentActivities = new List<RecentActivity>
				{
					new RecentActivity
					{
						Icon = "✅",
						Title = "Pothole Report Completed",
						Description = "Your reported pothole on Main Street has been repaired by the municipal maintenance team.",
						Timestamp = DateTime.Now.AddHours(-2),
						Status = ActivityStatus.Completed
					},
					new RecentActivity
					{
						Icon = "🔄",
						Title = "Service Request In Progress",
						Description = "Your waste collection schedule request is being processed by the environmental services department.",
						Timestamp = DateTime.Now.AddDays(-1),
						Status = ActivityStatus.InProgress
					},
					new RecentActivity
					{
						Icon = "⏳",
						Title = "Water Leak Report Pending",
						Description = "Your water leak report on Oak Avenue is pending review by the water services team.",
						Timestamp = DateTime.Now.AddDays(-3),
						Status = ActivityStatus.Pending
					},
					new RecentActivity
					{
						Icon = "🚨",
						Title = "Power Outage Report",
						Description = "Urgent: Power outage affecting residential area - priority response initiated.",
						Timestamp = DateTime.Now.AddDays(-5),
						Status = ActivityStatus.Urgent
					}
				},
				Announcements = new List<Announcement>
				{
					new Announcement
					{
						Icon = "🚰",
						Title = "Scheduled Water Maintenance",
						Content = "Water supply will be temporarily interrupted in the Northside residential area on Saturday, August 10th, from 8:00 AM to 2:00 PM for routine maintenance of the water infrastructure.",
						PostedDate = DateTime.Today,
						Type = AnnouncementType.Maintenance
					},
					new Announcement
					{
						Icon = "🏛️",
						Title = "Council Meeting - Public Invited",
						Content = "Monthly council meeting scheduled for August 15th at 6:00 PM in the Municipal Chambers. Citizens are welcome to attend and participate in the public comments session.",
						PostedDate = DateTime.Today.AddDays(-1),
						Type = AnnouncementType.Meeting
					},
					new Announcement
					{
						Icon = "🌳",
						Title = "Community Clean-Up Day",
						Content = "Join us for the monthly community clean-up event on August 20th starting at 9:00 AM. Meet at Central Park. Cleaning supplies and refreshments will be provided.",
						PostedDate = DateTime.Today.AddDays(-2),
						Type = AnnouncementType.Event
					}
				}
			};

			return View(viewModel);
		}

		[AllowAnonymous]
		public IActionResult AnimationsDemo()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}