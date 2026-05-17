using System.Net;
using System.Text.Json;
using Ordering.Application.Exceptions;

namespace Ordering.Api.Middleware;

public sealed class IdempotencyConflictMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (IdempotencyConflictException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = "Idempotency key conflict.",
                detail = ex.Message
            }));
        }
    }
}
