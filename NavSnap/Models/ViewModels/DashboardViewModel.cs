namespace NavSnap.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalCheckpoints { get; set; }
        public int TodayVisits { get; set; }
        public int TodayArrived { get; set; }
        public int TodayPending { get; set; }
        public int SalesTotal { get; set; }
        public int SalesActiveToday { get; set; }
        public double AvgAchievementPercent { get; set; }
        public double VisitCompletionRate { get; set; }
        public int PendingStoreSubmissions { get; set; }
        public int PendingLeaveApprovals { get; set; }
        public int PendingOvertimeApprovals { get; set; }
        public int TotalPendingApprovals { get; set; }
        public List<RecentVisitItem> RecentVisits { get; set; } = new();
        public List<SalesStatusItem> SalesOnline { get; set; } = new();
        public List<SalesPerformanceItem> SalesPerformances { get; set; } = new();
    }

    public class RecentVisitItem
    {
        public string SalesName { get; set; } = string.Empty;
        public string CheckpointName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ArrivedAt { get; set; }
    }

    public class SalesStatusItem
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public double? LastLatitude { get; set; }
        public double? LastLongitude { get; set; }
        public DateTime? LastSeen { get; set; }
    }

    public class SalesPerformanceItem
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = "Sales";
        public string? PhotoPath { get; set; }
        public int DailyTarget { get; set; }
        public int CompletedVisits { get; set; }
        public bool IsActiveToday { get; set; }
        public DateTime? LastSeen { get; set; }
        public double AchievementPercent { get; set; }
        public int StarRating { get; set; }
    }
}
