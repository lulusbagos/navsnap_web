using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NavSnap.Models.ViewModels
{
    public class AttendanceSettingViewModel
    {
        [Required(ErrorMessage = "Nama lokasi wajib diisi")]
        [MaxLength(150)]
        public string LocationName { get; set; } = string.Empty;
        [Required]
        public double Latitude { get; set; }
        [Required]
        public double Longitude { get; set; }
        [Range(10, 1000)]
        public int RadiusMeters { get; set; } = 100;

        public string? GeofencePoints { get; set; }
        public List<GeoPointViewModel> ExistingPoints { get; set; } = new();
    }

    public class AttendanceSettingsPageViewModel
    {
        public AttendanceSettingViewModel Form { get; set; } = new();
        public List<AttendanceLocationItem> Locations { get; set; } = new();
    }

    public class AttendanceLocationItem
    {
        public int Id { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int RadiusMeters { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PointCount { get; set; }
    }

    public class GeoPointViewModel
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    public class LeaveRequestInput
    {
        [Required]
        public DateOnly LeaveDateFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        [Required]
        public DateOnly LeaveDateTo { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        [Required]
        [MaxLength(30)]
        public string LeaveType { get; set; } = "izin";
        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    public class OvertimeRequestInput
    {
        [Required]
        public DateOnly OvertimeDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        [Range(0.5, 24)]
        public decimal Hours { get; set; } = 1;
        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
