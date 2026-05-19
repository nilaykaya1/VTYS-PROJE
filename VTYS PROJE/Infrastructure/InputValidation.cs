using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace VTYS_PROJE.Infrastructure;

public static class InputValidation
{
    public const string PhonePattern = @"^\d{11}$";
    public const string LettersOnlyPattern = @"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s'-]+$";
    public const string EmailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

    public const string PhoneMessage = "Telefon tam 11 haneli olmali ve sadece rakam icermelidir.";
    public const string LettersOnlyMessage = "Sadece harf kullanin; rakam veya ozel karakter girilemez.";
    public const string EmailMessage = "Gecerli bir e-posta adresi girin (ornek: ad@site.com).";

    private static readonly Regex PhoneRegex = new(PhonePattern, RegexOptions.Compiled);
    private static readonly Regex LettersRegex = new(LettersOnlyPattern, RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(EmailPattern, RegexOptions.Compiled);

    public static bool IsValidPhone(string? phone) =>
        !string.IsNullOrWhiteSpace(phone) && PhoneRegex.IsMatch(phone.Trim());

    public static bool IsValidLettersOnly(string? value) =>
        !string.IsNullOrWhiteSpace(value) && LettersRegex.IsMatch(value.Trim());

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var normalized = email.Trim();
        if (!EmailRegex.IsMatch(normalized))
        {
            return false;
        }

        return MailAddress.TryCreate(normalized, out _);
    }

    public static string NormalizePhone(string phone) => phone.Trim();

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static void ValidatePersonName(ModelStateDictionary modelState, string fieldName, string? value)
    {
        if (!IsValidLettersOnly(value))
        {
            modelState.AddModelError(fieldName, LettersOnlyMessage);
        }
    }

    public static void ValidatePhone(ModelStateDictionary modelState, string fieldName, string? value)
    {
        if (!IsValidPhone(value))
        {
            modelState.AddModelError(fieldName, PhoneMessage);
        }
    }

    public static void ValidateEmail(ModelStateDictionary modelState, string fieldName, string? value)
    {
        if (!IsValidEmail(value))
        {
            modelState.AddModelError(fieldName, EmailMessage);
        }
    }
}
