using BuildingBlocks.Domain;

namespace Identity.Domain.Entities;

public sealed class User : Entity<Guid>
{
    private User() { }

    public User(Guid id, string email, string passwordHash, Guid customerId, string role)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        CustomerId = customerId;
        Role = role;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string Role { get; private set; } = string.Empty;
}
