using System;
using System.Collections.Generic;

namespace VTYS_PROJE.Models;

public partial class order
{
    public long order_id { get; set; }

    public long customer_id { get; set; }

    public long restaurant_id { get; set; }

    public long? courier_id { get; set; }

    public DateTime order_date { get; set; }

    public string status { get; set; } = null!;

    public string delivery_address { get; set; } = null!;

    public string payment_method { get; set; } = null!;

    public decimal total_amount { get; set; }

    public decimal askida_used_amount { get; set; }

    public bool revenue_recognized { get; set; }

    public bool is_active { get; set; }

    public virtual askida_redemption? askida_redemption { get; set; }

    public virtual courier? courier { get; set; }

    public virtual customer customer { get; set; } = null!;

    public virtual ICollection<order_item> order_items { get; set; } = new List<order_item>();

    public virtual restaurant restaurant { get; set; } = null!;
}
