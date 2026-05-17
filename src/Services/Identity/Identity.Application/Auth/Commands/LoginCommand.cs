using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Application.Auth;
using MediatR;

namespace Identity.Application.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthTokenResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<LoginCommand, AuthTokenResponse>
{
    public async Task<AuthTokenResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var (token, expiresAtUtc) = jwtTokenGenerator.Generate(user);
        return new AuthTokenResponse(token, expiresAtUtc, user.Id, user.CustomerId, user.Email, user.Role);
    }
}
