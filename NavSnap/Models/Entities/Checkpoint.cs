using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_m_checkpoints")]
    public class Checkpoint
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("checkpoint_name")]
        [Required]
        [MaxLength(150)]
        public string CheckpointName { get; set; } = string.Empty;

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

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey("CreatedBy")]
        public User? Creator { get; set; }
        public ICollection<SalesVisit> SalesVisits { get; set; } = new List<SalesVisit>();
        public ICollection<StoreSubmission> ApprovedSubmissions { get; set; } = new List<StoreSubmission>();
    }
}
