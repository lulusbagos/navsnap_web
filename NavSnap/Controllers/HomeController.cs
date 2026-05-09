using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.ViewModels;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var vm = new DashboardViewModel
            {
                TotalUsers = await _db.Users.CountAsync(u => u.IsActive),
                TotalCheckpoints = await _db.Checkpoints.CountAsync(c => c.IsActive),
                TodayVisits = await _db.SalesVisits.CountAsync(v => v.VisitDate == today),
                TodayArrived = await _db.SalesVisits.CountAsync(v => v.VisitDate == today && v.Status == "arrived"),
                TodayPending = await _db.SalesVisits.CountAsync(v => v.VisitDate == today && v.Status == "pending"),
                PendingStoreSubmissions = await _db.StoreSubmissions.CountAsync(s => s.Status == "pending"),
                PendingLeaveApprovals = await _db.LeaveRequests.CountAsync(l => l.Status == "pending_supervisor" || l.Status == "pending_hr"),
                PendingOvertimeApprovals = await _db.OvertimeRequests.CountAsync(o => o.Status == "pending_supervisor" || o.Status == "pending_hr")
            };
            vm.TotalPendingApprovals = vm.PendingStoreSubmissions + vm.PendingLeaveApprovals + vm.PendingOvertimeApprovals;
            vm.VisitCompletionRate = vm.TodayVisits == 0 ? 0 : Math.Round((double)vm.TodayArrived / vm.TodayVisits * 100, 1);

            vm.RecentVisits = await _db.SalesVisits
                .Include(v => v.User)
                .Include(v => v.Checkpoint)
                .Where(v => v.VisitDate == today)
                .OrderByDescending(v => v.CreatedAt)
                .Take(10)
                .Select(v => new RecentVisitItem
                {
                    SalesName = v.User.FullName,
                    CheckpointName = v.Checkpoint.CheckpointName,
                    Status = v.Status,
                    ArrivedAt = v.ArrivedAt
                }).ToListAsync();

            var salesRoleId = await _db.Roles
                .Where(r => r.RoleName == "Sales")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var salesUserIds = await _db.UserRoles
                .Where(ur => ur.RoleId == salesRoleId)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();
            vm.SalesTotal = salesUserIds.Count;

            var cutoff = DateTime.UtcNow.AddMinutes(-30);
            vm.SalesOnline = await _db.GpsLogs
                .Where(g => salesUserIds.Contains(g.UserId) && g.LoggedAt >= cutoff)
                .GroupBy(g => g.UserId)
                .Select(g => new SalesStatusItem
                {
                    UserId = g.Key,
                    LastLatitude = g.OrderByDescending(x => x.LoggedAt).First().Latitude,
                    LastLongitude = g.OrderByDescending(x => x.LoggedAt).First().Longitude,
                    LastSeen = g.Max(x => x.LoggedAt)
                }).ToListAsync();
            vm.SalesActiveToday = vm.SalesOnline.Count;

            var salesIdsOnline = vm.SalesOnline.Select(s => s.UserId).ToList();
            var onlineNames = await _db.Users
                .Where(u => salesIdsOnline.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);
            foreach (var s in vm.SalesOnline)
                s.FullName = onlineNames.TryGetValue(s.UserId, out var n) ? n : "-";

            var salesUsers = await _db.Users
                .Where(u => salesUserIds.Contains(u.Id))
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            var salesVisitsToday = await _db.SalesVisits
                .Where(v => v.VisitDate == today && salesUserIds.Contains(v.UserId))
                .ToListAsync();

            var mobilePhotos = await _db.MUsers
                .Where(m => salesUsers.Select(su => su.Username).Contains(m.User))
                .ToDictionaryAsync(m => m.User, m => m.PathFotoProfile);

            var lastSeenMap = vm.SalesOnline.ToDictionary(s => s.UserId, s => s.LastSeen);
            var performances = new List<SalesPerformanceItem>();

            foreach (var su in salesUsers)
            {
                var userVisits = salesVisitsToday.Where(v => v.UserId == su.Id).ToList();
                var dailyTarget = userVisits.Count;
                var completed = userVisits.Count(v => v.Status == "arrived" || v.Status == "completed");
                var achievementPercent = dailyTarget == 0 ? 0 : Math.Round((double)completed / dailyTarget * 100, 1);
                var starRating = dailyTarget == 0 ? 0 : Math.Clamp((int)Math.Ceiling(achievementPercent / 20d), 1, 5);

                var jobTitle = su.UserRoles.Select(ur => ur.Role.RoleName).FirstOrDefault() ?? "Sales";
                mobilePhotos.TryGetValue(su.Username, out var photoPath);
                lastSeenMap.TryGetValue(su.Id, out var lastSeen);

                performances.Add(new SalesPerformanceItem
                {
                    UserId = su.Id,
                    FullName = su.FullName,
                    JobTitle = jobTitle,
                    PhotoPath = photoPath,
                    DailyTarget = dailyTarget,
                    CompletedVisits = completed,
                    IsActiveToday = lastSeen.HasValue,
                    LastSeen = lastSeen,
                    AchievementPercent = achievementPercent,
                    StarRating = starRating
                });
            }

            vm.SalesPerformances = performances
                .OrderByDescending(p => p.AchievementPercent)
                .ThenBy(p => p.FullName)
                .ToList();

            vm.AvgAchievementPercent = vm.SalesPerformances.Count == 0
                ? 0
                : Math.Round(vm.SalesPerformances.Average(p => p.AchievementPercent), 1);

            return View(vm);
        }
    }
}
