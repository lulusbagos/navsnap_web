using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_m_attendance_geofence_points")]
    public class AttendanceGeofencePoint
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("attendance_setting_id")]
        public int AttendanceSettingId { get; set; }

        [Column("seq_no")]
        public int SeqNo { get; set; }

        [Column("latitude")]
        public double Latitude { get; set; }

        [Column("longitude")]
        public double Longitude { get; set; }

        [ForeignKey("AttendanceSettingId")]
        public AttendanceSetting AttendanceSetting { get; set; } = null!;
    }
}
