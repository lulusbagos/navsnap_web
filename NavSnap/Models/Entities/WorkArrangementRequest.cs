using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_work_arrangement_requests")]
    public class WorkArrangementRequest
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("request_date")]
        public DateOnly RequestDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Column("work_date_from")]
        public DateOnly WorkDateFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Column("work_date_to")]
        public DateOnly WorkDateTo { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Column("arrangement_type")]
        [MaxLength(10)]
        public string ArrangementType { get; set; } = "WFH";

        [Column("location")]
        [MaxLength(200)]
        public string? Location { get; set; }

        [Column("reason")]
        [MaxLength(500)]
        public string? Reason { get; set; }

        [Column("status")]
        [MaxLength(30)]
        public string Status { get; set; } = "pending";

        [Column("approved_by")]
        public int? ApprovedBy { get; set; }

        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public User? Approver { get; set; }
    }
}
