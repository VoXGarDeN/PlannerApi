using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class RedirectMiddleware
{
    
    private readonly RequestDelegate _next;

    public RedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.User.Identity.IsAuthenticated &&
            context.Request.Path == "/")
        {
            context.Response.Redirect("/login");
            return;
        }

        await _next(context);
    }
}