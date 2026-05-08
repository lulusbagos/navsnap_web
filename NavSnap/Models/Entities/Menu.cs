using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_m_menus")]
    public class Menu
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("menu_name")]
        [Required]
        [MaxLength(100)]
        public string MenuName { get; set; } = string.Empty;

        [Column("menu_url")]
        [MaxLength(200)]
        public string? MenuUrl { get; set; }

        [Column("icon_class")]
        [MaxLength(100)]
        public string? IconClass { get; set; }

        [Column("parent_id")]
        public int? ParentId { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; } = 0;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("ParentId")]
        public Menu? Parent { get; set; }
        public ICollection<Menu> Children { get; set; } = new List<Menu>();
        public ICollection<RoleMenu> RoleMenus { get; set; } = new List<RoleMenu>();
    }
}
