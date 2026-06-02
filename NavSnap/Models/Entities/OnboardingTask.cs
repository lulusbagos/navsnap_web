using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_onboarding_tasks")]
    public class OnboardingTask
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("candidate_id")]
        public int? CandidateId { get; set; }

        [Column("employee_name")]
        [Required]
        [MaxLength(150)]
        public string EmployeeName { get; set; } = string.Empty;

        [Column("task_name")]
        [Required]
        [MaxLength(150)]
        public string TaskName { get; set; } = string.Empty;

        [Column("due_date")]
        public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Column("status")]
        [MaxLength(30)]
        public string Status { get; set; } = "open";

        [Column("owner_name")]
        [MaxLength(100)]
        public string? OwnerName { get; set; }

        [Column("notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public RecruitmentCandidate? Candidate { get; set; }
    }
}
