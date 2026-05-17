using System.Security.Claims;

namespace BuildingBlocks.Web;

public sealed class HttpCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserId => TryParseGuid(User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue("sub"));

    public Guid? CustomerId => TryParseGuid(User?.FindFirstValue(AuthClaimTypes.CustomerId));

    public string? Email => User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue("email");

    public bool IsInRole(string role) => User?.IsInRole(role) == true;

    private static Guid? TryParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
