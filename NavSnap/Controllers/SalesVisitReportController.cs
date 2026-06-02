using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using System.Security.Claims;
using System.Text;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie")]
    public class SalesVisitReportController : Controller
    {
        private readonly AppDbContext _db;
        public SalesVisitReportController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index(int? userId, string? status, string? dateFrom, string? dateTo)
        {
            var q = BuildQuery(userId, status, dateFrom, dateTo);
            var rows = await q.OrderByDescending(x => x.ReportDate).ThenBy(x => x.User!.FullName).Take(300).ToListAsync();
            await FillFilterLookups(userId, status, dateFrom, dateTo);
            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> Export(int? userId, string? status, string? dateFrom, string? dateTo)
        {
            var rows = await BuildQuery(userId, status, dateFrom, dateTo)
                .OrderByDescending(x => x.ReportDate)
                .ThenBy(x => x.User!.FullName)
                .Take(2000)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Tanggal,Sales,Checkpoint,Status,Outcome,Catatan");
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",",
                    Csv(row.ReportDate.ToString("yyyy-MM-dd")),
                    Csv(row.User?.FullName ?? "-"),
                    Csv(row.Checkpoint?.CheckpointName ?? "-"),
                    Csv(row.VisitStatus),
                    Csv(row.Outcome ?? "-"),
                    Csv(row.Notes ?? "-")));
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", $"laporan_kunjungan_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await FillLookups();
            return View("Form", new SalesVisitReport());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesVisitReport model)
        {
            CleanModelState();
            if (!IsApprover() && TryUserId() is int uid) model.UserId = uid;
            if (!ModelState.IsValid) { await FillLookups(); return View("Form", model); }
            model.CreatedAt = DateTime.UtcNow;
            _db.SalesVisitReports.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Report kunjungan sales ditambahkan";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var row = await _db.SalesVisitReports.FindAsync(id);
            if (row == null) return NotFound();
            if (!CanAccess(row.UserId)) return Forbid();
            await FillLookups();
            return View("Form", row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SalesVisitReport model)
        {
            CleanModelState();
            if (!IsApprover() && TryUserId() is int uid) model.UserId = uid;
            if (!ModelState.IsValid) { await FillLookups(); return View("Form", model); }
            var row = await _db.SalesVisitReports.FindAsync(model.Id);
            if (row == null) return NotFound();
            if (!CanAccess(row.UserId)) return Forbid();
            row.UserId = model.UserId;
            row.CheckpointId = model.CheckpointId;
            row.ReportDate = model.ReportDate;
            row.VisitStatus = model.VisitStatus;
            row.Outcome = model.Outcome;
            row.Notes = model.Notes;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Report kunjungan sales diperbarui";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _db.SalesVisitReports.FindAsync(id);
            if (row != null && CanAccess(row.UserId))
            {
                _db.SalesVisitReports.Remove(row);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Report kunjungan sales dihapus";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task FillLookups()
        {
            var usersQ = _db.Users.Where(u => u.IsActive);
            if (!IsApprover() && TryUserId() is int uid) usersQ = usersQ.Where(u => u.Id == uid);
            ViewBag.Users = new SelectList(await usersQ.OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName");
            ViewBag.Checkpoints = new SelectList(await _db.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.CheckpointName).ToListAsync(), "Id", "CheckpointName");
        }

        private IQueryable<SalesVisitReport> BuildQuery(int? userId, string? status, string? dateFrom, string? dateTo)
        {
            var q = _db.SalesVisitReports.Include(x => x.User).Include(x => x.Checkpoint).AsQueryable();
            if (!IsApprover() && TryUserId() is int uid) q = q.Where(x => x.UserId == uid);
            else if (userId.HasValue) q = q.Where(x => x.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.VisitStatus == status);
            if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var from)) q = q.Where(x => x.ReportDate >= from);
            if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var to)) q = q.Where(x => x.ReportDate <= to);
            return q;
        }

        private async Task FillFilterLookups(int? userId, string? status, string? dateFrom, string? dateTo)
        {
            var usersQ = _db.Users.Where(u => u.IsActive);
            if (!IsApprover() && TryUserId() is int uid) usersQ = usersQ.Where(u => u.Id == uid);
            ViewBag.FilterUsers = new SelectList(await usersQ.OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName", userId);
            ViewBag.SelectedUserId = userId;
            ViewBag.SelectedStatus = status ?? "";
            ViewBag.DateFrom = dateFrom ?? "";
            ViewBag.DateTo = dateTo ?? "";
        }

        private void CleanModelState()
        {
            ModelState.Remove("User");
            ModelState.Remove("Checkpoint");
        }

        private bool IsApprover() => User.IsInRole("Administrator") || User.IsInRole("Pengawas");

        private bool CanAccess(int ownerUserId) => IsApprover() || TryUserId() == ownerUserId;

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
