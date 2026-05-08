using System.ComponentModel.DataAnnotations;

namespace NavSnap.Models.ViewModels
{
    public class SalesTargetBatchViewModel
    {
        [Display(Name = "Sales")]
        public List<int> SelectedSalesIds { get; set; } = new();

        [Required(ErrorMessage = "Tanggal mulai wajib diisi")]
        [Display(Name = "Tanggal Mulai")]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required(ErrorMessage = "Tanggal selesai wajib diisi")]
        [Display(Name = "Tanggal Selesai")]
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Range(1, 200, ErrorMessage = "Target toko per hari minimal 1")]
        [Display(Name = "Target Toko/Hari")]
        public int TargetStores { get; set; } = 8;

        [MaxLength(200)]
        [Display(Name = "Catatan")]
        public string? Notes { get; set; }
    }

    public class SalesTargetPageViewModel
    {
        public SalesTargetBatchViewModel Form { get; set; } = new();
        public List<SalesTargetSalesItem> SalesList { get; set; } = new();
        public List<SalesTargetListItem> Targets { get; set; } = new();
    }

    public class SalesTargetSalesItem
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class SalesTargetListItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string SalesName { get; set; } = string.Empty;
        public DateOnly TargetDate { get; set; }
        public int TargetStores { get; set; }
        public string? Notes { get; set; }
    }
}
