using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Infrastructure.Persistence;
using IntegrationTests.Infrastructure;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace IntegrationTests;

public sealed class OrderingOutboxAndIdempotencyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("ordering_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .Build();

    private WebApplicationFactory<global::Ordering.Api.Program>? _factory;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();

        _factory = new WebApplicationFactory<global::Ordering.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:OrderingDb", _postgres.GetConnectionString());
                builder.UseSetting("RabbitMq:Host", _rabbitMq.Hostname);
                builder.UseSetting("RabbitMq:Username", "guest");
                builder.UseSetting("RabbitMq:Password", "guest");
                IntegrationTestAuthHelper.ApplyJwtSettings(builder);
            });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    [Fact]
    public async Task CreateOrder_Should_Persist_Outbox_Message_In_Same_Transaction()
    {
        var client = _factory!.CreateClient();
        var idempotencyKey = $"it-{Guid.NewGuid():N}";
        var customerId = Guid.NewGuid();

        IntegrationTestAuthHelper.SetBearerToken(client, customerId);
        var response = await PostOrderRawAsync(client, new
        {
            items = new[]
            {
                new { productId = Guid.NewGuid(), productName = "Item", quantity = 1, unitPrice = 10m }
            }
        }, idempotencyKey);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var outboxCount = await db.OutboxMessages.CountAsync();
        outboxCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateOrder_With_Same_IdempotencyKey_Should_Return_Same_OrderId()
    {
        var client = _factory!.CreateClient();
        var idempotencyKey = $"idem-{Guid.NewGuid():N}";
        var customerId = Guid.NewGuid();
        IntegrationTestAuthHelper.SetBearerToken(client, customerId);
        var payload = new
        {
            items = new[]
            {
                new { productId = Guid.NewGuid(), productName = "Item", quantity = 1, unitPrice = 15m }
            }
        };

        var first = await PostOrderAsync(client, payload, idempotencyKey);
        var second = await PostOrderAsync(client, payload, idempotencyKey);

        first.Id.Should().Be(second.Id);
    }

    [Fact]
    public async Task CreateOrder_With_Same_Key_And_Different_Payload_Should_Return_409()
    {
        var client = _factory!.CreateClient();
        var idempotencyKey = $"conflict-{Guid.NewGuid():N}";
        var customerId = Guid.NewGuid();

        IntegrationTestAuthHelper.SetBearerToken(client, customerId);
        await PostOrderAsync(client, new
        {
            items = new[] { new { productId = Guid.NewGuid(), productName = "A", quantity = 1, unitPrice = 10m } }
        }, idempotencyKey);

        var response = await PostOrderRawAsync(client, new
        {
            items = new[] { new { productId = Guid.NewGuid(), productName = "B", quantity = 2, unitPrice = 20m } }
        }, idempotencyKey);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static async Task<HttpResponseMessage> PostOrderRawAsync(
        HttpClient client,
        object payload,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add(IdempotencyKeyConstants.HeaderName, idempotencyKey);
        return await client.SendAsync(request);
    }

    private static async Task<OrderResponse> PostOrderAsync(HttpClient client, object payload, string idempotencyKey)
    {
        var response = await PostOrderRawAsync(client, payload, idempotencyKey);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }

    private sealed record OrderResponse(Guid Id, string Status);
}
