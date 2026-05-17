using BuildingBlocks.Web;
using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Persistence;

public static class IdentityDataSeeder
{
    public const string DemoEmail = "demo@ecommerce.local";
    public const string DemoPassword = "Demo123!";
    public static readonly Guid DemoCustomerId = Guid.Parse("33333333-3333-3333-3333-333333333301");
    public static readonly Guid DemoUserId = Guid.Parse("33333333-3333-3333-3333-333333333302");

    public const string AdminEmail = "admin@ecommerce.local";
    public const string AdminPassword = "Admin123!";
    public static readonly Guid AdminUserId = Guid.Parse("33333333-3333-3333-3333-333333333303");
    public static readonly Guid AdminCustomerId = Guid.Parse("33333333-3333-3333-3333-333333333304");

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityDataSeeder");

        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var demoUser = new User(
            DemoUserId,
            DemoEmail,
            passwordHasher.Hash(DemoPassword),
            DemoCustomerId,
            AuthRoles.Customer);

        var adminUser = new User(
            AdminUserId,
            AdminEmail,
            passwordHasher.Hash(AdminPassword),
            AdminCustomerId,
            AuthRoles.Admin);

        await db.Users.AddRangeAsync([demoUser, adminUser], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Demo users seeded: {DemoEmail}, {AdminEmail} (passwords not logged).",
            DemoEmail,
            AdminEmail);
    }
}
