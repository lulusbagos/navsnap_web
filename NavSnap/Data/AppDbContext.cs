using Microsoft.EntityFrameworkCore;
using NavSnap.Models.Entities;

namespace NavSnap.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<MUser> MUsers { get; set; }
        public DbSet<EmployeeProfile> EmployeeProfiles { get; set; }
        public DbSet<StoreSubmission> StoreSubmissions { get; set; }
        public DbSet<SalesDailyTarget> SalesDailyTargets { get; set; }
        public DbSet<AttendanceSetting> AttendanceSettings { get; set; }
        public DbSet<AttendanceLog> AttendanceLogs { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<OvertimeRequest> OvertimeRequests { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Checkpoint> Checkpoints { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RoleMenu> RoleMenus { get; set; }
        public DbSet<SalesVisit> SalesVisits { get; set; }
        public DbSet<GpsLog> GpsLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username).IsUnique();

            modelBuilder.Entity<MUser>()
                .HasIndex(u => u.User).IsUnique();

            modelBuilder.Entity<EmployeeProfile>()
                .HasIndex(e => e.FullName);

            modelBuilder.Entity<StoreSubmission>()
                .HasIndex(s => new { s.SalesUserId, s.Status });

            modelBuilder.Entity<SalesDailyTarget>()
                .HasIndex(t => new { t.UserId, t.TargetDate }).IsUnique();

            modelBuilder.Entity<AttendanceLog>()
                .HasIndex(t => new { t.UserId, t.AttendanceDate }).IsUnique();

            // UserRole unique pair
            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();

            // RoleMenu unique pair
            modelBuilder.Entity<RoleMenu>()
                .HasIndex(rm => new { rm.RoleId, rm.MenuId }).IsUnique();

            // Self-referencing Menu
            modelBuilder.Entity<Menu>()
                .HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> UserRoles
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Role -> UserRoles
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Role -> RoleMenus
            modelBuilder.Entity<RoleMenu>()
                .HasOne(rm => rm.Role)
                .WithMany(r => r.RoleMenus)
                .HasForeignKey(rm => rm.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Menu -> RoleMenus
            modelBuilder.Entity<RoleMenu>()
                .HasOne(rm => rm.Menu)
                .WithMany(m => m.RoleMenus)
                .HasForeignKey(rm => rm.MenuId)
                .OnDelete(DeleteBehavior.Cascade);

            // SalesVisit
            modelBuilder.Entity<SalesVisit>()
                .HasOne(sv => sv.User)
                .WithMany(u => u.SalesVisits)
                .HasForeignKey(sv => sv.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesVisit>()
                .HasOne(sv => sv.Checkpoint)
                .WithMany(c => c.SalesVisits)
                .HasForeignKey(sv => sv.CheckpointId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesVisit>()
                .HasOne(sv => sv.SourceSubmission)
                .WithMany()
                .HasForeignKey(sv => sv.SourceSubmissionId)
                .OnDelete(DeleteBehavior.SetNull);

            // GpsLog
            modelBuilder.Entity<GpsLog>()
                .HasOne(g => g.User)
                .WithMany(u => u.GpsLogs)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Checkpoint creator FK (nullable)
            modelBuilder.Entity<Checkpoint>()
                .HasOne(c => c.Creator)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MUser>()
                .HasOne(mu => mu.EmployeeProfile)
                .WithMany(ep => ep.MobileUsers)
                .HasForeignKey(mu => mu.EmployeeNrp)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<StoreSubmission>()
                .HasOne(s => s.SalesUser)
                .WithMany()
                .HasForeignKey(s => s.SalesUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StoreSubmission>()
                .HasOne(s => s.Approver)
                .WithMany()
                .HasForeignKey(s => s.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<StoreSubmission>()
                .HasOne(s => s.ApprovedCheckpoint)
                .WithMany(c => c.ApprovedSubmissions)
                .HasForeignKey(s => s.ApprovedCheckpointId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesDailyTarget>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesDailyTarget>()
                .HasOne(t => t.Creator)
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AttendanceLog>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(t => t.Approver)
                .WithMany()
                .HasForeignKey(t => t.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OvertimeRequest>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OvertimeRequest>()
                .HasOne(t => t.Approver)
                .WithMany()
                .HasForeignKey(t => t.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
