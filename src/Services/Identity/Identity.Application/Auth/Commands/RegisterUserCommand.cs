using BuildingBlocks.Web;
using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Application.Auth;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Auth.Commands;

public sealed record RegisterUserCommand(string Email, string Password) : IRequest<AuthTokenResponse>;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<RegisterUserCommand, AuthTokenResponse>
{
    public async Task<AuthTokenResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var passwordHash = passwordHasher.Hash(request.Password);
        var user = new User(userId, normalizedEmail, passwordHash, customerId, AuthRoles.Customer);

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        var (token, expiresAtUtc) = jwtTokenGenerator.Generate(user);
        return new AuthTokenResponse(token, expiresAtUtc, user.Id, user.CustomerId, user.Email, user.Role);
    }
}
