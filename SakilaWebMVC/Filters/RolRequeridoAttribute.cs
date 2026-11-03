using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SakilaWebMVC.Helpers;

namespace SakilaWebMVC.Filters
{
    // Autorizacion por rol: [RolRequerido(Roles.Administrador)]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RolRequeridoAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public RolRequeridoAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            ISession sesion = context.HttpContext.Session;

            if (sesion.Usuario() == null)
            {
                context.Result = new RedirectToActionResult("Login", "Acceso", null);
                return;
            }

            if (!_roles.Contains(sesion.Rol()))
            {
                context.Result = new RedirectToActionResult("AccesoDenegado", "Acceso", null);
            }
        }
    }
}
