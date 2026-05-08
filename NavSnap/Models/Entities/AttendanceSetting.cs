using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_m_attendance_settings")]
    public class AttendanceSetting
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("location_name")]
        [Required]
        [MaxLength(150)]
        public string LocationName { get; set; } = string.Empty;

        [Column("latitude")]
        public double Latitude { get; set; }

        [Column("longitude")]
        public double Longitude { get; set; }

        [Column("radius_meters")]
        public int RadiusMeters { get; set; } = 100;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
