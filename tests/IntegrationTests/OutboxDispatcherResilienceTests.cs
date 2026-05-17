using FluentAssertions;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ordering.Infrastructure.Persistence;
using BuildingBlocks.Messaging;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace IntegrationTests;

public sealed class OutboxDispatcherResilienceTests : IAsyncLifetime
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
    private TestFlakyOutboxPublisher? _flakyPublisher;

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
                builder.UseSetting("Outbox:PollIntervalSeconds", "1");
                builder.UseSetting("Outbox:MaxPublishRetries", "10");

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IOutboxPublisher>();
                    services.AddScoped<MassTransitOutboxPublisher>();
                    services.AddScoped<TestFlakyOutboxPublisher>(sp =>
                        new TestFlakyOutboxPublisher(sp.GetRequiredService<MassTransitOutboxPublisher>()));
                    services.AddScoped<IOutboxPublisher>(sp => sp.GetRequiredService<TestFlakyOutboxPublisher>());
                });
                IntegrationTestAuthHelper.ApplyJwtSettings(builder);
            });

        using var scope = _factory.Services.CreateScope();
        _flakyPublisher = scope.ServiceProvider.GetRequiredService<TestFlakyOutboxPublisher>();
        _flakyPublisher.FailuresBeforeSuccess = 2;

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
    public async Task OutboxDispatcher_Should_Keep_Message_Pending_On_Publish_Failure_Then_Mark_Processed_On_Success()
    {
        var client = _factory!.CreateClient();
        var customerId = Guid.NewGuid();
        IntegrationTestAuthHelper.SetBearerToken(client, customerId);
        var idempotencyKey = $"outbox-resilience-{Guid.NewGuid():N}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new
            {
                items = new[]
                {
                    new { productId = Guid.NewGuid(), productName = "Resilience", quantity = 1, unitPrice = 50m }
                }
            })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        await WaitUntilAsync(
            async () =>
            {
                var message = await db.OutboxMessages.OrderByDescending(x => x.CreatedAtUtc).FirstAsync();
                return message.ProcessedAtUtc is null && message.RetryCount > 0;
            },
            TimeSpan.FromSeconds(20),
            "outbox com falha de publicação e retry incrementado");

        var pending = await db.OutboxMessages.OrderByDescending(x => x.CreatedAtUtc).FirstAsync();
        pending.ProcessedAtUtc.Should().BeNull();
        pending.RetryCount.Should().BeGreaterThan(0);

        using (var scope2 = _factory.Services.CreateScope())
        {
            _flakyPublisher = scope2.ServiceProvider.GetRequiredService<TestFlakyOutboxPublisher>();
            _flakyPublisher.FailuresBeforeSuccess = 0;
        }

        await WaitUntilAsync(
            async () =>
            {
                var message = await db.OutboxMessages.OrderByDescending(x => x.CreatedAtUtc).FirstAsync();
                return message.ProcessedAtUtc is not null;
            },
            TimeSpan.FromSeconds(20),
            "outbox publicado com sucesso");

        var processed = await db.OutboxMessages.OrderByDescending(x => x.CreatedAtUtc).FirstAsync();
        processed.ProcessedAtUtc.Should().NotBeNull();
        processed.LastError.Should().BeNull();
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
}
