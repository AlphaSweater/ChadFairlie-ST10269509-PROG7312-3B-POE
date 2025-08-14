namespace MyLocalGov.com.ViewModels
{
    public class DashboardViewModel
    {
        public UserInfo User { get; set; } = new UserInfo();
        public DashboardStats Stats { get; set; } = new DashboardStats();
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
        public List<Announcement> Announcements { get; set; } = new List<Announcement>();
    }

    public class UserInfo
    {
        public string Name { get; set; } = "Citizen User";
        public string IdNumber { get; set; } = "9012345678901";
        public string Avatar { get; set; } = "CU";
        public string Municipality { get; set; } = "City of Cape Town";
        public string Ward { get; set; } = "Ward 15";
    }

    public class DashboardStats
    {
        public int ActiveRequests { get; set; } = 3;
        public int CompletedIssues { get; set; } = 7;
        public int UpcomingEvents { get; set; } = 2;
        public int UrgentNotices { get; set; } = 1;
    }

    public class RecentActivity
    {
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public ActivityStatus Status { get; set; }
        public string StatusText => Status switch
        {
            ActivityStatus.Completed => "Completed",
            ActivityStatus.InProgress => "In Progress",
            ActivityStatus.Pending => "Pending",
            ActivityStatus.Urgent => "Urgent",
            _ => "Unknown"
        };
        public string StatusClass => Status switch
        {
            ActivityStatus.Completed => "status-completed",
            ActivityStatus.InProgress => "status-progress",
            ActivityStatus.Pending => "status-pending",
            ActivityStatus.Urgent => "status-urgent",
            _ => "status-pending"
        };
    }

    public enum ActivityStatus
    {
        Pending,
        InProgress,
        Completed,
        Urgent
    }

    public class Announcement
    {
        public string Icon { get; set; } = "📢";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime PostedDate { get; set; }
        public AnnouncementType Type { get; set; }
        public string TypeClass => Type switch
        {
            AnnouncementType.Maintenance => "announcement-maintenance",
            AnnouncementType.Meeting => "announcement-meeting",
            AnnouncementType.Event => "announcement-event",
            AnnouncementType.Notice => "announcement-notice",
            _ => "announcement-general"
        };
    }

    public enum AnnouncementType
    {
        General,
        Maintenance,
        Meeting,
        Event,
        Notice,
        Emergency
    }
}
