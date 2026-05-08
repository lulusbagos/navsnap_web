using System.ComponentModel.DataAnnotations;

namespace NavSnap.Models.ViewModels
{
    public class CheckpointViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama checkpoint wajib diisi")]
        [MaxLength(150)]
        [Display(Name = "Nama Checkpoint")]
        public string CheckpointName { get; set; } = string.Empty;

        [MaxLength(300)]
        [Display(Name = "Alamat")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Latitude wajib diisi")]
        [Display(Name = "Latitude")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude wajib diisi")]
        [Display(Name = "Longitude")]
        public double Longitude { get; set; }

        [Range(10, 5000, ErrorMessage = "Radius harus antara 10 - 5000 meter")]
        [Display(Name = "Radius (meter)")]
        public int RadiusMeters { get; set; } = 100;

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }
}
