using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SakilaWebMVC.Helpers;

namespace SakilaWebMVC.Filters
{
    // Filtro global: toda accion exige sesion iniciada, salvo las marcadas con [AllowAnonymous]
    public class SesionRequeridaFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            bool anonimo = context.ActionDescriptor.EndpointMetadata
                .OfType<IAllowAnonymous>().Any();
            if (anonimo) return;

            if (context.HttpContext.Session.Usuario() == null)
            {
                context.Result = new RedirectToActionResult("Login", "Acceso", null);
            }
        }
    }
}
