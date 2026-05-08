using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using NavSnap.Models.ViewModels;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie", Roles = "Administrator")]
    public class RoleController : Controller
    {
        private readonly AppDbContext _db;

        public RoleController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _db.Roles
                .Include(r => r.RoleMenus)
                .Include(r => r.UserRoles)
                .OrderBy(r => r.Id)
                .ToListAsync();
            return View(roles);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new RoleViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var role = new Role { RoleName = vm.RoleName, Description = vm.Description, IsActive = vm.IsActive };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Role berhasil dibuat";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _db.Roles.FindAsync(id);
            if (role == null) return NotFound();

            return View(new RoleViewModel
            {
                Id = role.Id,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var role = await _db.Roles.FindAsync(vm.Id);
            if (role == null) return NotFound();

            role.RoleName = vm.RoleName;
            role.Description = vm.Description;
            role.IsActive = vm.IsActive;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Role diperbarui";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _db.Roles.FindAsync(id);
            if (role != null)
            {
                _db.Roles.Remove(role);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Role dihapus";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---- Menu Access per Role ----
        [HttpGet]
        public async Task<IActionResult> AccessMenu(int? id)
        {
            if (!id.HasValue)
            {
                TempData["Error"] = "Role tidak valid";
                return RedirectToAction(nameof(Index));
            }

            var role = await _db.Roles
                .Include(r => r.RoleMenus)
                .FirstOrDefaultAsync(r => r.Id == id.Value);
            if (role == null)
            {
                TempData["Error"] = "Role tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var allMenus = await _db.Menus.Where(m => m.IsActive).OrderBy(m => m.SortOrder).ToListAsync();
            var assigned = role.RoleMenus.Select(rm => rm.MenuId).ToHashSet();

            ViewBag.Role = role;
            ViewBag.AllMenus = allMenus;
            ViewBag.Assigned = assigned;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMenuAccess(int roleId, List<int>? menuIds)
        {
            var roleExists = await _db.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
            {
                TempData["Error"] = "Role tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var selectedMenuIds = (menuIds ?? new List<int>())
                .Distinct()
                .ToList();

            var validMenuIds = selectedMenuIds.Count == 0
                ? new List<int>()
                : await _db.Menus
                    .Where(m => m.IsActive && selectedMenuIds.Contains(m.Id))
                    .Select(m => m.Id)
                    .ToListAsync();

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var existing = await _db.RoleMenus.Where(rm => rm.RoleId == roleId).ToListAsync();
                _db.RoleMenus.RemoveRange(existing);
                await _db.SaveChangesAsync();

                foreach (var mid in validMenuIds)
                    _db.RoleMenus.Add(new RoleMenu { RoleId = roleId, MenuId = mid });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Gagal menyimpan akses menu";
                return RedirectToAction(nameof(AccessMenu), new { id = roleId });
            }

            TempData["Success"] = "Akses menu disimpan";
            return RedirectToAction(nameof(Index));
        }
    }
}
