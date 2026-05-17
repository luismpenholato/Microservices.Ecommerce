using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Infrastructure.Persistence;
using IntegrationTests.Infrastructure;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace IntegrationTests;

public sealed class CheckoutEndToEndTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("ordering_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .Build();

    private WebApplicationFactory<global::Ordering.Api.Program>? _orderingFactory;
    private WebApplicationFactory<global::Basket.Api.Program>? _basketFactory;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();
        await _rabbitMq.StartAsync();

        _orderingFactory = new WebApplicationFactory<global::Ordering.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:OrderingDb", _postgres.GetConnectionString());
                builder.UseSetting("RabbitMq:Host", _rabbitMq.Hostname);
                builder.UseSetting("RabbitMq:Username", "guest");
                builder.UseSetting("RabbitMq:Password", "guest");
                IntegrationTestAuthHelper.ApplyJwtSettings(builder);
            });

        using (var scope = _orderingFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            await db.Database.MigrateAsync();
        }

        _basketFactory = new WebApplicationFactory<global::Basket.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
                builder.UseSetting("Services:Ordering", _orderingFactory.Server.BaseAddress.ToString().TrimEnd('/'));
                IntegrationTestAuthHelper.ApplyJwtSettings(builder);
            });
    }

    public async Task DisposeAsync()
    {
        if (_basketFactory is not null)
        {
            await _basketFactory.DisposeAsync();
        }

        if (_orderingFactory is not null)
        {
            await _orderingFactory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    [Fact]
    public async Task Checkout_Should_Create_Order_And_Outbox_Message()
    {
        var customerId = Guid.NewGuid();
        var basketClient = _basketFactory!.CreateClient();
        IntegrationTestAuthHelper.SetBearerToken(basketClient, customerId);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111101");

        var addItem = await basketClient.PostAsJsonAsync(
            $"/api/baskets/{customerId}/items",
            new { productId, productName = "Notebook Pro", unitPrice = 5499.90m, quantity = 1 });

        addItem.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkout = await basketClient.PostAsync($"/api/baskets/{customerId}/checkout", null);
        checkout.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkoutBody = await checkout.Content.ReadFromJsonAsync<CheckoutResponse>();
        checkoutBody.Should().NotBeNull();

        using var scope = _orderingFactory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == checkoutBody!.OrderId);
        order.Should().NotBeNull();

        var outboxCount = await db.OutboxMessages.CountAsync();
        outboxCount.Should().BeGreaterThan(0);
    }

    private sealed record CheckoutResponse(Guid OrderId, string Status);
}
