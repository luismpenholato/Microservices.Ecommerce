using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Web;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Infrastructure.Persistence;
using IntegrationTests.Infrastructure;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace IntegrationTests;

public sealed class OrderingOutboxDispatcherTests : IAsyncLifetime
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
    public async Task OutboxDispatcher_Should_Mark_Message_As_Processed_After_Publish()
    {
        var client = _factory!.CreateClient();
        var idempotencyKey = $"outbox-{Guid.NewGuid():N}";
        var customerId = Guid.NewGuid();
        IntegrationTestAuthHelper.SetBearerToken(client, customerId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new
            {
                items = new[]
                {
                    new { productId = Guid.NewGuid(), productName = "Outbox", quantity = 1, unitPrice = 99m }
                }
            })
        };
        request.Headers.Add(IdempotencyKeyConstants.HeaderName, idempotencyKey);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var processed = await WaitForOutboxProcessedAsync(TimeSpan.FromSeconds(30));
        processed.Should().BeTrue("o OutboxDispatcher deve publicar e marcar ProcessedAtUtc");
    }

    [Fact]
    public async Task Duplicate_PaymentApprovedEvent_Should_Be_Processed_Once()
    {
        var orderId = await CreateOrderAsync();
        var eventId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var integrationEvent = new PaymentApprovedEvent
        {
            EventId = eventId,
            CorrelationId = correlationId,
            OrderId = orderId,
            CustomerId = Guid.NewGuid(),
            Amount = 10m,
            TransactionId = "test-tx",
            Items = []
        };

        using var scope = _factory!.Services.CreateScope();
        var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        await publish.Publish(integrationEvent);
        await publish.Publish(integrationEvent);

        await Task.Delay(TimeSpan.FromSeconds(5));

        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var processedCount = await db.ProcessedIntegrationEvents
            .CountAsync(x => x.EventId == eventId && x.ConsumerName == nameof(Ordering.Infrastructure.Messaging.Consumers.PaymentApprovedConsumer));

        processedCount.Should().Be(1);

        var order = await db.Orders.AsNoTracking().FirstAsync(x => x.Id == orderId);
        order.Status.ToString().Should().Be("PaymentApproved");
    }

    private async Task<Guid> CreateOrderAsync()
    {
        var client = _factory!.CreateClient();
        var customerId = Guid.NewGuid();
        IntegrationTestAuthHelper.SetBearerToken(client, customerId);
        var idempotencyKey = $"dup-{Guid.NewGuid():N}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new
            {
                items = new[]
                {
                    new { productId = Guid.NewGuid(), productName = "Dup", quantity = 1, unitPrice = 10m }
                }
            })
        };
        request.Headers.Add(IdempotencyKeyConstants.HeaderName, idempotencyKey);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>();
        return body!.Id;
    }

    private async Task<bool> WaitForOutboxProcessedAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            using var scope = _factory!.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            var pending = await db.OutboxMessages.AnyAsync(x => x.ProcessedAtUtc == null);

            if (!pending)
            {
                var anyProcessed = await db.OutboxMessages.AnyAsync(x => x.ProcessedAtUtc != null);
                return anyProcessed;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return false;
    }

    private sealed record OrderResponse(Guid Id, string Status);
}
