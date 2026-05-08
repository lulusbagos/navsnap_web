using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using NavSnap.Models.ViewModels;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie", Roles = "Administrator")]
    public class UserController : Controller
    {
        private readonly AppDbContext _db;

        public UserController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _db.Roles.Where(r => r.IsActive).ToListAsync();
            return View(new UserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel vm)
        {
            if (string.IsNullOrEmpty(vm.Password))
                ModelState.AddModelError("Password", "Password wajib diisi untuk user baru");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _db.Roles.Where(r => r.IsActive).ToListAsync();
                return View(vm);
            }

            if (await _db.Users.AnyAsync(u => u.Username == vm.Username))
            {
                ModelState.AddModelError("Username", "Username sudah digunakan");
                ViewBag.Roles = await _db.Roles.Where(r => r.IsActive).ToListAsync();
                return View(vm);
            }

            var user = new User
            {
                Username = vm.Username,
                Password = vm.Password!,
                FullName = vm.FullName,
                Email = vm.Email,
                Phone = vm.Phone,
                IsActive = vm.IsActive
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            foreach (var rid in vm.SelectedRoleIds)
                _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = rid });

            await _db.SaveChangesAsync();
            TempData["Success"] = "User berhasil dibuat";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _db.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            ViewBag.Roles = await _db.Roles.Where(r => r.IsActive).ToListAsync();
            return View(new UserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                IsActive = user.IsActive,
                SelectedRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _db.Roles.Where(r => r.IsActive).ToListAsync();
                return View(vm);
            }

            var user = await _db.Users.FindAsync(vm.Id);
            if (user == null) return NotFound();

            if (await _db.Users.AnyAsync(u => u.Username == vm.Username && u.Id != vm.Id))
            {
                ModelState.AddModelError("Username", "Username sudah digunakan");
                ViewBag.Roles = await _db.Roles.Where(r => r.IsActive).ToListAsync();
                return View(vm);
            }

            user.Username = vm.Username;
            if (!string.IsNullOrEmpty(vm.Password))
                user.Password = vm.Password;
            user.FullName = vm.FullName;
            user.Email = vm.Email;
            user.Phone = vm.Phone;
            user.IsActive = vm.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            // Update roles
            var existing = await _db.UserRoles.Where(ur => ur.UserId == vm.Id).ToListAsync();
            _db.UserRoles.RemoveRange(existing);
            foreach (var rid in vm.SelectedRoleIds)
                _db.UserRoles.Add(new UserRole { UserId = vm.Id, RoleId = rid });

            await _db.SaveChangesAsync();
            TempData["Success"] = "User berhasil diperbarui";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
                TempData["Success"] = "User dihapus";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
