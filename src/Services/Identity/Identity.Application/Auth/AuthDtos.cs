namespace Identity.Application.Auth;

public sealed record AuthTokenResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    Guid CustomerId,
    string Email,
    string Role);

public sealed record UserProfileDto(
    Guid UserId,
    Guid CustomerId,
    string Email,
    string Role);
