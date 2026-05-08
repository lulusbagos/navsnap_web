using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using NavSnap.Models.ViewModels;
using System.Security.Claims;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie", Roles = "Administrator,Pengawas")]
    public class SalesTargetController : Controller
    {
        private readonly AppDbContext _db;

        public SalesTargetController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = await BuildPageVmAsync(new SalesTargetBatchViewModel
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(6)),
                TargetStores = 8
            });
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBatch(SalesTargetBatchViewModel form)
        {
            if (form.EndDate < form.StartDate)
                ModelState.AddModelError("EndDate", "Tanggal selesai harus >= tanggal mulai");

            var maxDays = form.EndDate.DayNumber - form.StartDate.DayNumber + 1;
            if (maxDays > 31)
                ModelState.AddModelError("EndDate", "Rentang maksimal 31 hari sekali set");

            if (form.SelectedSalesIds == null || form.SelectedSalesIds.Count == 0)
                ModelState.AddModelError("SelectedSalesIds", "Pilih minimal 1 sales");

            if (!ModelState.IsValid)
            {
                var invalidVm = await BuildPageVmAsync(form);
                return View("Index", invalidVm);
            }

            var selectedSalesIds = form.SelectedSalesIds ?? new List<int>();

            var validSalesIds = await _db.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => selectedSalesIds.Contains(ur.UserId) && ur.Role.RoleName == "Sales")
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            if (validSalesIds.Count == 0)
            {
                ModelState.AddModelError("SelectedSalesIds", "Sales tidak valid");
                var invalidVm = await BuildPageVmAsync(form);
                return View("Index", invalidVm);
            }

            int? creatorId = null;
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out var parsedId))
                creatorId = parsedId;

            var dates = Enumerable.Range(0, maxDays)
                .Select(i => form.StartDate.AddDays(i))
                .ToList();

            var existing = await _db.SalesDailyTargets
                .Where(t => validSalesIds.Contains(t.UserId) && dates.Contains(t.TargetDate))
                .ToListAsync();

            foreach (var salesId in validSalesIds)
            {
                foreach (var date in dates)
                {
                    var row = existing.FirstOrDefault(x => x.UserId == salesId && x.TargetDate == date);
                    if (row == null)
                    {
                        _db.SalesDailyTargets.Add(new SalesDailyTarget
                        {
                            UserId = salesId,
                            TargetDate = date,
                            TargetStores = form.TargetStores,
                            Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim(),
                            CreatedBy = creatorId
                        });
                    }
                    else
                    {
                        row.TargetStores = form.TargetStores;
                        row.Notes = string.IsNullOrWhiteSpace(form.Notes) ? row.Notes : form.Notes.Trim();
                        row.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Target harian sales berhasil disimpan";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var target = await _db.SalesDailyTargets.FindAsync(id);
            if (target != null)
            {
                _db.SalesDailyTargets.Remove(target);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Target harian dihapus";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<SalesTargetPageViewModel> BuildPageVmAsync(SalesTargetBatchViewModel form)
        {
            var salesList = await _db.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.RoleName == "Sales")
                .Select(ur => new SalesTargetSalesItem
                {
                    UserId = ur.UserId,
                    FullName = ur.User.FullName,
                    IsActive = ur.User.IsActive
                })
                .Distinct()
                .OrderBy(s => s.FullName)
                .ToListAsync();

            var start = DateOnly.FromDateTime(DateTime.Today.AddDays(-2));
            var end = DateOnly.FromDateTime(DateTime.Today.AddDays(21));

            var targets = await _db.SalesDailyTargets
                .Include(t => t.User)
                .Where(t => t.TargetDate >= start && t.TargetDate <= end)
                .OrderBy(t => t.TargetDate)
                .ThenBy(t => t.User.FullName)
                .Select(t => new SalesTargetListItem
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    SalesName = t.User.FullName,
                    TargetDate = t.TargetDate,
                    TargetStores = t.TargetStores,
                    Notes = t.Notes
                })
                .ToListAsync();

            return new SalesTargetPageViewModel
            {
                Form = form,
                SalesList = salesList,
                Targets = targets
            };
        }
    }
}
