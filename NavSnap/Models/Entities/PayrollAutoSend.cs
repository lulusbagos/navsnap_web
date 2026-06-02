using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NavSnap.Models.Entities
{
    [Table("tbl_t_payroll_auto_sends")]
    public class PayrollAutoSend
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("payroll_period")]
        [Required]
        [MaxLength(20)]
        public string PayrollPeriod { get; set; } = string.Empty;

        [Column("send_date")]
        public DateOnly SendDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Column("status")]
        [MaxLength(30)]
        public string Status { get; set; } = "scheduled";

        [Column("recipient_count")]
        public int RecipientCount { get; set; }

        [Column("notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        [Column("sent_at")]
        public DateTime? SentAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
