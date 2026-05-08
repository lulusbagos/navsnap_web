using System.ComponentModel.DataAnnotations;

namespace NavSnap.Models.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username wajib diisi")]
        [MaxLength(50)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [MaxLength(100)]
        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [MaxLength(20)]
        [Display(Name = "Telepon")]
        public string? Phone { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Role")]
        public List<int> SelectedRoleIds { get; set; } = new();
    }

    public class RoleViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama role wajib diisi")]
        [MaxLength(50)]
        [Display(Name = "Nama Role")]
        public string RoleName { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "Deskripsi")]
        public string? Description { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Menu yang Diizinkan")]
        public List<int> SelectedMenuIds { get; set; } = new();
    }
}
