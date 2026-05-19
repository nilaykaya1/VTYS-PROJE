using System;
using System.Collections.Generic;

namespace VTYS_PROJE.Models;

public partial class courier
{
    public long courier_id { get; set; }

    public string full_name { get; set; } = null!;

    public string email { get; set; } = null!;

    public string phone { get; set; } = null!;

    public string vehicle_type { get; set; } = null!;

    public bool is_active { get; set; }

    public DateTime created_at { get; set; }

    public virtual ICollection<order> orders { get; set; } = new List<order>();
}
