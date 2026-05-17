using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BuildingBlocks.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Security.IntegrationTests.Infrastructure;
using Testcontainers.Redis;
using Xunit;

namespace Security.IntegrationTests;

public sealed class BasketAuthorizationTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private WebApplicationFactory<global::Basket.Api.Controllers.BasketsController>? _factory;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();

        _factory = new WebApplicationFactory<global::Basket.Api.Controllers.BasketsController>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
                builder.UseSetting("Services:Ordering", "http://localhost:5003");
                builder.UseSetting("Jwt:Secret", TestJwtSettings.Secret);
                builder.UseSetting("Jwt:Issuer", TestJwtSettings.Issuer);
                builder.UseSetting("Jwt:Audience", TestJwtSettings.Audience);
            });
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _redis.DisposeAsync();
    }

    [Fact]
    public async Task Basket_Without_Token_Should_Return_401()
    {
        var client = _factory!.CreateClient();
        var customerId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/baskets/{customerId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Basket_With_Valid_Token_And_Matching_Customer_Should_Return_200()
    {
        var customerId = Guid.NewGuid();
        var token = TestJwtTokenFactory.CreateToken(
            Guid.NewGuid(),
            customerId,
            "basket@test.local",
            AuthRoles.Customer);

        var client = _factory!.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/baskets/{customerId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Basket_With_Mismatched_CustomerId_Should_Return_403()
    {
        var tokenCustomerId = Guid.NewGuid();
        var routeCustomerId = Guid.NewGuid();
        var token = TestJwtTokenFactory.CreateToken(
            Guid.NewGuid(),
            tokenCustomerId,
            "mismatch@test.local",
            AuthRoles.Customer);

        var client = _factory!.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/baskets/{routeCustomerId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
