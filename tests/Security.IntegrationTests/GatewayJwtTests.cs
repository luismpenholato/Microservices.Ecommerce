using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Security.IntegrationTests.Infrastructure;
using Xunit;

namespace Security.IntegrationTests;

public sealed class GatewayJwtTests
{
    [Fact]
    public async Task Gateway_Basket_Without_Token_Should_Return_401()
    {
        await using var factory = new WebApplicationFactory<ApiGateway.GatewayAuthorizationMiddleware>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Jwt:Secret", TestJwtSettings.Secret);
                builder.UseSetting("Jwt:Issuer", TestJwtSettings.Issuer);
                builder.UseSetting("Jwt:Audience", TestJwtSettings.Audience);
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/basket/baskets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Gateway_Catalog_Get_Without_Token_Should_Not_Return_401()
    {
        await using var factory = new WebApplicationFactory<ApiGateway.GatewayAuthorizationMiddleware>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Jwt:Secret", TestJwtSettings.Secret);
                builder.UseSetting("Jwt:Issuer", TestJwtSettings.Issuer);
                builder.UseSetting("Jwt:Audience", TestJwtSettings.Audience);
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/catalog/products");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Gateway_Basket_With_Valid_Token_Should_Not_Return_401()
    {
        var customerId = Guid.NewGuid();
        var token = TestJwtTokenFactory.CreateToken(
            Guid.NewGuid(),
            customerId,
            "gateway@test.local",
            BuildingBlocks.Web.AuthRoles.Customer);

        await using var factory = new WebApplicationFactory<ApiGateway.GatewayAuthorizationMiddleware>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Jwt:Secret", TestJwtSettings.Secret);
                builder.UseSetting("Jwt:Issuer", TestJwtSettings.Issuer);
                builder.UseSetting("Jwt:Audience", TestJwtSettings.Audience);
            });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync($"/basket/baskets/{customerId}");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
