using System.ComponentModel.DataAnnotations;

namespace NavSnap.Models.ViewModels
{
    public class AttendanceSettingViewModel
    {
        [Required]
        [MaxLength(150)]
        public string LocationName { get; set; } = string.Empty;
        [Required]
        public double Latitude { get; set; }
        [Required]
        public double Longitude { get; set; }
        [Range(10, 1000)]
        public int RadiusMeters { get; set; } = 100;
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
