using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_sales_visits")]
    public class SalesVisit
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        [Required]
        public int UserId { get; set; }

        [Column("checkpoint_id")]
        [Required]
        public int CheckpointId { get; set; }

        [Column("visit_date")]
        [Required]
        public DateOnly VisitDate { get; set; }

        /// <summary>pending | arrived | completed | skipped</summary>
        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        [Column("arrived_at")]
        public DateTime? ArrivedAt { get; set; }

        [Column("latitude_arrived")]
        public double? LatitudeArrived { get; set; }

        [Column("longitude_arrived")]
        public double? LongitudeArrived { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("visit_photo_path")]
        [MaxLength(255)]
        public string? VisitPhotoPath { get; set; }

        [Column("point_earned")]
        public int PointEarned { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_mandatory")]
        public bool IsMandatory { get; set; } = false;

        [Column("source_submission_id")]
        public int? SourceSubmissionId { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        [ForeignKey("CheckpointId")]
        public Checkpoint Checkpoint { get; set; } = null!;

        [ForeignKey("SourceSubmissionId")]
        public StoreSubmission? SourceSubmission { get; set; }
    }
}
