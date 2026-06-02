using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_sales_target_compliances")]
    public class SalesTargetCompliance
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("compliance_date")]
        public DateOnly ComplianceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Column("target_visits")]
        public int TargetVisits { get; set; }

        [Column("actual_visits")]
        public int ActualVisits { get; set; }

        [Column("compliance_percent")]
        public double CompliancePercent { get; set; }

        [Column("status")]
        [MaxLength(30)]
        public string Status { get; set; } = "watch";

        [Column("notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}
