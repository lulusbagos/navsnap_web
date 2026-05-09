using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavSnap.Data;
using NavSnap.Models.Entities;
using System.Security.Claims;

namespace NavSnap.Controllers
{
    [Authorize(AuthenticationSchemes = "NavSnapCookie")]
    [Authorize(Roles = "Administrator,Pengawas")]
    public class ApprovalController : Controller
    {
        private readonly AppDbContext _db;
        public ApprovalController(AppDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pendingSubmissions = await _db.StoreSubmissions
                .Include(x => x.SalesUser)
                .Where(x => x.Status == "pending")
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToListAsync();

            var leaveQ = _db.LeaveRequests.Include(x => x.User).AsQueryable();
            var overtimeQ = _db.OvertimeRequests.Include(x => x.User).AsQueryable();

            if (User.IsInRole("Pengawas") && !User.IsInRole("Administrator"))
            {
                leaveQ = leaveQ.Where(x => x.Status == "pending_supervisor");
                overtimeQ = overtimeQ.Where(x => x.Status == "pending_supervisor");
            }
            else
            {
                leaveQ = leaveQ.Where(x => x.Status == "pending_supervisor" || x.Status == "pending_hr");
                overtimeQ = overtimeQ.Where(x => x.Status == "pending_supervisor" || x.Status == "pending_hr");
            }

            ViewBag.PendingSubmissions = pendingSubmissions;
            ViewBag.PendingLeaves = await leaveQ.OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync();
            ViewBag.PendingOvertimes = await overtimeQ.OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewSubmission(int id, string actionType, string? adminNote, DateOnly? visitDate)
        {
            var item = await _db.StoreSubmissions.FindAsync(id);
            if (item == null) return RedirectToAction(nameof(Index));

            var uid = TryUserId();
            if (!uid.HasValue) return RedirectToAction(nameof(Index));

            if (actionType == "reject")
            {
                item.Status = "rejected";
                item.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? "Ditolak" : adminNote.Trim();
                item.ApprovedBy = uid.Value;
                item.ApprovedAt = DateTime.UtcNow;
                item.UpdatedAt = DateTime.UtcNow;
                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = uid.Value,
                    Module = "ApprovalCenter",
                    Action = "Reject Store Submission",
                    EntityName = "StoreSubmission",
                    EntityId = item.Id,
                    Description = $"Pengajuan toko {item.StoreName} ditolak."
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = "Pengajuan toko ditolak.";
                return RedirectToAction(nameof(Index));
            }

            var checkpoint = await _db.Checkpoints.FirstOrDefaultAsync(c => c.CheckpointName == item.StoreName);
            if (checkpoint == null)
            {
                checkpoint = new Checkpoint
                {
                    CheckpointName = item.StoreName,
                    Address = item.Address,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    RadiusMeters = item.RadiusMeters,
                    IsActive = true,
                    CreatedBy = uid.Value,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Checkpoints.Add(checkpoint);
                await _db.SaveChangesAsync();
            }

            var targetDate = visitDate ?? item.ProposedVisitDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            var startTime = "09:00";
            var endTime = "10:00";
            var hasSchedule = await _db.SalesVisitSchedules.AnyAsync(x =>
                x.UserId == item.SalesUserId &&
                x.CheckpointId == checkpoint.Id &&
                x.ScheduleDate == targetDate &&
                x.Status != "canceled");
            if (!hasSchedule)
            {
                _db.SalesVisitSchedules.Add(new SalesVisitSchedule
                {
                    UserId = item.SalesUserId,
                    CheckpointId = checkpoint.Id,
                    ScheduleDate = targetDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    Status = "planned",
                    SourceSubmissionId = item.Id,
                    CreatedBy = uid.Value,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var hasVisit = await _db.SalesVisits.AnyAsync(v =>
                v.UserId == item.SalesUserId &&
                v.CheckpointId == checkpoint.Id &&
                v.VisitDate == targetDate);
            if (!hasVisit)
            {
                _db.SalesVisits.Add(new SalesVisit
                {
                    UserId = item.SalesUserId,
                    CheckpointId = checkpoint.Id,
                    VisitDate = targetDate,
                    Status = "pending",
                    IsMandatory = true,
                    SourceSubmissionId = item.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            item.Status = "approved";
            item.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? "Disetujui" : adminNote.Trim();
            item.ProposedVisitDate = targetDate;
            item.ApprovedCheckpointId = checkpoint.Id;
            item.ApprovedBy = uid.Value;
            item.ApprovedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = uid.Value,
                Module = "ApprovalCenter",
                Action = "Approve Store Submission",
                EntityName = "StoreSubmission",
                EntityId = item.Id,
                Description = $"Pengajuan {item.StoreName} disetujui, jadwal {targetDate:yyyy-MM-dd}."
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = "Pengajuan toko disetujui dan jadwal kunjungan dibuat.";
            return RedirectToAction(nameof(Index));
        }

        private int? TryUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
