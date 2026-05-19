using System.ComponentModel.DataAnnotations;
using VTYS_PROJE.Infrastructure;

namespace VTYS_PROJE.ViewModels;

public class RegisterAdminViewModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Ad soyad 2-120 karakter olmalidir.")]
    [RegularExpression(InputValidation.LettersOnlyPattern, ErrorMessage = InputValidation.LettersOnlyMessage)]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [RegularExpression(InputValidation.EmailPattern, ErrorMessage = InputValidation.EmailMessage)]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Sifre en az 8 karakter olmalidir.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Sifre en az bir harf ve bir rakam icermelidir.")]
    [DataType(DataType.Password)]
    [Display(Name = "Sifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre tekrari zorunludur.")]
    [Compare(nameof(Password), ErrorMessage = "Sifreler eslesmiyor.")]
    [DataType(DataType.Password)]
    [Display(Name = "Sifre Tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yonetici davet kodu zorunludur.")]
    [Display(Name = "Yonetici Davet Kodu")]
    public string InviteCode { get; set; } = string.Empty;
}
