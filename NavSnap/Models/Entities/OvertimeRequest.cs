using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_overtime_requests")]
    public class OvertimeRequest
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("request_date")]
        public DateOnly RequestDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Column("overtime_date")]
        public DateOnly OvertimeDate { get; set; }

        [Column("hours")]
        public decimal Hours { get; set; }

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
