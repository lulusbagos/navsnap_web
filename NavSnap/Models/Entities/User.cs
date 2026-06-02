using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_m_users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("username")]
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Column("password")]
        [Required]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Column("full_name")]
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(100)]
        public string? Email { get; set; }

        [Column("phone")]
        [MaxLength(20)]
        public string? Phone { get; set; }

        [Column("profile_photo_path")]
        [MaxLength(255)]
        public string? ProfilePhotoPath { get; set; }

        [Column("theme_preference")]
        [MaxLength(30)]
        public string ThemePreference { get; set; } = "ocean";

        [Column("session_version")]
        public int SessionVersion { get; set; } = 1;

        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<SalesVisit> SalesVisits { get; set; } = new List<SalesVisit>();
        public ICollection<GpsLog> GpsLogs { get; set; } = new List<GpsLog>();
    }
}
