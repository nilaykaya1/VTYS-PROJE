using System;

namespace VTYS_PROJE.Models;

public partial class restaurant_review
{
    public long review_id { get; set; }

    public long restaurant_id { get; set; }

    public long customer_id { get; set; }

    public byte rating { get; set; }

    public string comment { get; set; } = null!;

    public bool is_active { get; set; }

    public DateTime created_at { get; set; }

    public virtual customer customer { get; set; } = null!;

    public virtual restaurant restaurant { get; set; } = null!;
}
