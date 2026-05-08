using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using NavSnap.Models.ViewModels;
using System.Security.Claims;
using System.Text;

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
            var active = await _db.AttendanceSettings.Where(x => x.IsActive).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
            var vm = active == null
                ? new AttendanceSettingViewModel()
                : new AttendanceSettingViewModel
                {
                    LocationName = active.LocationName,
                    Latitude = active.Latitude,
                    Longitude = active.Longitude,
                    RadiusMeters = active.RadiusMeters
                };
            return View(vm);
        }

        [Authorize(Roles = "Administrator,Pengawas")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendanceSettings(AttendanceSettingViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var uid = TryUserId();
            var active = await _db.AttendanceSettings.Where(x => x.IsActive).ToListAsync();
            foreach (var a in active) a.IsActive = false;

            _db.AttendanceSettings.Add(new AttendanceSetting
            {
                LocationName = vm.LocationName.Trim(),
                Latitude = vm.Latitude,
                Longitude = vm.Longitude,
                RadiusMeters = vm.RadiusMeters,
                IsActive = true,
                CreatedBy = uid
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Setting lokasi absen berhasil disimpan";
            return RedirectToAction(nameof(AttendanceSettings));
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
                Status = "pending"
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
                Status = "pending"
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
            item.Status = actionType == "approve" ? "approved" : "rejected";
            item.ApprovedAt = DateTime.UtcNow;
            item.ApprovedBy = TryUserId();
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
            item.Status = actionType == "approve" ? "approved" : "rejected";
            item.ApprovedAt = DateTime.UtcNow;
            item.ApprovedBy = TryUserId();
            await _db.SaveChangesAsync();
            TempData["Success"] = "Status lembur diperbarui";
            return RedirectToAction(nameof(LeaveOvertime));
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
    }
}
