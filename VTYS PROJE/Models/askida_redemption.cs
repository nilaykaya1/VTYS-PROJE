using System;
using System.Collections.Generic;

namespace VTYS_PROJE.Models;

public partial class askida_redemption
{
    public long redemption_id { get; set; }

    public long pool_id { get; set; }

    public long beneficiary_customer_id { get; set; }

    public long order_id { get; set; }

    public decimal amount_used { get; set; }

    public string status { get; set; } = null!;

    public DateTime created_at { get; set; }

    public virtual customer beneficiary_customer { get; set; } = null!;

    public virtual order order { get; set; } = null!;

    public virtual askida_pool pool { get; set; } = null!;
}
