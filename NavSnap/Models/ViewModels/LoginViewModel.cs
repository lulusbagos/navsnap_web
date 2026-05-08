using System.ComponentModel.DataAnnotations;

namespace NavSnap.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username wajib diisi")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
