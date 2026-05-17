using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Web;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business rule violation");
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            var detail = environment.IsDevelopment() ? ex.Message : null;
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, detail);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode status, string? detail)
    {
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            title = status == HttpStatusCode.BadRequest
                ? "Request could not be processed."
                : "An unexpected error occurred.",
            detail
        }));
    }
}
