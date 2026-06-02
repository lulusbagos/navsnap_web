using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie", Roles = "Administrator,Pengawas")]
    public class CvSummaryController : Controller
    {
        private readonly AppDbContext _db;
        public CvSummaryController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            return View(await _db.CvMasterSummaries.Include(x => x.Candidate).OrderByDescending(x => x.Score).Take(300).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await FillCandidates();
            return View("Form", new CvMasterSummary());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CvMasterSummary model)
        {
            ModelState.Remove("Candidate");
            if (!ModelState.IsValid) { await FillCandidates(); return View("Form", model); }
            model.CreatedAt = DateTime.UtcNow;
            _db.CvMasterSummaries.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "CV summary ditambahkan";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var row = await _db.CvMasterSummaries.FindAsync(id);
            if (row == null) return NotFound();
            await FillCandidates();
            return View("Form", row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CvMasterSummary model)
        {
            ModelState.Remove("Candidate");
            if (!ModelState.IsValid) { await FillCandidates(); return View("Form", model); }
            var row = await _db.CvMasterSummaries.FindAsync(model.Id);
            if (row == null) return NotFound();
            row.CandidateId = model.CandidateId;
            row.CandidateName = model.CandidateName;
            row.Position = model.Position;
            row.LastEducation = model.LastEducation;
            row.YearsExperience = model.YearsExperience;
            row.Skills = model.Skills;
            row.Summary = model.Summary;
            row.Score = model.Score;
            await _db.SaveChangesAsync();
            TempData["Success"] = "CV summary diperbarui";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _db.CvMasterSummaries.FindAsync(id);
            if (row != null)
            {
                _db.CvMasterSummaries.Remove(row);
                await _db.SaveChangesAsync();
                TempData["Success"] = "CV summary dihapus";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task FillCandidates()
        {
            ViewBag.Candidates = new SelectList(await _db.RecruitmentCandidates.OrderBy(c => c.CandidateName).ToListAsync(), "Id", "CandidateName");
        }
    }
}
