using System.Net.Http.Json;
using BuildingBlocks.Contracts;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Domain.Enums;
using Ordering.Infrastructure.Messaging.Consumers;
using Ordering.Infrastructure.Persistence;
using BuildingBlocks.Messaging;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace IntegrationTests;

public sealed class ConsumerTransientFailureTests : IAsyncLifetime
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
    private TestConsumerExecutionFaultHook? _faultHook;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();

        _faultHook = new TestConsumerExecutionFaultHook
        {
            TargetConsumerName = nameof(PaymentApprovedConsumer)
        };

        _factory = new WebApplicationFactory<global::Ordering.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:OrderingDb", _postgres.GetConnectionString());
                builder.UseSetting("RabbitMq:Host", _rabbitMq.Hostname);
                builder.UseSetting("RabbitMq:Username", "guest");
                builder.UseSetting("RabbitMq:Password", "guest");
                builder.UseSetting("MessageBus:RetryLimit", "5");
                builder.UseSetting("MessageBus:RetryIntervalSeconds", "1");

                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IConsumerExecutionFaultHook>(_faultHook!);
                });
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
    public async Task Consumer_Should_Not_Mark_Processed_On_Failure_Then_Succeed_On_Retry_Without_Duplicate_Effect()
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
            Amount = 42m,
            TransactionId = "tx-transient",
            Items = []
        };

        using var scope = _factory!.Services.CreateScope();
        var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await publish.Publish(integrationEvent);

        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        await WaitUntilAsync(
            () => Task.FromResult(_faultHook!.GetAttemptCount(eventId) >= 1),
            TimeSpan.FromSeconds(20),
            "primeira tentativa com falha simulada");

        var processedDuringFailure = await db.ProcessedIntegrationEvents
            .AnyAsync(x => x.EventId == eventId && x.ConsumerName == nameof(PaymentApprovedConsumer));
        processedDuringFailure.Should().BeFalse("falha deve reverter a transação");

        await WaitUntilAsync(
            async () =>
            {
                var processed = await db.ProcessedIntegrationEvents
                    .AnyAsync(x => x.EventId == eventId && x.ConsumerName == nameof(PaymentApprovedConsumer));
                if (!processed)
                {
                    return false;
                }

                var order = await db.Orders.AsNoTracking().FirstAsync(x => x.Id == orderId);
                return order.Status == OrderStatus.PaymentApproved;
            },
            TimeSpan.FromSeconds(30),
            "retry bem-sucedido");

        var processedCount = await db.ProcessedIntegrationEvents.CountAsync(
            x => x.EventId == eventId && x.ConsumerName == nameof(PaymentApprovedConsumer));
        processedCount.Should().Be(1);

        await publish.Publish(integrationEvent);
        await Task.Delay(TimeSpan.FromSeconds(3));

        processedCount = await db.ProcessedIntegrationEvents.CountAsync(
            x => x.EventId == eventId && x.ConsumerName == nameof(PaymentApprovedConsumer));
        processedCount.Should().Be(1);

        var finalOrder = await db.Orders.AsNoTracking().FirstAsync(x => x.Id == orderId);
        finalOrder.Status.Should().Be(OrderStatus.PaymentApproved);
    }

    private async Task<Guid> CreateOrderAsync()
    {
        var client = _factory!.CreateClient();
        var customerId = Guid.NewGuid();
        IntegrationTestAuthHelper.SetBearerToken(client, customerId);
        var idempotencyKey = $"transient-{Guid.NewGuid():N}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new
            {
                items = new[]
                {
                    new { productId = Guid.NewGuid(), productName = "Item", quantity = 1, unitPrice = 10m }
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
