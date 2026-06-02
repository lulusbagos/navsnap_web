using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using NavSnap.Data;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllersWithViews();

var connStr = builder.Configuration.GetConnectionString("Postgres");
if (string.IsNullOrWhiteSpace(connStr))
{
    connStr = builder.Configuration["NAVSNAP_POSTGRES"];
}
if (string.IsNullOrWhiteSpace(connStr))
{
    connStr = Environment.GetEnvironmentVariable("NAVSNAP_POSTGRES");
}

if (string.IsNullOrWhiteSpace(connStr))
{
    throw new InvalidOperationException("Connection string 'Postgres' belum diset. Gunakan env var NAVSNAP_POSTGRES.");
}
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connStr));

builder.Services.AddAuthentication("NavSnapCookie")
    .AddCookie("NavSnapCookie", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "NavSnap.Auth";
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var userIdText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var sessionVersionText = context.Principal?.FindFirstValue("SessionVersion");
                if (!int.TryParse(userIdText, out var userId) || !int.TryParse(sessionVersionText, out var sessionVersion))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync("NavSnapCookie");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var user = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.IsActive, u.SessionVersion })
                    .FirstOrDefaultAsync();

                if (user == null || !user.IsActive || user.SessionVersion != sessionVersion)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync("NavSnapCookie");
                }
            }
        };
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// --- Optional startup DB seed / schema sync (disabled by default for faster startup) ---
var runSeeder = builder.Configuration.GetValue<bool?>("RunSeeder") ?? false;
var runSeederEnv = Environment.GetEnvironmentVariable("NAVSNAP_RUN_SEEDER");
if (!string.IsNullOrWhiteSpace(runSeederEnv) &&
    (runSeederEnv == "1" || runSeederEnv.Equals("true", StringComparison.OrdinalIgnoreCase)))
{
    runSeeder = true;
}

if (runSeeder)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

// --- Middleware ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_HTTPS_PORT"]))
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var bindUrl =
    builder.Configuration["ASPNETCORE_URLS"]
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
    ?? "http://0.0.0.0:5164";

app.Run(bindUrl);
