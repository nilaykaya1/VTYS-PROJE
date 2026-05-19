using System;
using System.ComponentModel.DataAnnotations;

namespace VTYS_PROJE.Models;

public class CustomerMetadata
{
    [Display(Name = "Ad Soyad")]
    public string full_name { get; set; } = null!;

    [Display(Name = "E-posta")]
    public string email { get; set; } = null!;

    [Display(Name = "Telefon")]
    public string phone { get; set; } = null!;

    [Display(Name = "Sifre")]
    public string password_hash { get; set; } = null!;

    [Display(Name = "Yararlanici Onayli")]
    public bool is_beneficiary_verified { get; set; }

    [Display(Name = "Aktif")]
    public bool is_active { get; set; }

    [Display(Name = "Kayit Tarihi")]
    public DateTime created_at { get; set; }
}

[MetadataType(typeof(CustomerMetadata))]
public partial class customer
{
}
