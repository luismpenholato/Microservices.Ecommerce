namespace BuildingBlocks.Web;

public interface ICurrentUserAccessor
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? CustomerId { get; }
    string? Email { get; }
    bool IsInRole(string role);
}
