using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyLocalGov.com.Data;
using MyLocalGov.com.ViewModels;
using System.Security.Claims;

namespace MyLocalGov.com.Controllers
{
	// ============================================================================
	// DashboardController
	// ----------------------------------------------------------------------------
	// Handles the main user dashboard displayed after login.
	// Acts as the "hub" where users access key features.
	// ----------------------------------------------------------------------------
	// Typical Actions:
	// - Index() : Show user dashboard with quick info and 3 main buttons:
	//             -> Make a Report
	//             -> Local Events & Announcements
	//             -> Service Request Status
	// ============================================================================
	[Authorize]
	public class DashboardController : Controller
	{
		private readonly MyLocalGovDbContext _dbContext;

		public DashboardController(MyLocalGovDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public IActionResult Index()
		{
			// Get the logged-in user's ID
			string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Query the database for the user's details
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

			return View("~/Views/Dashboard/DashboardView.cshtml", viewModel);
		}
	}
}