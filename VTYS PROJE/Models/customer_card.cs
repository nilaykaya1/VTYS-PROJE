using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VTYS_PROJE.Models;

public class customer_card
{
    [Key]
    public int card_id { get; set; }

    public int customer_id { get; set; }

    [Required]
    [StringLength(120)]
    public string card_holder_name { get; set; } = string.Empty;

    [Required]
    [StringLength(16)]
    public string card_number { get; set; } = string.Empty;

    [Required]
    public int expiry_month { get; set; }

    [Required]
    public int expiry_year { get; set; }

    [Required]
    [StringLength(4)]
    public string cvv { get; set; } = string.Empty;

    public DateTime created_at { get; set; } = DateTime.Now;

    [ForeignKey(nameof(customer_id))]
    public virtual customer? customer { get; set; }
}