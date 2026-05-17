using BuildingBlocks.Web;

namespace ApiGateway;

public sealed class GatewayAuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        if (IsPublicPath(path, method))
        {
            await next(context);
            return;
        }

        if (!IsAuthenticated(context))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { title = "Unauthorized", status = 401 });
            return;
        }

        if (RequiresAdminRole(path, method) && !context.User.IsInRole(AuthRoles.Admin))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { title = "Forbidden", status = 403 });
            return;
        }

        await next(context);
    }

    private static bool IsPublicPath(string path, string method)
    {
        if (path == "/" || path.StartsWith("/identity/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.StartsWith("/catalog/", StringComparison.OrdinalIgnoreCase)
            && IsCatalogRead(path, method))
        {
            return true;
        }

        if (path.StartsWith("/inventory/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsCatalogRead(string path, string method) =>
        HttpMethods.IsGet(method)
        && (path.Equals("/catalog/products", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/catalog/products/", StringComparison.OrdinalIgnoreCase));

    private static bool RequiresAdminRole(string path, string method) =>
        path.StartsWith("/catalog/", StringComparison.OrdinalIgnoreCase)
        && (HttpMethods.IsPost(method) || HttpMethods.IsPut(method));

    private static bool IsAuthenticated(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true;
}
