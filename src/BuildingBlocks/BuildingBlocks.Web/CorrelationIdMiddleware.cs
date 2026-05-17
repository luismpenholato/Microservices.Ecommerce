using Serilog.Context;

namespace BuildingBlocks.Web;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdConstants.HeaderName].FirstOrDefault()
            ?? context.TraceIdentifier;

        context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
        context.Items[CorrelationIdConstants.ItemKey] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
