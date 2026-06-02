using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie", Roles = "Administrator,Pengawas")]
    public class OnboardingController : Controller
    {
        private readonly AppDbContext _db;
        public OnboardingController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            return View(await _db.OnboardingTasks.Include(x => x.Candidate).OrderBy(x => x.DueDate).Take(300).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await FillCandidates();
            return View("Form", new OnboardingTask());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OnboardingTask model)
        {
            ModelState.Remove("Candidate");
            if (!ModelState.IsValid) { await FillCandidates(); return View("Form", model); }
            model.CreatedAt = DateTime.UtcNow;
            _db.OnboardingTasks.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Task onboarding ditambahkan";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var row = await _db.OnboardingTasks.FindAsync(id);
            if (row == null) return NotFound();
            await FillCandidates();
            return View("Form", row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OnboardingTask model)
        {
            ModelState.Remove("Candidate");
            if (!ModelState.IsValid) { await FillCandidates(); return View("Form", model); }
            var row = await _db.OnboardingTasks.FindAsync(model.Id);
            if (row == null) return NotFound();
            row.CandidateId = model.CandidateId;
            row.EmployeeName = model.EmployeeName;
            row.TaskName = model.TaskName;
            row.DueDate = model.DueDate;
            row.Status = model.Status;
            row.OwnerName = model.OwnerName;
            row.Notes = model.Notes;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Task onboarding diperbarui";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _db.OnboardingTasks.FindAsync(id);
            if (row != null)
            {
                _db.OnboardingTasks.Remove(row);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Task onboarding dihapus";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task FillCandidates()
        {
            ViewBag.Candidates = new SelectList(await _db.RecruitmentCandidates.OrderBy(c => c.CandidateName).ToListAsync(), "Id", "CandidateName");
        }
    }
}
