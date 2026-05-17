using Basket.Application.Abstractions;
using Basket.Infrastructure.Http;
using Basket.Infrastructure.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Basket.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBasketInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' not configured.");

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<IBasketRepository, RedisBasketRepository>();

        services.AddTransient<BearerTokenForwardingHandler>();
        services.AddHttpClient<IOrderingClient, OrderingHttpClient>(client =>
        {
            var baseUrl = configuration["Services:Ordering"] ?? "http://localhost:5003";
            client.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler<BearerTokenForwardingHandler>()
        .AddStandardResilienceHandler();

        services.AddHealthChecks()
            .AddRedis(redisConnection, name: "redis", tags: ["ready"]);

        return services;
    }
}
