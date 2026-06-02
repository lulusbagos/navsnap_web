using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using NavSnap.Models.ViewModels;
using System.Security.Claims;

namespace NavSnap.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == model.Username
                                       && u.Password == model.Password
                                       && u.IsActive);

            if (user == null)
            {
                var loginName = string.IsNullOrWhiteSpace(model.Username) ? "-" : model.Username.Trim();
                await AddAccountLogAsync(null, "Login Failed", $"Percobaan login gagal untuk username {loginName}.");
                await _db.SaveChangesAsync();
                ModelState.AddModelError("", "Username atau password salah");
                return View(model);
            }

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

            var identity = new ClaimsIdentity(claims, "NavSnapCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("NavSnapCookie", principal, new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

            user.LastLoginAt = DateTime.UtcNow;
            await AddAccountLogAsync(user.Id, "Login Success", "User berhasil login ke aplikasi web.");
            await _db.SaveChangesAsync();

            // Force landing page to application root after successful login.
            return Redirect("/");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idText, out var userId))
            {
                await AddAccountLogAsync(userId, "Logout", "User keluar dari aplikasi web.");
                await _db.SaveChangesAsync();
            }
            await HttpContext.SignOutAsync("NavSnapCookie");
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task AddAccountLogAsync(int? userId, string action, string description)
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

        private static string NormalizeTheme(string? theme)
        {
            var allowed = new[] { "ocean", "midnight", "emerald", "graphite" };
            return !string.IsNullOrWhiteSpace(theme) && allowed.Contains(theme, StringComparer.OrdinalIgnoreCase)
                ? theme.ToLowerInvariant()
                : "ocean";
        }
    }
}
