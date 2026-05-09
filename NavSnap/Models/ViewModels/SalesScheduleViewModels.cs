using System.ComponentModel.DataAnnotations;

namespace NavSnap.Models.ViewModels
{
    public class SalesScheduleInput
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int CheckpointId { get; set; }

        [Required]
        public DateOnly DateFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required]
        public DateOnly DateTo { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required]
        [RegularExpression(@"^\d{2}:\d{2}$")]
        public string StartTime { get; set; } = "09:00";

        [Required]
        [RegularExpression(@"^\d{2}:\d{2}$")]
        public string EndTime { get; set; } = "10:00";
    }
}
