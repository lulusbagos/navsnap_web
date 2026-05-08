using System.ComponentModel.DataAnnotations;

namespace NavSnap.Models.ViewModels
{
    public class MUserViewModel
    {
        [Required(ErrorMessage = "NRP wajib diisi")]
        [MaxLength(30)]
        [Display(Name = "NRP")]
        public string Nrp { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username wajib diisi")]
        [MaxLength(50)]
        [Display(Name = "Username")]
        public string User { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Display(Name = "Terakhir Login")]
        public DateTime? TerakhirLogin { get; set; }

        [Display(Name = "Status Aktif")]
        public bool Status { get; set; } = true;

        [MaxLength(255)]
        [Display(Name = "Path Foto Profile")]
        public string? PathFotoProfile { get; set; }
    }
}
