using System.ComponentModel.DataAnnotations;

namespace VTYS_PROJE.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [Display(Name = "Sifre")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}