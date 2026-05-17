using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Security.IntegrationTests.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace Security.IntegrationTests;

public sealed class IdentityAuthTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("identity_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<global::Identity.Api.Controllers.AuthController>? _factory;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<global::Identity.Api.Controllers.AuthController>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:IdentityDb", _postgres.GetConnectionString());
                builder.UseSetting("Jwt:Secret", TestJwtSettings.Secret);
                builder.UseSetting("Jwt:Issuer", TestJwtSettings.Issuer);
                builder.UseSetting("Jwt:Audience", TestJwtSettings.Audience);
            });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Register_And_Login_Should_Return_Jwt()
    {
        var client = _factory!.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@test.local";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!"
        });

        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await register.Content.ReadFromJsonAsync<AuthResponse>();
        registerBody!.AccessToken.Should().NotBeNullOrWhiteSpace();
        registerBody.CustomerId.Should().NotBeEmpty();

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await login.Content.ReadFromJsonAsync<AuthResponse>();
        loginBody!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_With_Invalid_Credentials_Should_Return_401()
    {
        var client = _factory!.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "missing@test.local",
            password = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_Without_Token_Should_Return_401()
    {
        var client = _factory!.CreateClient();
        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record AuthResponse(
        string AccessToken,
        DateTime ExpiresAtUtc,
        Guid UserId,
        Guid CustomerId,
        string Email,
        string Role);
}
