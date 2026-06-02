using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using NavSnap.Models.ViewModels;

namespace NavSnap.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private static readonly HashSet<string> AllowedThemes = new(StringComparer.OrdinalIgnoreCase)
        {
            "ocean", "midnight", "emerald", "graphite"
        };

        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProfileController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var user = await CurrentUserQuery().FirstOrDefaultAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var logs = await _db.AuditLogs
                .Where(a => a.UserId == user.Id && a.Module == "Account")
                .OrderByDescending(a => a.CreatedAt)
                .Take(12)
                .ToListAsync();

            return View(new ProfileViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                ProfilePhotoPath = user.ProfilePhotoPath,
                ThemePreference = NormalizeTheme(user.ThemePreference),
                LastLoginAt = user.LastLoginAt,
                Roles = user.UserRoles.Select(ur => ur.Role.RoleName).OrderBy(x => x).ToList(),
                SecurityLogs = logs
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProfileUpdateViewModel model)
        {
            var user = await CurrentUserQuery().FirstOrDefaultAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Profil belum valid. Periksa kembali nama, email, atau nomor telepon.";
                return RedirectToAction(nameof(Index));
            }

            user.FullName = model.FullName.Trim();
            user.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            user.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

            if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
            {
                var photoPath = await SaveProfilePhotoAsync(model.ProfilePhoto, user.Id);
                if (photoPath == null)
                {
                    TempData["Error"] = "Foto profil harus JPG, PNG, atau WebP dan maksimal 2 MB.";
                    return RedirectToAction(nameof(Index));
                }
                user.ProfilePhotoPath = photoPath;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await AddAccountLogAsync(user.Id, "Profile Updated", "User memperbarui informasi profil.");
            await _db.SaveChangesAsync();
            await RefreshSignInAsync(user);

            TempData["Success"] = "Profil berhasil diperbarui.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await CurrentUserQuery().FirstOrDefaultAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid || user.Password != model.CurrentPassword)
            {
                TempData["Error"] = "Password tidak valid. Pastikan password saat ini dan konfirmasi sudah benar.";
                return RedirectToAction(nameof(Index));
            }

            user.Password = model.NewPassword;
            user.SessionVersion += 1;
            user.UpdatedAt = DateTime.UtcNow;
            await AddAccountLogAsync(user.Id, "Password Changed", "Password diganti dan sesi lama otomatis dicabut.");
            await _db.SaveChangesAsync();
            await RefreshSignInAsync(user);

            TempData["Success"] = "Password berhasil diganti. Sesi lama sudah dicabut.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetTheme(string theme)
        {
            var user = await CurrentUserQuery().FirstOrDefaultAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            user.ThemePreference = NormalizeTheme(theme);
            user.UpdatedAt = DateTime.UtcNow;
            await AddAccountLogAsync(user.Id, "Theme Changed", $"Tema aplikasi diubah ke {user.ThemePreference}.");
            await _db.SaveChangesAsync();
            await RefreshSignInAsync(user);

            TempData["Success"] = "Tema aplikasi berhasil diterapkan.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeSessions()
        {
            var user = await CurrentUserQuery().FirstOrDefaultAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            user.SessionVersion += 1;
            user.UpdatedAt = DateTime.UtcNow;
            await AddAccountLogAsync(user.Id, "Sessions Revoked", "User mencabut seluruh sesi login lama.");
            await _db.SaveChangesAsync();
            await RefreshSignInAsync(user);

            TempData["Success"] = "Semua sesi lama berhasil dicabut. Sesi aktif saat ini tetap berjalan.";
            return RedirectToAction(nameof(Index));
        }

        private IQueryable<User> CurrentUserQuery()
        {
            var idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idText, out var userId))
                return _db.Users.Where(u => false);

            return _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.Id == userId && u.IsActive);
        }

        private async Task<string?> SaveProfilePhotoAsync(IFormFile file, int userId)
        {
            if (file.Length > 2 * 1024 * 1024) return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not ".jpg" and not ".jpeg" and not ".png" and not ".webp") return null;

            var dir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(dir);
            var fileName = $"user-{userId}-{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
            var fullPath = Path.Combine(dir, fileName);

            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);
            return $"/uploads/profiles/{fileName}";
        }

        private async Task AddAccountLogAsync(int userId, string action, string description)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "-";
            var ua = Request.Headers.UserAgent.ToString();
            if (ua.Length > 180) ua = ua[..180];

            await _db.AuditLogs.AddAsync(new AuditLog
            {
                UserId = userId,
                Module = "Account",
                Action = action,
                EntityName = "User",
                EntityId = userId,
                Description = $"{description} IP: {ip}. Device: {ua}",
                CreatedAt = DateTime.UtcNow
            });
        }

        private async Task RefreshSignInAsync(User user)
        {
            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new("FullName", user.FullName),
                new("ProfilePhoto", user.ProfilePhotoPath ?? string.Empty),
                new("ThemePreference", NormalizeTheme(user.ThemePreference)),
                new("SessionVersion", user.SessionVersion.ToString())
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            await HttpContext.SignInAsync("NavSnapCookie", new ClaimsPrincipal(new ClaimsIdentity(claims, "NavSnapCookie")));
        }

        private static string NormalizeTheme(string? theme)
        {
            return !string.IsNullOrWhiteSpace(theme) && AllowedThemes.Contains(theme) ? theme.ToLowerInvariant() : "ocean";
        }
    }
}
