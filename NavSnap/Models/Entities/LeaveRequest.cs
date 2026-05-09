using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_leave_requests")]
    public class LeaveRequest
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("request_date")]
        public DateOnly RequestDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Column("leave_date_from")]
        public DateOnly LeaveDateFrom { get; set; }

        [Column("leave_date_to")]
        public DateOnly LeaveDateTo { get; set; }

        [Column("leave_type")]
        [MaxLength(30)]
        public string LeaveType { get; set; } = "izin";

        [Column("reason")]
        [MaxLength(500)]
        public string? Reason { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        [Column("approved_by")]
        public int? ApprovedBy { get; set; }

        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [Column("approval_stage")]
        public int ApprovalStage { get; set; } = 1;

        [Column("supervisor_approved_by")]
        public int? SupervisorApprovedBy { get; set; }

        [Column("supervisor_approved_at")]
        public DateTime? SupervisorApprovedAt { get; set; }

        [Column("hr_approved_by")]
        public int? HrApprovedBy { get; set; }

        [Column("hr_approved_at")]
        public DateTime? HrApprovedAt { get; set; }

        [Column("sla_due_at")]
        public DateTime? SlaDueAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("ApprovedBy")]
        public User? Approver { get; set; }
    }
}
