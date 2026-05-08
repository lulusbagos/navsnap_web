using Microsoft.EntityFrameworkCore;
using NavSnap.Data;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllersWithViews();

var connStr =
    builder.Configuration.GetConnectionString("Postgres")
    ?? builder.Configuration["NAVSNAP_POSTGRES"]
    ?? Environment.GetEnvironmentVariable("NAVSNAP_POSTGRES");

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
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// --- Middleware ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// --- Seed DB ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

var bindUrl =
    builder.Configuration["ASPNETCORE_URLS"]
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
    ?? "http://0.0.0.0:5164";

app.Run(bindUrl);
