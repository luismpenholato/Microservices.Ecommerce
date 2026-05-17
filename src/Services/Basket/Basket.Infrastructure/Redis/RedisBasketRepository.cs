using System.Text.Json;
using Basket.Application.Abstractions;
using Basket.Domain.Entities;
using StackExchange.Redis;

namespace Basket.Infrastructure.Redis;

public sealed class RedisBasketRepository(IConnectionMultiplexer connectionMultiplexer) : IBasketRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<CustomerBasket?> GetAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var value = await _database.StringGetAsync(GetKey(customerId));
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<CustomerBasket>(value.ToString()!, JsonOptions);
    }

    public async Task SaveAsync(CustomerBasket basket, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(basket, JsonOptions);
        await _database.StringSetAsync(GetKey(basket.CustomerId), payload, TimeSpan.FromDays(7));
    }

    public Task DeleteAsync(Guid customerId, CancellationToken cancellationToken) =>
        _database.KeyDeleteAsync(GetKey(customerId));

    private static string GetKey(Guid customerId) => $"basket:{customerId}";
}
