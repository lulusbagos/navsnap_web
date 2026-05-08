using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using System.Text;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie")]
    public class SalesTrackingController : Controller
    {
        private readonly AppDbContext _db;

        public SalesTrackingController(AppDbContext db)
        {
            _db = db;
        }

        // Live Tracking map
        public IActionResult Index()
        {
            return View();
        }

        // Visit history with filter
        public async Task<IActionResult> History(int? userId, string? date, string? dateFrom, string? dateTo)
        {
            var query = _db.SalesVisits
                .Include(v => v.User)
                .Include(v => v.Checkpoint)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(v => v.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var fromDate))
                query = query.Where(v => v.VisitDate >= fromDate);

            if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var toDate))
                query = query.Where(v => v.VisitDate <= toDate);

            if (string.IsNullOrWhiteSpace(dateFrom) && string.IsNullOrWhiteSpace(dateTo))
            {
                DateOnly filterDate;
                if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out filterDate))
                    query = query.Where(v => v.VisitDate == filterDate);
                else
                    query = query.Where(v => v.VisitDate == DateOnly.FromDateTime(DateTime.Today));
            }

            var visits = await query.OrderByDescending(v => v.CreatedAt).ToListAsync();

            var salesList = await _db.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.RoleName == "Sales")
                .Select(ur => ur.User)
                .Distinct()
                .ToListAsync();

            ViewBag.SalesList = salesList;
            ViewBag.SelectedUserId = userId;
            ViewBag.SelectedDate = date ?? DateTime.Today.ToString("yyyy-MM-dd");
            ViewBag.SelectedDateFrom = dateFrom ?? string.Empty;
            ViewBag.SelectedDateTo = dateTo ?? string.Empty;

            return View(visits);
        }

        [HttpGet]
        [Authorize(Roles = "Administrator,Pengawas")]
        public async Task<IActionResult> ExportHistory(int? userId, string? date, string? dateFrom, string? dateTo)
        {
            var query = _db.SalesVisits
                .Include(v => v.User)
                .Include(v => v.Checkpoint)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(v => v.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var fromDate))
                query = query.Where(v => v.VisitDate >= fromDate);

            if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var toDate))
                query = query.Where(v => v.VisitDate <= toDate);

            if (string.IsNullOrWhiteSpace(dateFrom) && string.IsNullOrWhiteSpace(dateTo))
            {
                DateOnly filterDate;
                if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out filterDate))
                    query = query.Where(v => v.VisitDate == filterDate);
                else
                    query = query.Where(v => v.VisitDate == DateOnly.FromDateTime(DateTime.Today));
            }

            var data = await query.OrderByDescending(v => v.CreatedAt).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Tanggal,Sales,Checkpoint,Lokasi,Jam Kunjung,Status,Keterangan,Foto,Poin");

            foreach (var v in data)
            {
                var lokasi = $"{v.Checkpoint.CheckpointName} ({v.Checkpoint.Address ?? "-"})";
                var jam = v.ArrivedAt.HasValue ? v.ArrivedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : "-";
                var photo = v.VisitPhotoPath ?? "-";
                var ket = v.Notes ?? "-";
                sb.AppendLine(string.Join(",",
                    Csv(v.VisitDate.ToString("yyyy-MM-dd")),
                    Csv(v.User.FullName),
                    Csv(v.Checkpoint.CheckpointName),
                    Csv(lokasi),
                    Csv(jam),
                    Csv(v.Status),
                    Csv(ket),
                    Csv(photo),
                    Csv(v.PointEarned.ToString())
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"laporan_kunjungan_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        // --- API for real-time data ---

        [HttpGet]
        public async Task<IActionResult> GetSalesPositions()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-60);
            var data = await _db.GpsLogs
                .Include(g => g.User)
                .Where(g => g.LoggedAt >= cutoff)
                .GroupBy(g => g.UserId)
                .Select(g => new
                {
                    userId = g.Key,
                    fullName = g.First().User.FullName,
                    latitude = g.OrderByDescending(x => x.LoggedAt).First().Latitude,
                    longitude = g.OrderByDescending(x => x.LoggedAt).First().Longitude,
                    lastSeen = g.Max(x => x.LoggedAt)
                }).ToListAsync();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetCheckpointStatuses(string? date)
        {
            DateOnly filterDate = string.IsNullOrEmpty(date)
                ? DateOnly.FromDateTime(DateTime.Today)
                : DateOnly.Parse(date);

            var checkpoints = await _db.Checkpoints.Where(c => c.IsActive).ToListAsync();
            var visits = await _db.SalesVisits
                .Where(v => v.VisitDate == filterDate)
                .Include(v => v.User)
                .ToListAsync();

            var result = checkpoints.Select(c =>
            {
                var cvs = visits.Where(v => v.CheckpointId == c.Id).ToList();
                return new
                {
                    id = c.Id,
                    name = c.CheckpointName,
                    latitude = c.Latitude,
                    longitude = c.Longitude,
                    radius = c.RadiusMeters,
                    arrivedCount = cvs.Count(v => v.Status == "arrived" || v.Status == "completed"),
                    pendingCount = cvs.Count(v => v.Status == "pending"),
                    visits = cvs.Select(v => new
                    {
                        salesName = v.User.FullName,
                        status = v.Status,
                        arrivedAt = v.ArrivedAt
                    })
                };
            });

            return Json(result);
        }

        // API: Mobile posts GPS position
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PostGps([FromBody] GpsPayload payload)
        {
            if (payload == null || payload.UserId <= 0)
                return BadRequest("Invalid payload");

            var log = new GpsLog
            {
                UserId = payload.UserId,
                Latitude = payload.Latitude,
                Longitude = payload.Longitude,
                Accuracy = payload.Accuracy,
                LoggedAt = DateTime.UtcNow
            };
            _db.GpsLogs.Add(log);

            // Auto-check: arrived at checkpoints within radius?
            var checkpoints = await _db.Checkpoints.Where(c => c.IsActive).ToListAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);
            foreach (var cp in checkpoints)
            {
                double dist = HaversineMeters(payload.Latitude, payload.Longitude, cp.Latitude, cp.Longitude);
                if (dist <= cp.RadiusMeters)
                {
                    var visit = await _db.SalesVisits.FirstOrDefaultAsync(v =>
                        v.UserId == payload.UserId &&
                        v.CheckpointId == cp.Id &&
                        v.VisitDate == today &&
                        v.Status == "pending");

                    if (visit != null)
                    {
                        visit.Status = "arrived";
                        visit.ArrivedAt = DateTime.UtcNow;
                        visit.LatitudeArrived = payload.Latitude;
                        visit.LongitudeArrived = payload.Longitude;
                        if (!string.IsNullOrWhiteSpace(payload.Notes))
                            visit.Notes = payload.Notes.Trim();
                        if (!string.IsNullOrWhiteSpace(payload.PhotoPath))
                            visit.VisitPhotoPath = payload.PhotoPath.Trim();

                        // Game scoring: mandatory visit gives higher points.
                        visit.PointEarned = visit.IsMandatory ? 15 : 10;
                    }
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { status = "ok" });
        }

        // Create visit assignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Pengawas")]
        public async Task<IActionResult> AssignVisit(int userId, int checkpointId, string visitDate)
        {
            if (!DateOnly.TryParse(visitDate, out var date))
                return BadRequest("Format tanggal tidak valid");

            var exists = await _db.SalesVisits.AnyAsync(v =>
                v.UserId == userId && v.CheckpointId == checkpointId && v.VisitDate == date);

            if (!exists)
            {
                _db.SalesVisits.Add(new SalesVisit
                {
                    UserId = userId,
                    CheckpointId = checkpointId,
                    VisitDate = date,
                    Status = "pending"
                });
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Kunjungan berhasil ditugaskan";
            return RedirectToAction("History");
        }

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static string Csv(string value)
        {
            var safe = value.Replace("\"", "\"\"");
            return $"\"{safe}\"";
        }
    }

    public class GpsPayload
    {
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public string? Notes { get; set; }
        public string? PhotoPath { get; set; }
    }
}
