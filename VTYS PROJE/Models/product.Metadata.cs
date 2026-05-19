using System;
using System.ComponentModel.DataAnnotations;

namespace VTYS_PROJE.Models;

public class ProductMetadata
{
    [Display(Name = "Urun Adi")]
    public string product_name { get; set; } = null!;

    [Display(Name = "Restoran")]
    public long restaurant_id { get; set; }

    [Display(Name = "Kategori")]
    public long category_id { get; set; }

    [Display(Name = "Aciklama")]
    public string? description { get; set; }

    [Display(Name = "Birim Fiyat")]
    public decimal unit_price { get; set; }

    [Display(Name = "Stok Adedi")]
    public int stock_qty { get; set; }

    [Display(Name = "Aktif")]
    public bool is_active { get; set; }

    [Display(Name = "Kayit Tarihi")]
    public DateTime created_at { get; set; }
}

[MetadataType(typeof(ProductMetadata))]
public partial class product
{
}
