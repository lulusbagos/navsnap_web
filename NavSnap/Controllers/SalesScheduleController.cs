using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using NavSnap.Models.ViewModels;
using System.Globalization;
using System.Security.Claims;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie")]
    [Authorize(Roles = "Administrator,Pengawas")]
    public class SalesScheduleController : Controller
    {
        private readonly AppDbContext _db;
        public SalesScheduleController(AppDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> Index(string? dateFrom, string? dateTo, int? userId)
        {
            var q = _db.SalesVisitSchedules
                .Include(x => x.User)
                .Include(x => x.Checkpoint)
                .AsQueryable();

            if (userId.HasValue) q = q.Where(x => x.UserId == userId.Value);
            if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var from)) q = q.Where(x => x.ScheduleDate >= from);
            if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var to)) q = q.Where(x => x.ScheduleDate <= to);
            if (string.IsNullOrWhiteSpace(dateFrom) && string.IsNullOrWhiteSpace(dateTo))
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                q = q.Where(x => x.ScheduleDate >= today && x.ScheduleDate <= today.AddDays(7));
            }

            var rows = await q.OrderBy(x => x.ScheduleDate).ThenBy(x => x.StartTime).ToListAsync();
            ViewBag.Rows = rows;
            ViewBag.SalesList = await _db.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.RoleName == "Sales")
                .Select(ur => ur.User)
                .Distinct()
                .OrderBy(u => u.FullName)
                .ToListAsync();
            ViewBag.Checkpoints = await _db.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.CheckpointName).ToListAsync();
            ViewBag.SelectedUserId = userId;
            ViewBag.SelectedDateFrom = dateFrom ?? "";
            ViewBag.SelectedDateTo = dateTo ?? "";
            ViewBag.RouteInsights = await BuildRouteInsightsAsync(rows);
            return View(new SalesScheduleInput());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesScheduleInput input)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Input jadwal tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            if (input.DateTo < input.DateFrom)
            {
                TempData["Error"] = "Tanggal akhir harus lebih besar atau sama dengan tanggal awal.";
                return RedirectToAction(nameof(Index));
            }

            if (!TryParseTime(input.StartTime, out var start) || !TryParseTime(input.EndTime, out var end) || end <= start)
            {
                TempData["Error"] = "Jam kunjungan tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            var uid = TryUserId();
            var created = 0;
            var skippedConflict = 0;
            var skippedDuplicateStore = 0;
            var skippedCapacity = 0;

            for (var date = input.DateFrom; date <= input.DateTo; date = date.AddDays(1))
            {
                // Anti duplikat: toko yang sama tidak boleh dijadwalkan lebih dari 1x per hari untuk sales yang sama.
                var duplicateStore = await _db.SalesVisitSchedules.AnyAsync(x =>
                    x.UserId == input.UserId &&
                    x.CheckpointId == input.CheckpointId &&
                    x.ScheduleDate == date &&
                    x.Status != "canceled");
                if (duplicateStore)
                {
                    skippedDuplicateStore++;
                    continue;
                }

                // Conflict: sales sudah ada jadwal lain pada slot waktu overlap.
                var sameDaySchedules = await _db.SalesVisitSchedules
                    .Where(x => x.UserId == input.UserId && x.ScheduleDate == date && x.Status != "canceled")
                    .ToListAsync();
                var hasOverlap = sameDaySchedules.Any(x => IsOverlap(input.StartTime, input.EndTime, x.StartTime, x.EndTime));
                if (hasOverlap)
                {
                    skippedConflict++;
                    continue;
                }

                // Batas kapasitas per hari berdasarkan target toko.
                var dailyTarget = await _db.SalesDailyTargets
                    .Where(t => t.UserId == input.UserId && t.TargetDate == date)
                    .Select(t => (int?)t.TargetStores)
                    .FirstOrDefaultAsync();
                if (dailyTarget.HasValue && dailyTarget.Value > 0 && sameDaySchedules.Count >= dailyTarget.Value)
                {
                    skippedCapacity++;
                    continue;
                }

                var schedule = new SalesVisitSchedule
                {
                    UserId = input.UserId,
                    CheckpointId = input.CheckpointId,
                    ScheduleDate = date,
                    StartTime = input.StartTime,
                    EndTime = input.EndTime,
                    Status = "planned",
                    CreatedBy = uid,
                    CreatedAt = DateTime.UtcNow
                };
                _db.SalesVisitSchedules.Add(schedule);
                created++;

                var hasVisit = await _db.SalesVisits.AnyAsync(v =>
                    v.UserId == input.UserId &&
                    v.CheckpointId == input.CheckpointId &&
                    v.VisitDate == date);
                if (!hasVisit)
                {
                    _db.SalesVisits.Add(new SalesVisit
                    {
                        UserId = input.UserId,
                        CheckpointId = input.CheckpointId,
                        VisitDate = date,
                        Status = "pending",
                        IsMandatory = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
            if (created > 0)
            {
                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = uid,
                    Module = "SalesSchedule",
                    Action = "Create Schedule",
                    EntityName = "SalesVisitSchedule",
                    Description = $"Create batch jadwal user:{input.UserId} checkpoint:{input.CheckpointId} {input.DateFrom:yyyy-MM-dd} s/d {input.DateTo:yyyy-MM-dd}. created={created}, dup={skippedDuplicateStore}, conflict={skippedConflict}, overcap={skippedCapacity}"
                });
                await _db.SaveChangesAsync();
            }
            TempData["Success"] = $"Jadwal dibuat: {created}. Dilewati duplikat toko: {skippedDuplicateStore}. Dilewati konflik jam: {skippedConflict}. Melebihi target harian: {skippedCapacity}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var row = await _db.SalesVisitSchedules.FindAsync(id);
            if (row != null)
            {
                row.Status = "canceled";
                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = TryUserId(),
                    Module = "SalesSchedule",
                    Action = "Cancel Schedule",
                    EntityName = "SalesVisitSchedule",
                    EntityId = row.Id,
                    Description = $"Jadwal {row.ScheduleDate:yyyy-MM-dd} {row.StartTime}-{row.EndTime} dibatalkan."
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = "Jadwal dibatalkan.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoReassign(string? targetDate)
        {
            if (!DateOnly.TryParse(targetDate, out var date))
            {
                date = DateOnly.FromDateTime(DateTime.Today);
            }

            var planned = await _db.SalesVisitSchedules
                .Include(x => x.User)
                .Where(x => x.ScheduleDate == date && x.Status == "planned")
                .ToListAsync();

            if (!planned.Any())
            {
                TempData["Success"] = $"Tidak ada jadwal planned untuk {date:dd MMM yyyy}.";
                return RedirectToAction(nameof(Index), new { dateFrom = date.ToString("yyyy-MM-dd"), dateTo = date.ToString("yyyy-MM-dd") });
            }

            var salesRoleId = await _db.Roles.Where(r => r.RoleName == "Sales").Select(r => (int?)r.Id).FirstOrDefaultAsync();
            if (!salesRoleId.HasValue)
            {
                TempData["Error"] = "Role Sales tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            var salesIds = await _db.UserRoles
                .Where(ur => ur.RoleId == salesRoleId.Value)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            var attendance = await _db.AttendanceLogs
                .Where(a => a.AttendanceDate == date && salesIds.Contains(a.UserId))
                .ToListAsync();

            var presentSales = attendance
                .Where(a => a.CheckInAt != null && a.Status != "absent")
                .Select(a => a.UserId)
                .Distinct()
                .ToHashSet();

            var dailyTargets = await _db.SalesDailyTargets
                .Where(t => t.TargetDate == date && salesIds.Contains(t.UserId))
                .ToDictionaryAsync(t => t.UserId, t => t.TargetStores);

            var daySchedules = await _db.SalesVisitSchedules
                .Where(x => x.ScheduleDate == date && x.Status == "planned")
                .ToListAsync();

            var moved = 0;
            foreach (var row in planned)
            {
                var sourcePresent = presentSales.Contains(row.UserId);
                if (sourcePresent) continue;

                var candidates = salesIds
                    .Where(id => id != row.UserId && presentSales.Contains(id))
                    .ToList();

                int? pickUser = null;
                var bestLoad = int.MaxValue;
                foreach (var candidate in candidates)
                {
                    var candidateSchedules = daySchedules.Where(s => s.UserId == candidate && s.Status == "planned").ToList();

                    var hasOverlap = candidateSchedules.Any(s => IsOverlap(row.StartTime, row.EndTime, s.StartTime, s.EndTime));
                    if (hasOverlap) continue;

                    var duplicateStore = candidateSchedules.Any(s => s.CheckpointId == row.CheckpointId);
                    if (duplicateStore) continue;

                    var target = dailyTargets.TryGetValue(candidate, out var targetStores) ? targetStores : 0;
                    if (target > 0 && candidateSchedules.Count >= target) continue;

                    if (candidateSchedules.Count < bestLoad)
                    {
                        bestLoad = candidateSchedules.Count;
                        pickUser = candidate;
                    }
                }

                if (pickUser.HasValue)
                {
                    row.UserId = pickUser.Value;
                    moved++;
                }
            }

            await _db.SaveChangesAsync();
            if (moved > 0)
            {
                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = TryUserId(),
                    Module = "SalesSchedule",
                    Action = "Auto Reassign",
                    EntityName = "SalesVisitSchedule",
                    Description = $"Auto reassign tanggal {date:yyyy-MM-dd}, moved={moved}"
                });
                await _db.SaveChangesAsync();
            }
            TempData["Success"] = $"Auto re-assign {date:dd MMM yyyy}: {moved} jadwal dipindahkan.";
            return RedirectToAction(nameof(Index), new { dateFrom = date.ToString("yyyy-MM-dd"), dateTo = date.ToString("yyyy-MM-dd") });
        }

        private int? TryUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }

        private static bool TryParseTime(string text, out TimeSpan t)
        {
            return TimeSpan.TryParseExact(text, @"hh\:mm", CultureInfo.InvariantCulture, out t);
        }

        private static bool IsOverlap(string aStart, string aEnd, string bStart, string bEnd)
        {
            if (!TryParseTime(aStart, out var sa)) return false;
            if (!TryParseTime(aEnd, out var ea)) return false;
            if (!TryParseTime(bStart, out var sb)) return false;
            if (!TryParseTime(bEnd, out var eb)) return false;
            return sa < eb && sb < ea;
        }

        private async Task<List<RouteInsightRow>> BuildRouteInsightsAsync(List<SalesVisitSchedule> rows)
        {
            var result = new List<RouteInsightRow>();
            if (!rows.Any()) return result;

            var userIds = rows.Select(x => x.UserId).Distinct().ToList();
            var minDate = rows.Min(x => x.ScheduleDate).ToDateTime(TimeOnly.MinValue);
            var maxDate = rows.Max(x => x.ScheduleDate).ToDateTime(TimeOnly.MaxValue);

            var gpsLogs = await _db.GpsLogs
                .Where(g => userIds.Contains(g.UserId) && g.LoggedAt >= minDate && g.LoggedAt <= maxDate)
                .ToListAsync();

            var visits = await _db.SalesVisits
                .Where(v => userIds.Contains(v.UserId) && v.VisitDate >= DateOnly.FromDateTime(minDate) && v.VisitDate <= DateOnly.FromDateTime(maxDate))
                .ToListAsync();

            foreach (var row in rows)
            {
                var dayLogs = gpsLogs
                    .Where(g => g.UserId == row.UserId && DateOnly.FromDateTime(g.LoggedAt) == row.ScheduleDate)
                    .ToList();

                var nearest = dayLogs
                    .Select(g => new
                    {
                        Log = g,
                        Distance = HaversineMeters(g.Latitude, g.Longitude, row.Checkpoint.Latitude, row.Checkpoint.Longitude)
                    })
                    .OrderBy(x => x.Distance)
                    .FirstOrDefault();

                var visit = visits.FirstOrDefault(v =>
                    v.UserId == row.UserId &&
                    v.CheckpointId == row.CheckpointId &&
                    v.VisitDate == row.ScheduleDate);

                var distance = nearest?.Distance;
                var score = distance.HasValue ? Math.Max(0, Math.Min(100, 100 - (int)(distance.Value / 25))) : 0;
                var status = visit?.Status == "completed" || visit?.Status == "arrived"
                    ? "Arrived"
                    : (distance.HasValue
                        ? (distance.Value <= 250 ? "Near Target" : distance.Value <= 1500 ? "On Route" : "Off Route")
                        : "No Signal");

                result.Add(new RouteInsightRow
                {
                    ScheduleId = row.Id,
                    SalesName = row.User.FullName,
                    StoreName = row.Checkpoint.CheckpointName,
                    ScheduleDate = row.ScheduleDate,
                    TimeSlot = $"{row.StartTime} - {row.EndTime}",
                    NearestDistanceMeters = distance,
                    LastGpsAt = nearest?.Log.LoggedAt,
                    VisitStatus = status,
                    RouteScore = score
                });
            }

            return result
                .OrderBy(x => x.ScheduleDate)
                .ThenBy(x => x.TimeSlot)
                .ToList();
        }

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double DegreesToRadians(double d) => d * Math.PI / 180.0;

        public class RouteInsightRow
        {
            public int ScheduleId { get; set; }
            public string SalesName { get; set; } = "";
            public string StoreName { get; set; } = "";
            public DateOnly ScheduleDate { get; set; }
            public string TimeSlot { get; set; } = "";
            public double? NearestDistanceMeters { get; set; }
            public DateTime? LastGpsAt { get; set; }
            public string VisitStatus { get; set; } = "";
            public int RouteScore { get; set; }
        }
    }
}
