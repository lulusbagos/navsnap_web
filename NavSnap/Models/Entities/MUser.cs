using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_m_user")]
    public class MUser
    {
        [Key]
        [Column("nrp")]
        [Required]
        [MaxLength(30)]
        public string Nrp { get; set; } = string.Empty;

        [Column("user")]
        [Required]
        [MaxLength(50)]
        public string User { get; set; } = string.Empty;

        [Column("password")]
        [Required]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Column("terakhir_login")]
        public DateTime? TerakhirLogin { get; set; }

        [Column("status")]
        public bool Status { get; set; } = true;

        [Column("path_foto_profile")]
        [MaxLength(255)]
        public string? PathFotoProfile { get; set; }

        [Column("employee_nrp")]
        [MaxLength(30)]
        public string? EmployeeNrp { get; set; }

        [ForeignKey("EmployeeNrp")]
        public EmployeeProfile? EmployeeProfile { get; set; }
    }
}
