using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_cv_master_summaries")]
    public class CvMasterSummary
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("candidate_id")]
        public int? CandidateId { get; set; }

        [Column("candidate_name")]
        [Required]
        [MaxLength(150)]
        public string CandidateName { get; set; } = string.Empty;

        [Column("position")]
        [MaxLength(100)]
        public string? Position { get; set; }

        [Column("last_education")]
        [MaxLength(100)]
        public string? LastEducation { get; set; }

        [Column("years_experience")]
        public int YearsExperience { get; set; }

        [Column("skills")]
        [MaxLength(500)]
        public string? Skills { get; set; }

        [Column("summary")]
        [MaxLength(1000)]
        public string? Summary { get; set; }

        [Column("score")]
        public int Score { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public RecruitmentCandidate? Candidate { get; set; }
    }
}
