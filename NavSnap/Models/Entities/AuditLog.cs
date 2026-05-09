using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_audit_logs")]
    public class AuditLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("module")]
        [MaxLength(50)]
        public string Module { get; set; } = string.Empty;

        [Column("action")]
        [MaxLength(80)]
        public string Action { get; set; } = string.Empty;

        [Column("entity_name")]
        [MaxLength(80)]
        public string? EntityName { get; set; }

        [Column("entity_id")]
        public int? EntityId { get; set; }

        [Column("description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}

