using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_sales_daily_targets")]
    public class SalesDailyTarget
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        [Required]
        public int UserId { get; set; }

        [Column("target_date")]
        [Required]
        public DateOnly TargetDate { get; set; }

        [Column("target_stores")]
        [Required]
        public int TargetStores { get; set; } = 0;

        [Column("notes")]
        [MaxLength(200)]
        public string? Notes { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public User? Creator { get; set; }
    }
}
