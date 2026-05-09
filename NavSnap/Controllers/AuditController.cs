using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie")]
    [Authorize(Roles = "Administrator,Pengawas")]
    public class AuditController : Controller
    {
        private readonly AppDbContext _db;
        public AuditController(AppDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> Index(string? module, string? dateFrom, string? dateTo)
        {
            var q = _db.AuditLogs.Include(a => a.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(module))
                q = q.Where(a => a.Module == module);

            if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var from))
                q = q.Where(a => a.CreatedAt >= from.ToDateTime(TimeOnly.MinValue));
            if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var to))
                q = q.Where(a => a.CreatedAt <= to.ToDateTime(TimeOnly.MaxValue));

            var rows = await q.OrderByDescending(a => a.CreatedAt).Take(300).ToListAsync();
            ViewBag.Modules = await _db.AuditLogs.Select(a => a.Module).Distinct().OrderBy(x => x).ToListAsync();
            ViewBag.SelectedModule = module ?? "";
            ViewBag.DateFrom = dateFrom ?? "";
            ViewBag.DateTo = dateTo ?? "";
            return View(rows);
        }
    }
}

