using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using System.Security.Claims;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie")]
    public class WorkArrangementController : Controller
    {
        private readonly AppDbContext _db;
        public WorkArrangementController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var isApprover = User.IsInRole("Administrator") || User.IsInRole("Pengawas");
            var uid = TryUserId();
            var q = _db.WorkArrangementRequests.Include(x => x.User).Include(x => x.Approver).AsQueryable();
            if (!isApprover && uid.HasValue) q = q.Where(x => x.UserId == uid.Value);
            return View(await q.OrderByDescending(x => x.CreatedAt).Take(300).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await FillUsers();
            return View("Form", new WorkArrangementRequest { UserId = TryUserId() ?? 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkArrangementRequest model)
        {
            CleanModelState();
            if (!IsApprover() && TryUserId() is int uid) model.UserId = uid;
            if (model.WorkDateTo < model.WorkDateFrom) ModelState.AddModelError("", "Tanggal selesai harus >= tanggal mulai");
            if (!ModelState.IsValid) { await FillUsers(); return View("Form", model); }
            model.RequestDate = DateOnly.FromDateTime(DateTime.Today);
            model.Status = "pending";
            model.CreatedAt = DateTime.UtcNow;
            _db.WorkArrangementRequests.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Pengajuan WFH/WFA dikirim";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var row = await _db.WorkArrangementRequests.FindAsync(id);
            if (row == null) return NotFound();
            if (!CanAccess(row.UserId)) return Forbid();
            await FillUsers();
            return View("Form", row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WorkArrangementRequest model)
        {
            CleanModelState();
            if (!IsApprover() && TryUserId() is int uid) model.UserId = uid;
            if (model.WorkDateTo < model.WorkDateFrom) ModelState.AddModelError("", "Tanggal selesai harus >= tanggal mulai");
            if (!ModelState.IsValid) { await FillUsers(); return View("Form", model); }
            var row = await _db.WorkArrangementRequests.FindAsync(model.Id);
            if (row == null) return NotFound();
            if (!CanAccess(row.UserId)) return Forbid();
            row.UserId = model.UserId;
            row.WorkDateFrom = model.WorkDateFrom;
            row.WorkDateTo = model.WorkDateTo;
            row.ArrangementType = model.ArrangementType;
            row.Location = model.Location;
            row.Reason = model.Reason;
            row.Status = model.Status;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Pengajuan WFH/WFA diperbarui";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrator,Pengawas")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string status)
        {
            var row = await _db.WorkArrangementRequests.FindAsync(id);
            var uid = TryUserId();
            if (row != null && uid.HasValue && (status == "approved" || status == "rejected"))
            {
                row.Status = status;
                row.ApprovedBy = uid.Value;
                row.ApprovedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Status WFH/WFA diperbarui";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _db.WorkArrangementRequests.FindAsync(id);
            if (row != null && CanAccess(row.UserId))
            {
                _db.WorkArrangementRequests.Remove(row);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Pengajuan WFH/WFA dihapus";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task FillUsers()
        {
            var usersQ = _db.Users.Where(u => u.IsActive);
            if (!IsApprover() && TryUserId() is int uid) usersQ = usersQ.Where(u => u.Id == uid);
            ViewBag.Users = new SelectList(await usersQ.OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName");
        }

        private bool IsApprover() => User.IsInRole("Administrator") || User.IsInRole("Pengawas");

        private bool CanAccess(int ownerUserId) => IsApprover() || TryUserId() == ownerUserId;

        private int? TryUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }

        private void CleanModelState()
        {
            ModelState.Remove("User");
            ModelState.Remove("Approver");
        }
    }
}
