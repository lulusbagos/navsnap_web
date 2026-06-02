using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using System.Text;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie", Roles = "Administrator,Pengawas")]
    public class SalesComplianceController : Controller
    {
        private readonly AppDbContext _db;
        public SalesComplianceController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index(int? userId, string? status, string? dateFrom, string? dateTo)
        {
            var q = BuildQuery(userId, status, dateFrom, dateTo);
            var rows = await q.OrderByDescending(x => x.ComplianceDate).ThenBy(x => x.User!.FullName).Take(300).ToListAsync();
            await FillFilterLookups(userId, status, dateFrom, dateTo);
            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> Export(int? userId, string? status, string? dateFrom, string? dateTo)
        {
            var rows = await BuildQuery(userId, status, dateFrom, dateTo)
                .OrderByDescending(x => x.ComplianceDate)
                .ThenBy(x => x.User!.FullName)
                .Take(2000)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Tanggal,Sales,Target,Actual,CompliancePercent,Status,Catatan");
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",",
                    Csv(row.ComplianceDate.ToString("yyyy-MM-dd")),
                    Csv(row.User?.FullName ?? "-"),
                    Csv(row.TargetVisits.ToString()),
                    Csv(row.ActualVisits.ToString()),
                    Csv(row.CompliancePercent.ToString("0.0")),
                    Csv(row.Status),
                    Csv(row.Notes ?? "-")));
            }
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", $"compliance_target_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await FillUsers();
            return View("Form", new SalesTargetCompliance());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesTargetCompliance model)
        {
            CleanModelState();
            ApplyCompliance(model);
            if (!ModelState.IsValid) { await FillUsers(); return View("Form", model); }
            model.CreatedAt = DateTime.UtcNow;
            _db.SalesTargetCompliances.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Compliance report ditambahkan";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var row = await _db.SalesTargetCompliances.FindAsync(id);
            if (row == null) return NotFound();
            await FillUsers();
            return View("Form", row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SalesTargetCompliance model)
        {
            CleanModelState();
            ApplyCompliance(model);
            if (!ModelState.IsValid) { await FillUsers(); return View("Form", model); }
            var row = await _db.SalesTargetCompliances.FindAsync(model.Id);
            if (row == null) return NotFound();
            row.UserId = model.UserId;
            row.ComplianceDate = model.ComplianceDate;
            row.TargetVisits = model.TargetVisits;
            row.ActualVisits = model.ActualVisits;
            row.CompliancePercent = model.CompliancePercent;
            row.Status = model.Status;
            row.Notes = model.Notes;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Compliance report diperbarui";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _db.SalesTargetCompliances.FindAsync(id);
            if (row != null)
            {
                _db.SalesTargetCompliances.Remove(row);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Compliance report dihapus";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task FillUsers()
        {
            ViewBag.Users = new SelectList(await _db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName");
        }

        private IQueryable<SalesTargetCompliance> BuildQuery(int? userId, string? status, string? dateFrom, string? dateTo)
        {
            var q = _db.SalesTargetCompliances.Include(x => x.User).AsQueryable();
            if (userId.HasValue) q = q.Where(x => x.UserId == userId.Value);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
            if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var from)) q = q.Where(x => x.ComplianceDate >= from);
            if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var to)) q = q.Where(x => x.ComplianceDate <= to);
            return q;
        }

        private async Task FillFilterLookups(int? userId, string? status, string? dateFrom, string? dateTo)
        {
            ViewBag.FilterUsers = new SelectList(await _db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName", userId);
            ViewBag.SelectedUserId = userId;
            ViewBag.SelectedStatus = status ?? "";
            ViewBag.DateFrom = dateFrom ?? "";
            ViewBag.DateTo = dateTo ?? "";
        }

        private static void ApplyCompliance(SalesTargetCompliance model)
        {
            model.CompliancePercent = model.TargetVisits <= 0 ? 0 : Math.Round((double)model.ActualVisits / model.TargetVisits * 100, 1);
            model.Status = model.CompliancePercent >= 90 ? "compliant" : model.CompliancePercent >= 75 ? "watch" : "risk";
        }

        private void CleanModelState() => ModelState.Remove("User");

        private static string Csv(string value)
        {
            var safe = value.Replace("\"", "\"\"");
            return $"\"{safe}\"";
        }
    }
}
