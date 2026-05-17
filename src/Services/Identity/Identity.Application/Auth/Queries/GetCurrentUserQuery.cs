using BuildingBlocks.Web;
using Identity.Application.Abstractions;
using Identity.Application.Auth;
using MediatR;

namespace Identity.Application.Auth.Queries;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<UserProfileDto?>;

public sealed class GetCurrentUserQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetCurrentUserQuery, UserProfileDto?>
{
    public async Task<UserProfileDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        return user is null
            ? null
            : new UserProfileDto(user.Id, user.CustomerId, user.Email, user.Role);
    }
}
