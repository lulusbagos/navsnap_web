using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_gps_logs")]
    public class GpsLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        [Required]
        public int UserId { get; set; }

        [Column("latitude")]
        [Required]
        public double Latitude { get; set; }

        [Column("longitude")]
        [Required]
        public double Longitude { get; set; }

        [Column("accuracy")]
        public double? Accuracy { get; set; }

        [Column("logged_at")]
        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
