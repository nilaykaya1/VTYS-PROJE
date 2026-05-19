using System;
using System.Collections.Generic;

namespace VTYS_PROJE.Models;

public partial class askida_pool
{
    public long pool_id { get; set; }

    public decimal current_balance { get; set; }

    public decimal total_donated_balance { get; set; }

    public decimal total_used_balance { get; set; }

    public DateTime updated_at { get; set; }

    public virtual ICollection<askida_donation> askida_donations { get; set; } = new List<askida_donation>();

    public virtual ICollection<askida_redemption> askida_redemptions { get; set; } = new List<askida_redemption>();
}
