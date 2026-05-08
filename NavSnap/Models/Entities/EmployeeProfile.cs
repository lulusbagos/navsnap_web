using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_m_employee_profiles")]
    public class EmployeeProfile
    {
        [Key]
        [Column("nrp")]
        [Required]
        [MaxLength(30)]
        public string Nrp { get; set; } = string.Empty;

        [Column("full_name")]
        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Column("full_address")]
        [MaxLength(500)]
        public string? FullAddress { get; set; }

        [Column("job_title")]
        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [Column("position")]
        [MaxLength(100)]
        public string? Position { get; set; }

        [Column("division")]
        [MaxLength(100)]
        public string? Division { get; set; }

        [Column("department")]
        [MaxLength(100)]
        public string? Department { get; set; }

        [Column("phone")]
        [MaxLength(30)]
        public string? Phone { get; set; }

        [Column("email")]
        [MaxLength(120)]
        public string? Email { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public ICollection<MUser> MobileUsers { get; set; } = new List<MUser>();
    }
}
