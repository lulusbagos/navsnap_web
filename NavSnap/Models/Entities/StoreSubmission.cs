using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_store_submissions")]
    public class StoreSubmission
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("sales_user_id")]
        [Required]
        public int SalesUserId { get; set; }

        [Column("store_name")]
        [Required]
        [MaxLength(150)]
        public string StoreName { get; set; } = string.Empty;

        [Column("address")]
        [MaxLength(300)]
        public string? Address { get; set; }

        [Column("latitude")]
        [Required]
        public double Latitude { get; set; }

        [Column("longitude")]
        [Required]
        public double Longitude { get; set; }

        [Column("radius_meters")]
        public int RadiusMeters { get; set; } = 100;

        [Column("submission_note")]
        [MaxLength(500)]
        public string? SubmissionNote { get; set; }

        [Column("status")]
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        [Column("admin_note")]
        [MaxLength(500)]
        public string? AdminNote { get; set; }

        [Column("proposed_visit_date")]
        public DateOnly? ProposedVisitDate { get; set; }

        [Column("approved_checkpoint_id")]
        public int? ApprovedCheckpointId { get; set; }

        [Column("approved_by")]
        public int? ApprovedBy { get; set; }

        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("SalesUserId")]
        public User SalesUser { get; set; } = null!;

        [ForeignKey("ApprovedCheckpointId")]
        public Checkpoint? ApprovedCheckpoint { get; set; }

        [ForeignKey("ApprovedBy")]
        public User? Approver { get; set; }
    }
}
