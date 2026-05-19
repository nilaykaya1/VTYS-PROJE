using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VTYS_PROJE.ViewModels;

public class OrderCreateViewModel
{
    [Required]
    [Display(Name = "Musteri ID")]
    public long CustomerId { get; set; }

    [Required]
    [Display(Name = "Restoran ID")]
    public long RestaurantId { get; set; }

    [Display(Name = "Kurye")]
    public long? CourierId { get; set; }

    [Required]
    [Display(Name = "Urun")]
    public long ProductId { get; set; }

    [Range(1, 100)]
    [Display(Name = "Adet")]
    public int Quantity { get; set; } = 1;

    [Required]
    [StringLength(500)]
    [Display(Name = "Teslimat Adresi")]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Display(Name = "Askida Yemek bakiyesini kullan")]
    public bool UseAskidaBalance { get; set; }

    public List<OrderCartItemViewModel> CartItems { get; set; } = new();

    public decimal CartSubtotal { get; set; }
   
    public bool UseAskida { get; set; }

    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> Restaurants { get; set; } = new();
    public List<SelectListItem> Couriers { get; set; } = new();
    public List<SelectListItem> Products { get; set; } = new();
}
