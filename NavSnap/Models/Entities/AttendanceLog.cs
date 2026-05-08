using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_attendance_logs")]
    public class AttendanceLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("attendance_date")]
        public DateOnly AttendanceDate { get; set; }

        [Column("check_in_at")]
        public DateTime? CheckInAt { get; set; }

        [Column("check_out_at")]
        public DateTime? CheckOutAt { get; set; }

        [Column("check_in_latitude")]
        public double? CheckInLatitude { get; set; }

        [Column("check_in_longitude")]
        public double? CheckInLongitude { get; set; }

        [Column("check_out_latitude")]
        public double? CheckOutLatitude { get; set; }

        [Column("check_out_longitude")]
        public double? CheckOutLongitude { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "present";

        [Column("notes")]
        [MaxLength(300)]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
