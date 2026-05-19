using System;
using System.ComponentModel.DataAnnotations;

namespace VTYS_PROJE.Models;

public class RestaurantMetadata
{
    [Display(Name = "Restoran Adi")]
    public string restaurant_name { get; set; } = null!;

    [Display(Name = "E-posta")]
    public string email { get; set; } = null!;

    [Display(Name = "Telefon")]
    public string phone { get; set; } = null!;

    [Display(Name = "Mutfak Turu")]
    public string cuisine_type { get; set; } = null!;

    [Display(Name = "Puan")]
    public decimal rating { get; set; }

    [Display(Name = "Toplam Ciro")]
    public decimal total_revenue { get; set; }

    [Display(Name = "Aktif")]
    public bool is_active { get; set; }

    [Display(Name = "Kayit Tarihi")]
    public DateTime created_at { get; set; }
}

[MetadataType(typeof(RestaurantMetadata))]
public partial class restaurant
{
}
