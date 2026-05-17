using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.Web;

public static class CustomerAccessExtensions
{
    public static ActionResult? EnsureRouteCustomerMatchesToken(
        this ControllerBase controller,
        Guid routeCustomerId,
        ICurrentUserAccessor currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.CustomerId is null)
        {
            return controller.Unauthorized();
        }

        if (currentUser.CustomerId.Value != routeCustomerId)
        {
            return controller.StatusCode(StatusCodes.Status403Forbidden, new
            {
                title = "CustomerId in route does not match authenticated user.",
                status = StatusCodes.Status403Forbidden
            });
        }

        return null;
    }
}
