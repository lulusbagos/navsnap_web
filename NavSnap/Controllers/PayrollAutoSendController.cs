using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie", Roles = "Administrator,Pengawas")]
    public class PayrollAutoSendController : Controller
    {
        private readonly AppDbContext _db;
        public PayrollAutoSendController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var rows = await _db.PayrollAutoSends.OrderByDescending(x => x.SendDate).Take(200).ToListAsync();
            return View(rows);
        }

        [HttpGet]
        public IActionResult Create() => View("Form", new PayrollAutoSend());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PayrollAutoSend model)
        {
            NormalizePayrollStatus(model);
            if (!ModelState.IsValid) return View("Form", model);
            model.CreatedAt = DateTime.UtcNow;
            _db.PayrollAutoSends.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Jadwal payroll auto send ditambahkan";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var row = await _db.PayrollAutoSends.FindAsync(id);
            return row == null ? NotFound() : View("Form", row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PayrollAutoSend model)
        {
            NormalizePayrollStatus(model);
            if (!ModelState.IsValid) return View("Form", model);
            var row = await _db.PayrollAutoSends.FindAsync(model.Id);
            if (row == null) return NotFound();
            row.PayrollPeriod = model.PayrollPeriod;
            row.SendDate = model.SendDate;
            row.Status = model.Status;
            row.RecipientCount = model.RecipientCount;
            row.Notes = model.Notes;
            row.SentAt = model.SentAt;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Jadwal payroll auto send diperbarui";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkSent(int id)
        {
            var row = await _db.PayrollAutoSends.FindAsync(id);
            if (row != null)
            {
                row.Status = "sent";
                row.SentAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Payroll ditandai terkirim";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _db.PayrollAutoSends.FindAsync(id);
            if (row != null)
            {
                _db.PayrollAutoSends.Remove(row);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Jadwal payroll auto send dihapus";
            }
            return RedirectToAction(nameof(Index));
        }

        private static void NormalizePayrollStatus(PayrollAutoSend model)
        {
            model.Status = string.IsNullOrWhiteSpace(model.Status) ? "scheduled" : model.Status.Trim().ToLowerInvariant();
            if (model.Status == "sent")
                model.SentAt ??= DateTime.UtcNow;
            else
                model.SentAt = null;
        }
    }
}
