using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using NavSnap.Models.Entities;

namespace NavSnap.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public string? ProfilePhotoPath { get; set; }
        public string ThemePreference { get; set; } = "ocean";
        public DateTime? LastLoginAt { get; set; }
        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
        public IReadOnlyList<AuditLog> SecurityLogs { get; set; } = Array.Empty<AuditLog>();
    }

    public class ProfileUpdateViewModel
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public IFormFile? ProfilePhoto { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Password saat ini wajib diisi")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password baru wajib diisi")]
        [MinLength(6, ErrorMessage = "Password minimal 6 karakter")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password wajib diisi")]
        [Compare(nameof(NewPassword), ErrorMessage = "Konfirmasi password tidak sama")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
