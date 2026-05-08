using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_r_role_menus")]
    public class RoleMenu
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("role_id")]
        [Required]
        public int RoleId { get; set; }

        [Column("menu_id")]
        [Required]
        public int MenuId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;
        [ForeignKey("MenuId")]
        public Menu Menu { get; set; } = null!;
    }
}
