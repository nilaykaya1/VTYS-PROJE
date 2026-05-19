using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VTYS_PROJE.Infrastructure;

namespace VTYS_PROJE.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAdminAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var role = context.HttpContext.Session.GetString(AuthSessionKeys.Role);
        if (role != AuthSessionKeys.RoleAdmin)
        {
            context.HttpContext.Session.SetString("auth_denied_message",
                "Bu sayfa yalnizca yonetici icindir. Musteri hesabi ile erisemezsiniz.");
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }
}
