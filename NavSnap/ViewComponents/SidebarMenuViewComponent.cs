using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using System.Security.Claims;

namespace NavSnap.ViewComponents
{
    public class SidebarMenuViewComponent : ViewComponent
    {
        private readonly AppDbContext _db;

        public SidebarMenuViewComponent(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return View(new List<Menu>());

            int userId = int.Parse(userIdClaim);

            var roleIds = await _db.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            // Administrator sees all menus
            List<Menu> menus;
            var isAdmin = await _db.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && ur.Role.RoleName == "Administrator");

            if (isAdmin)
            {
                menus = await _db.Menus
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.SortOrder)
                    .ToListAsync();
            }
            else
            {
                var menuIds = await _db.RoleMenus
                    .Where(rm => roleIds.Contains(rm.RoleId))
                    .Select(rm => rm.MenuId)
                    .Distinct()
                    .ToListAsync();

                menus = await _db.Menus
                    .Where(m => m.IsActive && menuIds.Contains(m.Id))
                    .OrderBy(m => m.SortOrder)
                    .ToListAsync();
            }

            return View(menus);
        }
    }
}
