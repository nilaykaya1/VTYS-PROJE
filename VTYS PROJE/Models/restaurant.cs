using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VTYS_PROJE.Models;

public partial class restaurant
{
    public long restaurant_id { get; set; }

    [Display(Name = "Restoran Adı")]
    public string restaurant_name { get; set; } = null!;

    [Display(Name = "E-Posta")]
    public string email { get; set; } = null!;

    [Display(Name = "Telefon")]
    public string phone { get; set; } = null!;

    [Display(Name = "Mutfak Türü")]
    public string cuisine_type { get; set; } = null!;

    [Display(Name = "Puan")]
    public decimal rating { get; set; }

    [Display(Name = "Toplam Kazanç")]
    public decimal total_revenue { get; set; }

    public bool is_active { get; set; }

    public DateTime created_at { get; set; }

    public virtual ICollection<order> orders { get; set; } = new List<order>();

    public virtual ICollection<product> products { get; set; } = new List<product>();

    public virtual ICollection<restaurant_review> restaurant_reviews { get; set; } = new List<restaurant_review>();
}