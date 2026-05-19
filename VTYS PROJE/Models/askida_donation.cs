using System;
using System.Collections.Generic;

namespace VTYS_PROJE.Models;

public partial class askida_donation
{
    public long donation_id { get; set; }

    public long pool_id { get; set; }

    public long donor_customer_id { get; set; }

    public string donation_type { get; set; } = null!;

    public int? meal_count { get; set; }

    public decimal amount { get; set; }

    public bool is_anonymous { get; set; }

    public string? note { get; set; }

    public DateTime created_at { get; set; }

    public virtual customer donor_customer { get; set; } = null!;

    public virtual askida_pool pool { get; set; } = null!;
}
