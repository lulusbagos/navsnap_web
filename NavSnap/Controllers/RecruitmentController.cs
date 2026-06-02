using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie", Roles = "Administrator,Pengawas")]
    public class RecruitmentController : Controller
    {
        private readonly AppDbContext _db;
        public RecruitmentController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            return View(await _db.RecruitmentCandidates.OrderByDescending(x => x.CreatedAt).Take(300).ToListAsync());
        }

        [HttpGet]
        public IActionResult Create() => View("Form", new RecruitmentCandidate());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecruitmentCandidate model)
        {
            CleanModelState();
            if (!ModelState.IsValid) return View("Form", model);
            model.CreatedAt = DateTime.UtcNow;
            _db.RecruitmentCandidates.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Kandidat recruitment ditambahkan";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var row = await _db.RecruitmentCandidates.FindAsync(id);
            return row == null ? NotFound() : View("Form", row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RecruitmentCandidate model)
        {
            CleanModelState();
            if (!ModelState.IsValid) return View("Form", model);
            var row = await _db.RecruitmentCandidates.FindAsync(model.Id);
            if (row == null) return NotFound();
            row.CandidateName = model.CandidateName;
            row.Email = model.Email;
            row.Phone = model.Phone;
            row.PositionApplied = model.PositionApplied;
            row.Stage = model.Stage;
            row.Status = model.Status;
            row.Source = model.Source;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Kandidat recruitment diperbarui";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _db.RecruitmentCandidates.FindAsync(id);
            if (row != null)
            {
                _db.RecruitmentCandidates.Remove(row);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Kandidat recruitment dihapus";
            }
            return RedirectToAction(nameof(Index));
        }

        private void CleanModelState()
        {
            ModelState.Remove("OnboardingTasks");
            ModelState.Remove("CvSummaries");
        }
    }
}
