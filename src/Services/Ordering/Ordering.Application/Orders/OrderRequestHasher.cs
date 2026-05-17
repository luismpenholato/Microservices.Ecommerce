using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ordering.Application.Orders.Commands;

namespace Ordering.Application.Orders;

internal static class OrderRequestHasher
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Compute(CreateOrderCommand command)
    {
        var payload = new
        {
            command.CustomerId,
            Items = command.Items
                .OrderBy(x => x.ProductId)
                .Select(x => new { x.ProductId, x.ProductName, x.Quantity, x.UnitPrice })
        };

        var json = JsonSerializer.Serialize(payload, Options);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }
}
