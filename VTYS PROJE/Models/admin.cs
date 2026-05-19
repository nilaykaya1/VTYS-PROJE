using System;

namespace VTYS_PROJE.Models;

public partial class admin
{
    public long admin_id { get; set; }

    public string full_name { get; set; } = null!;

    public string email { get; set; } = null!;

    public string password_hash { get; set; } = null!;

    public string role { get; set; } = null!;

    public bool is_active { get; set; }

    public DateTime created_at { get; set; }
}
