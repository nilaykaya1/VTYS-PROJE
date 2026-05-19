using System;
using System.Collections.Generic;

namespace VTYS_PROJE.Models;

public partial class customer
{
    public long customer_id { get; set; }

    public string full_name { get; set; } = null!;

    public string email { get; set; } = null!;

    public string? phone { get; set; }

    public string password_hash { get; set; } = null!;

    public bool? is_beneficiary_verified { get; set; }

    public bool? is_active { get; set; }

    public DateTime? created_at { get; set; }

    public virtual ICollection<askida_donation> askida_donations { get; set; }
        = new List<askida_donation>();

    public virtual ICollection<askida_redemption> askida_redemptions { get; set; }
        = new List<askida_redemption>();

    public virtual ICollection<order> orders { get; set; }
        = new List<order>();

    public virtual ICollection<restaurant_review> restaurant_reviews { get; set; }
        = new List<restaurant_review>();

    // EKLENDİ
    public virtual ICollection<customer_card> customer_cards { get; set; }
        = new List<customer_card>();
}