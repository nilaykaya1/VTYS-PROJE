using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VTYS_PROJE.ViewModels;

public class AskidaYemekDonateViewModel
{
    [Required]
    [Display(Name = "Bagis yapan Musteri ID")]
    public long CustomerId { get; set; }

    [Range(1, 100000)]
    [Display(Name = "Bagis Tutari (TL)")]
    public decimal Amount { get; set; }

    [StringLength(250)]
    [Display(Name = "Not")]
    public string? Note { get; set; }

    [Display(Name = "Bagis Turu")]
    public string DonationType { get; set; } = "BALANCE";

    [Display(Name = "Anonim Bagis")]
    public bool IsAnonymous { get; set; }
   
    public int? MealCount { get; set; }

    public List<SelectListItem> Customers { get; set; } = new();
}
