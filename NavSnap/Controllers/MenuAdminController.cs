using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie", Roles = "Administrator")]
    [Route("[controller]/[action]")]
    public class MenuAdminController : Controller
    {
        private readonly AppDbContext _db;

        public MenuAdminController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var menus = await _db.Menus
                .Include(m => m.Parent)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();
            return View(menus);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Parents = await _db.Menus.Where(m => m.ParentId == null).OrderBy(m => m.SortOrder).ToListAsync();
            return View(new Menu());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Menu menu)
        {
            ModelState.Remove("Parent");
            ModelState.Remove("Children");
            ModelState.Remove("RoleMenus");
            if (!ModelState.IsValid)
            {
                ViewBag.Parents = await _db.Menus.Where(m => m.ParentId == null).OrderBy(m => m.SortOrder).ToListAsync();
                return View(menu);
            }
            menu.CreatedAt = DateTime.UtcNow;
            _db.Menus.Add(menu);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Menu ditambahkan";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var menu = await _db.Menus.FindAsync(id);
            if (menu == null) return NotFound();
            ViewBag.Parents = await _db.Menus.Where(m => m.ParentId == null && m.Id != id).OrderBy(m => m.SortOrder).ToListAsync();
            return View(menu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Menu menu)
        {
            ModelState.Remove("Parent");
            ModelState.Remove("Children");
            ModelState.Remove("RoleMenus");
            if (!ModelState.IsValid)
            {
                ViewBag.Parents = await _db.Menus.Where(m => m.ParentId == null && m.Id != menu.Id).OrderBy(m => m.SortOrder).ToListAsync();
                return View(menu);
            }
            var entity = await _db.Menus.FindAsync(menu.Id);
            if (entity == null) return NotFound();
            entity.MenuName = menu.MenuName;
            entity.MenuUrl = menu.MenuUrl;
            entity.IconClass = menu.IconClass;
            entity.ParentId = menu.ParentId;
            entity.SortOrder = menu.SortOrder;
            entity.IsActive = menu.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Menu diperbarui";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Menus.FindAsync(id);
            if (entity != null)
            {
                _db.Menus.Remove(entity);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Menu dihapus";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
