using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using NavSnap.Models.ViewModels;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie")]
    public class PayrollController : Controller
    {
        private readonly AppDbContext _db;
        public PayrollController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            ViewBag.TodayPresent = await _db.AttendanceLogs.CountAsync(a => a.AttendanceDate == today && a.CheckInAt != null);
            ViewBag.PendingLeave = await _db.LeaveRequests.CountAsync(a => a.Status == "pending");
            ViewBag.PendingOvertime = await _db.OvertimeRequests.CountAsync(a => a.Status == "pending");
            return View();
        }

        [Authorize(Roles = "Administrator,Pengawas")]
        [HttpGet]
        public async Task<IActionResult> AttendanceSettings()
        {
            var locations = await _db.AttendanceSettings
                .Include(x => x.GeofencePoints)
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync();

            var vm = new AttendanceSettingsPageViewModel
            {
                Form = new AttendanceSettingViewModel { RadiusMeters = 100 },
                Locations = locations.Select(l => new AttendanceLocationItem
                {
                    Id = l.Id,
                    LocationName = l.LocationName,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    RadiusMeters = l.RadiusMeters,
                    IsActive = l.IsActive,
                    CreatedAt = l.CreatedAt,
                    PointCount = l.GeofencePoints.Count
                }).ToList()
            };
            return View(vm);
        }

        [Authorize(Roles = "Administrator,Pengawas")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendanceSettings(AttendanceSettingViewModel form)
        {
            if (!ModelState.IsValid)
            {
                var locations = await _db.AttendanceSettings
                    .Include(x => x.GeofencePoints)
                    .OrderByDescending(x => x.IsActive).ThenByDescending(x => x.CreatedAt)
                    .ToListAsync();
                var pageVm = new AttendanceSettingsPageViewModel
                {
                    Form = form,
                    Locations = locations.Select(l => new AttendanceLocationItem
                    {
                        Id = l.Id, LocationName = l.LocationName, Latitude = l.Latitude,
                        Longitude = l.Longitude, RadiusMeters = l.RadiusMeters,
                        IsActive = l.IsActive, CreatedAt = l.CreatedAt, PointCount = l.GeofencePoints.Count
                    }).ToList()
                };
                return View(pageVm);
            }

            var points = ParsePoints(form.GeofencePoints);
            if (points.Count < 3)
            {
                ModelState.AddModelError("", "Geofence minimal 3 titik.");
                var locations2 = await _db.AttendanceSettings
                    .Include(x => x.GeofencePoints)
                    .OrderByDescending(x => x.IsActive).ThenByDescending(x => x.CreatedAt)
                    .ToListAsync();
                var pageVm2 = new AttendanceSettingsPageViewModel
                {
                    Form = form,
                    Locations = locations2.Select(l => new AttendanceLocationItem
                    {
                        Id = l.Id, LocationName = l.LocationName, Latitude = l.Latitude,
                        Longitude = l.Longitude, RadiusMeters = l.RadiusMeters,
                        IsActive = l.IsActive, CreatedAt = l.CreatedAt, PointCount = l.GeofencePoints.Count
                    }).ToList()
                };
                return View(pageVm2);
            }

            var uid = TryUserId();
            var centerLat = points.Average(p => p.Lat);
            var centerLng = points.Average(p => p.Lng);
            var setting = new AttendanceSetting
            {
                LocationName = form.LocationName.Trim(),
                Latitude = centerLat,
                Longitude = centerLng,
                RadiusMeters = form.RadiusMeters,
                IsActive = true,
                CreatedBy = uid
            };
            _db.AttendanceSettings.Add(setting);
            await _db.SaveChangesAsync();

            int seq = 1;
            foreach (var p in points)
            {
                _db.AttendanceGeofencePoints.Add(new AttendanceGeofencePoint
                {
                    AttendanceSettingId = setting.Id,
                    SeqNo = seq++,
                    Latitude = p.Lat,
                    Longitude = p.Lng
                });
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Lokasi \"{setting.LocationName}\" berhasil ditambahkan";
            return RedirectToAction(nameof(AttendanceSettings));
        }

        [Authorize(Roles = "Administrator,Pengawas")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLocation(int id)
        {
            var loc = await _db.AttendanceSettings.FindAsync(id);
            if (loc != null)
            {
                loc.IsActive = !loc.IsActive;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Lokasi \"{loc.LocationName}\" {(loc.IsActive ? "diaktifkan" : "dinonaktifkan")}";
            }
            return RedirectToAction(nameof(AttendanceSettings));
        }

        [Authorize(Roles = "Administrator,Pengawas")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            var loc = await _db.AttendanceSettings.Include(x => x.GeofencePoints).FirstOrDefaultAsync(x => x.Id == id);
            if (loc != null)
            {
                _db.AttendanceGeofencePoints.RemoveRange(loc.GeofencePoints);
                _db.AttendanceSettings.Remove(loc);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Lokasi \"{loc.LocationName}\" dihapus";
            }
            return RedirectToAction(nameof(AttendanceSettings));
        }

        [Authorize(Roles = "Administrator,Pengawas")]
        [HttpGet]
        public async Task<IActionResult> GetLocationPoints(int id)
        {
            var points = await _db.AttendanceGeofencePoints
                .Where(p => p.AttendanceSettingId == id)
                .OrderBy(p => p.SeqNo)
                .Select(p => new GeoPointViewModel { Lat = p.Latitude, Lng = p.Longitude })
                .ToListAsync();
            return Json(points);
        }

        [HttpGet]
        public async Task<IActionResult> AttendanceReport(int? userId, string? dateFrom, string? dateTo)
        {
            var q = _db.AttendanceLogs.Include(a => a.User).AsQueryable();
            if (userId.HasValue) q = q.Where(a => a.UserId == userId.Value);
            if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var from)) q = q.Where(a => a.AttendanceDate >= from);
            if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var to)) q = q.Where(a => a.AttendanceDate <= to);
            if (string.IsNullOrWhiteSpace(dateFrom) && string.IsNullOrWhiteSpace(dateTo))
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                q = q.Where(a => a.AttendanceDate == today);
            }

            var rows = await q.OrderByDescending(a => a.AttendanceDate).ThenBy(a => a.User.FullName).ToListAsync();
            ViewBag.SalesList = await _db.UserRoles.Include(ur => ur.User).Include(ur => ur.Role)
                .Where(ur => ur.Role.RoleName == "Sales")
                .Select(ur => ur.User).Distinct().OrderBy(u => u.FullName).ToListAsync();
            ViewBag.SelectedUserId = userId;
            ViewBag.SelectedDateFrom = dateFrom ?? "";
            ViewBag.SelectedDateTo = dateTo ?? "";
            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> ExportAttendance(int? userId, string? dateFrom, string? dateTo)
        {
            var q = _db.AttendanceLogs.Include(a => a.User).AsQueryable();
            if (userId.HasValue) q = q.Where(a => a.UserId == userId.Value);
            if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var from)) q = q.Where(a => a.AttendanceDate >= from);
            if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var to)) q = q.Where(a => a.AttendanceDate <= to);
            if (string.IsNullOrWhiteSpace(dateFrom) && string.IsNullOrWhiteSpace(dateTo))
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                q = q.Where(a => a.AttendanceDate == today);
            }
            var rows = await q.OrderByDescending(a => a.AttendanceDate).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Tanggal,Nama,Check In,Check Out,Status,Keterangan");
            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",",
                    Csv(r.AttendanceDate.ToString("yyyy-MM-dd")),
                    Csv(r.User.FullName),
                    Csv(r.CheckInAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "-"),
                    Csv(r.CheckOutAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "-"),
                    Csv(r.Status),
                    Csv(r.Notes ?? "-")));
            }
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", $"laporan_absen_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> LeaveOvertime()
        {
            var isApprover = User.IsInRole("Administrator") || User.IsInRole("Pengawas");
            var uid = TryUserId();
            var leaveQ = _db.LeaveRequests.Include(x => x.User).AsQueryable();
            var overtimeQ = _db.OvertimeRequests.Include(x => x.User).AsQueryable();
            if (!isApprover && uid.HasValue)
            {
                leaveQ = leaveQ.Where(x => x.UserId == uid.Value);
                overtimeQ = overtimeQ.Where(x => x.UserId == uid.Value);
            }

            ViewBag.Leaves = await leaveQ.OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync();
            ViewBag.Overtimes = await overtimeQ.OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync();
            var now = DateTime.UtcNow;
            ViewBag.PendingSupervisor = await _db.LeaveRequests.CountAsync(x => x.Status == "pending_supervisor")
                                       + await _db.OvertimeRequests.CountAsync(x => x.Status == "pending_supervisor");
            ViewBag.PendingHr = await _db.LeaveRequests.CountAsync(x => x.Status == "pending_hr")
                             + await _db.OvertimeRequests.CountAsync(x => x.Status == "pending_hr");
            ViewBag.OverdueSla = await _db.LeaveRequests.CountAsync(x => (x.Status == "pending_supervisor" || x.Status == "pending_hr") && x.SlaDueAt != null && x.SlaDueAt < now)
                              + await _db.OvertimeRequests.CountAsync(x => (x.Status == "pending_supervisor" || x.Status == "pending_hr") && x.SlaDueAt != null && x.SlaDueAt < now);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitLeave(LeaveRequestInput input)
        {
            if (input.LeaveDateTo < input.LeaveDateFrom) ModelState.AddModelError("", "Tanggal akhir izin harus >= tanggal mulai");
            var uid = TryUserId();
            if (!uid.HasValue) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return RedirectToAction(nameof(LeaveOvertime));

            _db.LeaveRequests.Add(new LeaveRequest
            {
                UserId = uid.Value,
                LeaveDateFrom = input.LeaveDateFrom,
                LeaveDateTo = input.LeaveDateTo,
                LeaveType = input.LeaveType,
                Reason = string.IsNullOrWhiteSpace(input.Reason) ? null : input.Reason.Trim(),
                Status = await HasPengawasAsync() ? "pending_supervisor" : "pending_hr",
                ApprovalStage = 1,
                SlaDueAt = DateTime.UtcNow.AddDays(2)
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Pengajuan izin dikirim";
            return RedirectToAction(nameof(LeaveOvertime));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitOvertime(OvertimeRequestInput input)
        {
            var uid = TryUserId();
            if (!uid.HasValue) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return RedirectToAction(nameof(LeaveOvertime));

            _db.OvertimeRequests.Add(new OvertimeRequest
            {
                UserId = uid.Value,
                OvertimeDate = input.OvertimeDate,
                Hours = input.Hours,
                Reason = string.IsNullOrWhiteSpace(input.Reason) ? null : input.Reason.Trim(),
                Status = await HasPengawasAsync() ? "pending_supervisor" : "pending_hr",
                ApprovalStage = 1,
                SlaDueAt = DateTime.UtcNow.AddDays(2)
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Pengajuan lembur dikirim";
            return RedirectToAction(nameof(LeaveOvertime));
        }

        [Authorize(Roles = "Administrator,Pengawas")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLeave(int id, string actionType)
        {
            var item = await _db.LeaveRequests.FindAsync(id);
            if (item == null) return RedirectToAction(nameof(LeaveOvertime));
            var uid = TryUserId();
            if (!uid.HasValue) return RedirectToAction(nameof(LeaveOvertime));
            var isAdmin = User.IsInRole("Administrator");
            var isPengawas = User.IsInRole("Pengawas");

            if (actionType == "reject")
            {
                item.Status = "rejected";
                item.ApprovedAt = DateTime.UtcNow;
                item.ApprovedBy = uid;
            }
            else if (isPengawas && item.Status == "pending_supervisor")
            {
                item.SupervisorApprovedBy = uid;
                item.SupervisorApprovedAt = DateTime.UtcNow;
                item.ApprovalStage = 2;
                item.Status = "pending_hr";
            }
            else if (isAdmin && (item.Status == "pending_hr" || item.Status == "pending_supervisor"))
            {
                if (item.Status == "pending_supervisor")
                {
                    item.SupervisorApprovedBy ??= uid;
                    item.SupervisorApprovedAt ??= DateTime.UtcNow;
                }
                item.HrApprovedBy = uid;
                item.HrApprovedAt = DateTime.UtcNow;
                item.ApprovedBy = uid;
                item.ApprovedAt = DateTime.UtcNow;
                item.ApprovalStage = 3;
                item.Status = "approved";
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = "Status izin diperbarui";
            return RedirectToAction(nameof(LeaveOvertime));
        }

        [Authorize(Roles = "Administrator,Pengawas")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOvertime(int id, string actionType)
        {
            var item = await _db.OvertimeRequests.FindAsync(id);
            if (item == null) return RedirectToAction(nameof(LeaveOvertime));
            var uid = TryUserId();
            if (!uid.HasValue) return RedirectToAction(nameof(LeaveOvertime));
            var isAdmin = User.IsInRole("Administrator");
            var isPengawas = User.IsInRole("Pengawas");

            if (actionType == "reject")
            {
                item.Status = "rejected";
                item.ApprovedAt = DateTime.UtcNow;
                item.ApprovedBy = uid;
            }
            else if (isPengawas && item.Status == "pending_supervisor")
            {
                item.SupervisorApprovedBy = uid;
                item.SupervisorApprovedAt = DateTime.UtcNow;
                item.ApprovalStage = 2;
                item.Status = "pending_hr";
            }
            else if (isAdmin && (item.Status == "pending_hr" || item.Status == "pending_supervisor"))
            {
                if (item.Status == "pending_supervisor")
                {
                    item.SupervisorApprovedBy ??= uid;
                    item.SupervisorApprovedAt ??= DateTime.UtcNow;
                }
                item.HrApprovedBy = uid;
                item.HrApprovedAt = DateTime.UtcNow;
                item.ApprovedBy = uid;
                item.ApprovedAt = DateTime.UtcNow;
                item.ApprovalStage = 3;
                item.Status = "approved";
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = "Status lembur diperbarui";
            return RedirectToAction(nameof(LeaveOvertime));
        }

        private async Task<bool> HasPengawasAsync()
        {
            return await _db.UserRoles
                .Include(x => x.Role)
                .AnyAsync(x => x.Role.RoleName == "Pengawas");
        }

        private int? TryUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }

        private static string Csv(string value)
        {
            var safe = value.Replace("\"", "\"\"");
            return $"\"{safe}\"";
        }

        private static List<GeoPointViewModel> ParsePoints(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<GeoPointViewModel>();
            try
            {
                var data = JsonSerializer.Deserialize<List<GeoPointViewModel>>(raw);
                return data?.Where(p =>
                    p.Lat >= -90 && p.Lat <= 90 &&
                    p.Lng >= -180 && p.Lng <= 180).ToList() ?? new List<GeoPointViewModel>();
            }
            catch
            {
                return new List<GeoPointViewModel>();
            }
        }
    }
}
