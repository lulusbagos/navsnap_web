using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using NavSnap.Models.ViewModels;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie", Roles = "Administrator")]
    public class MUserController : Controller
    {
        private readonly AppDbContext _db;

        public MUserController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _db.MUsers
                .OrderBy(m => m.Nrp)
                .ToListAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new MUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MUserViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Password))
                ModelState.AddModelError("Password", "Password wajib diisi untuk user baru");

            if (!ModelState.IsValid)
                return View(vm);

            if (await _db.MUsers.AnyAsync(x => x.Nrp == vm.Nrp))
                ModelState.AddModelError("Nrp", "NRP sudah digunakan");

            if (await _db.MUsers.AnyAsync(x => x.User == vm.User))
                ModelState.AddModelError("User", "Username sudah digunakan");

            if (!ModelState.IsValid)
                return View(vm);

            var entity = new MUser
            {
                Nrp = vm.Nrp.Trim(),
                User = vm.User.Trim(),
                Password = vm.Password!,
                TerakhirLogin = vm.TerakhirLogin,
                Status = vm.Status,
                PathFotoProfile = string.IsNullOrWhiteSpace(vm.PathFotoProfile) ? null : vm.PathFotoProfile.Trim()
            };

            _db.MUsers.Add(entity);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Data user mobile berhasil dibuat";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var entity = await _db.MUsers.FindAsync(id);
            if (entity == null) return NotFound();

            var vm = new MUserViewModel
            {
                Nrp = entity.Nrp,
                User = entity.User,
                TerakhirLogin = entity.TerakhirLogin,
                Status = entity.Status,
                PathFotoProfile = entity.PathFotoProfile
            };
            return View("Create", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MUserViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("Create", vm);

            var entity = await _db.MUsers.FindAsync(vm.Nrp);
            if (entity == null) return NotFound();

            if (await _db.MUsers.AnyAsync(x => x.User == vm.User && x.Nrp != vm.Nrp))
                ModelState.AddModelError("User", "Username sudah digunakan");

            if (!ModelState.IsValid)
                return View("Create", vm);

            entity.User = vm.User.Trim();
            if (!string.IsNullOrWhiteSpace(vm.Password))
                entity.Password = vm.Password;
            entity.TerakhirLogin = vm.TerakhirLogin;
            entity.Status = vm.Status;
            entity.PathFotoProfile = string.IsNullOrWhiteSpace(vm.PathFotoProfile) ? null : vm.PathFotoProfile.Trim();

            await _db.SaveChangesAsync();
            TempData["Success"] = "Data user mobile berhasil diperbarui";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Index));
            var entity = await _db.MUsers.FindAsync(id);
            if (entity != null)
            {
                _db.MUsers.Remove(entity);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Data user mobile dihapus";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
