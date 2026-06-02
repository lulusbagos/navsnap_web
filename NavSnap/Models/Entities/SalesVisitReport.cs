using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_sales_visit_reports")]
    public class SalesVisitReport
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("checkpoint_id")]
        public int CheckpointId { get; set; }

        [Column("report_date")]
        public DateOnly ReportDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Column("visit_status")]
        [MaxLength(30)]
        public string VisitStatus { get; set; } = "completed";

        [Column("outcome")]
        [MaxLength(200)]
        public string? Outcome { get; set; }

        [Column("notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Checkpoint? Checkpoint { get; set; }
    }
}
