using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using NavSnap.Models.ViewModels;
using System.Security.Claims;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie")]
    public class CheckpointController : Controller
    {
        private readonly AppDbContext _db;

        public CheckpointController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _db.Checkpoints
                .Include(c => c.Creator)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(list);
        }

        public IActionResult Map()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllJson()
        {
            var data = await _db.Checkpoints
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    c.Id,
                    c.CheckpointName,
                    c.Address,
                    c.Latitude,
                    c.Longitude,
                    c.RadiusMeters
                }).ToListAsync();
            return Json(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CheckpointViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckpointViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var entity = new Checkpoint
            {
                CheckpointName = vm.CheckpointName,
                Address = vm.Address,
                Latitude = vm.Latitude,
                Longitude = vm.Longitude,
                RadiusMeters = vm.RadiusMeters,
                IsActive = vm.IsActive,
                CreatedBy = userId
            };
            _db.Checkpoints.Add(entity);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Checkpoint berhasil ditambahkan";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _db.Checkpoints.FindAsync(id);
            if (entity == null) return NotFound();
            var vm = new CheckpointViewModel
            {
                Id = entity.Id,
                CheckpointName = entity.CheckpointName,
                Address = entity.Address,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                RadiusMeters = entity.RadiusMeters,
                IsActive = entity.IsActive
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CheckpointViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var entity = await _db.Checkpoints.FindAsync(vm.Id);
            if (entity == null) return NotFound();

            entity.CheckpointName = vm.CheckpointName;
            entity.Address = vm.Address;
            entity.Latitude = vm.Latitude;
            entity.Longitude = vm.Longitude;
            entity.RadiusMeters = vm.RadiusMeters;
            entity.IsActive = vm.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Checkpoint berhasil diperbarui";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Checkpoints.FindAsync(id);
            if (entity != null)
            {
                _db.Checkpoints.Remove(entity);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Checkpoint dihapus";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
