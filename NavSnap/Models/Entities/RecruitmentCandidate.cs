using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_recruitment_candidates")]
    public class RecruitmentCandidate
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("candidate_name")]
        [Required]
        [MaxLength(150)]
        public string CandidateName { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(120)]
        public string? Email { get; set; }

        [Column("phone")]
        [MaxLength(30)]
        public string? Phone { get; set; }

        [Column("position_applied")]
        [MaxLength(100)]
        public string? PositionApplied { get; set; }

        [Column("stage")]
        [MaxLength(50)]
        public string Stage { get; set; } = "Screening";

        [Column("status")]
        [MaxLength(30)]
        public string Status { get; set; } = "active";

        [Column("source")]
        [MaxLength(80)]
        public string? Source { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<OnboardingTask> OnboardingTasks { get; set; } = new List<OnboardingTask>();
        public ICollection<CvMasterSummary> CvSummaries { get; set; } = new List<CvMasterSummary>();
    }
}
