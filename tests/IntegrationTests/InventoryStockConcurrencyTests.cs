using System.Net.Http.Json;
using BuildingBlocks.Contracts;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Domain.Enums;
using Ordering.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence;
using IntegrationTests.Infrastructure;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace IntegrationTests;

public sealed class InventoryStockConcurrencyTests : IAsyncLifetime
{
    private static readonly Guid ProductId = Guid.Parse("11111111-1111-1111-1111-111111111101");

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("postgres")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .Build();

    private WebApplicationFactory<global::Ordering.Api.Program>? _orderingFactory;
    private WebApplicationFactory<global::Inventory.Api.Program>? _inventoryFactory;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();

        await EnsureDatabasesAsync();

        var orderingConnection = BuildConnectionString("ordering_db");
        var inventoryConnection = BuildConnectionString("inventory_db");

        _orderingFactory = new WebApplicationFactory<global::Ordering.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:OrderingDb", orderingConnection);
                builder.UseSetting("RabbitMq:Host", _rabbitMq.Hostname);
                builder.UseSetting("RabbitMq:Username", "guest");
                builder.UseSetting("RabbitMq:Password", "guest");
                builder.UseSetting("MessageBus:RetryIntervalSeconds", "1");
                builder.UseSetting("Outbox:PollIntervalSeconds", "1");
                IntegrationTestAuthHelper.ApplyJwtSettings(builder);
            });

        _inventoryFactory = new WebApplicationFactory<global::Inventory.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:InventoryDb", inventoryConnection);
                builder.UseSetting("RabbitMq:Host", _rabbitMq.Hostname);
                builder.UseSetting("RabbitMq:Username", "guest");
                builder.UseSetting("RabbitMq:Password", "guest");
                builder.UseSetting("MessageBus:RetryIntervalSeconds", "1");
                builder.UseSetting("Outbox:PollIntervalSeconds", "1");
            });

        using (var scope = _orderingFactory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().Database.MigrateAsync();
        }

        using (var scope = _inventoryFactory.Services.CreateScope())
        {
            var inventoryDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            await inventoryDb.Database.MigrateAsync();
            await SeedSingleUnitStockAsync(inventoryDb);
        }
    }

    public async Task DisposeAsync()
    {
        if (_inventoryFactory is not null)
        {
            await _inventoryFactory.DisposeAsync();
        }

        if (_orderingFactory is not null)
        {
            await _orderingFactory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    [Fact]
    public async Task Concurrent_Reservations_With_Stock_One_Should_Reserve_Once_And_Fail_Once()
    {
        var orderId1 = await CreateOrderAsync();
        var orderId2 = await CreateOrderAsync();
        var correlationId = Guid.NewGuid();

        var event1 = new PaymentApprovedEvent
        {
            EventId = Guid.NewGuid(),
            CorrelationId = correlationId,
            OrderId = orderId1,
            CustomerId = Guid.NewGuid(),
            Amount = 10m,
            TransactionId = "tx-1",
            Items = [new OrderItemDto(ProductId, "Notebook", 1, 10m)]
        };

        var event2 = new PaymentApprovedEvent
        {
            EventId = Guid.NewGuid(),
            CorrelationId = correlationId,
            OrderId = orderId2,
            CustomerId = Guid.NewGuid(),
            Amount = 10m,
            TransactionId = "tx-2",
            Items = [new OrderItemDto(ProductId, "Notebook", 1, 10m)]
        };

        using var inventoryScope = _inventoryFactory!.Services.CreateScope();
        var publish = inventoryScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        await Task.WhenAll(
            publish.Publish(event1),
            publish.Publish(event2));

        await WaitUntilAsync(
            async () =>
            {
                using var orderingScope = _orderingFactory!.Services.CreateScope();
                var orderingDb = orderingScope.ServiceProvider.GetRequiredService<OrderingDbContext>();
                using var invScope = _inventoryFactory.Services.CreateScope();
                var inventoryDb = invScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var reservedCount = await inventoryDb.OutboxMessages.CountAsync(
                    x => x.EventType == nameof(StockReservedEvent) && x.ProcessedAtUtc != null);
                var failedCount = await inventoryDb.OutboxMessages.CountAsync(
                    x => x.EventType == nameof(StockReservationFailedEvent) && x.ProcessedAtUtc != null);

                if (reservedCount < 1 || failedCount < 1)
                {
                    return false;
                }

                var order1 = await orderingDb.Orders.AsNoTracking().FirstAsync(x => x.Id == orderId1);
                var order2 = await orderingDb.Orders.AsNoTracking().FirstAsync(x => x.Id == orderId2);

                var statuses = new[] { order1.Status, order2.Status };
                return statuses.Contains(OrderStatus.Completed) && statuses.Contains(OrderStatus.Failed);
            },
            TimeSpan.FromSeconds(60),
            "reserva concorrente processada");

        using var finalInventoryScope = _inventoryFactory.Services.CreateScope();
        var finalInventoryDb = finalInventoryScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var reservedOutbox = await finalInventoryDb.OutboxMessages.CountAsync(x => x.EventType == nameof(StockReservedEvent));
        var failedOutbox = await finalInventoryDb.OutboxMessages.CountAsync(x => x.EventType == nameof(StockReservationFailedEvent));
        reservedOutbox.Should().Be(1);
        failedOutbox.Should().Be(1);

        var product = await finalInventoryDb.ProductInventories.AsNoTracking()
            .FirstAsync(x => x.ProductId == ProductId);
        product.AvailableQuantity.Should().BeGreaterThanOrEqualTo(0);
        (product.AvailableQuantity + product.ReservedQuantity).Should().BeLessThanOrEqualTo(1);
    }

    private string BuildConnectionString(string database)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = database
        };
        return builder.ConnectionString;
    }

    private async Task EnsureDatabasesAsync()
    {
        await using var connection = new Npgsql.NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        foreach (var dbName in new[] { "ordering_db", "inventory_db" })
        {
            await using var cmd = new Npgsql.NpgsqlCommand(
                $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'",
                connection);
            var exists = await cmd.ExecuteScalarAsync() is not null;
            if (!exists)
            {
                await using var create = new Npgsql.NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", connection);
                await create.ExecuteNonQueryAsync();
            }
        }
    }

    private static async Task SeedSingleUnitStockAsync(InventoryDbContext db)
    {
        var product = await db.ProductInventories.FirstOrDefaultAsync(x => x.ProductId == ProductId);
        if (product is null)
        {
            db.ProductInventories.Add(new Inventory.Domain.Entities.ProductInventory(ProductId, 1));
        }
        else
        {
            product.UpdateAvailableQuantity(1);
        }

        await db.SaveChangesAsync();
    }

    private async Task<Guid> CreateOrderAsync()
    {
        var client = _orderingFactory!.CreateClient();
        var customerId = Guid.NewGuid();
        IntegrationTestAuthHelper.SetBearerToken(client, customerId);
        var idempotencyKey = $"concurrency-{Guid.NewGuid():N}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new
            {
                items = new[]
                {
                    new { productId = ProductId, productName = "Notebook", quantity = 1, unitPrice = 10m }
                }
            })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>();
        return body!.Id;
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout, string description)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException($"Condition not met: {description}");
    }

    private sealed record OrderResponse(Guid Id, string Status);
}
