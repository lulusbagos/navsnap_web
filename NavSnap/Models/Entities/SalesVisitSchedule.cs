using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_sales_visit_schedules")]
    public class SalesVisitSchedule
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("checkpoint_id")]
        public int CheckpointId { get; set; }

        [Column("schedule_date")]
        public DateOnly ScheduleDate { get; set; }

        [Column("start_time")]
        [MaxLength(5)]
        public string StartTime { get; set; } = "09:00";

        [Column("end_time")]
        [MaxLength(5)]
        public string EndTime { get; set; } = "10:00";

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "planned";

        [Column("source_submission_id")]
        public int? SourceSubmissionId { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("CheckpointId")]
        public Checkpoint Checkpoint { get; set; } = null!;

        [ForeignKey("SourceSubmissionId")]
        public StoreSubmission? SourceSubmission { get; set; }

        [ForeignKey("CreatedBy")]
        public User? Creator { get; set; }
    }
}
