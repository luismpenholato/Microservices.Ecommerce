using BuildingBlocks.Web;

namespace Ordering.Api;

internal static class CorrelationIdHelper
{
    public static Guid FromHttpContext(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(CorrelationIdConstants.ItemKey, out var value)
            && value is string correlationId
            && Guid.TryParse(correlationId, out var parsed))
        {
            return parsed;
        }

        return Guid.NewGuid();
    }
}
