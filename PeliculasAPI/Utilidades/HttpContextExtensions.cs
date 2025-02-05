using Microsoft.EntityFrameworkCore;

namespace PeliculasAPI.Utilidades
{
    public static class HttpContextExtensions
    {
        public static async Task InsertarParametrosPaginacionEnCabecera<T>(this HttpContext httpContext, IQueryable<T> queryable) 
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }
            else 
            {
                double cantidad = await queryable.CountAsync();
                httpContext.Response.Headers.Append("cantidad-total-reistros", cantidad.ToString());
            }
        }
    }
}
