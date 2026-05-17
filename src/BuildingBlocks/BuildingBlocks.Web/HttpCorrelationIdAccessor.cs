using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Web;

public sealed class HttpCorrelationIdAccessor(IHttpContextAccessor httpContextAccessor) : ICorrelationIdAccessor
{
    public Guid Get()
    {
        var context = httpContextAccessor.HttpContext;
        if (context?.Items.TryGetValue(CorrelationIdConstants.ItemKey, out var value) == true
            && value is string correlationId
            && Guid.TryParse(correlationId, out var parsed))
        {
            return parsed;
        }

        return Guid.NewGuid();
    }
}
