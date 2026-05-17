using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Basket.Domain.Entities;

namespace Basket.Application.Checkout;

internal static class CheckoutIdempotencyKeyFactory
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Create(Guid customerId, CustomerBasket basket)
    {
        var payload = new
        {
            customerId,
            items = basket.Items
                .OrderBy(x => x.ProductId)
                .Select(x => new { x.ProductId, x.Quantity, x.UnitPrice })
        };

        var json = JsonSerializer.Serialize(payload, Options);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return $"checkout-{customerId:N}-{Convert.ToHexString(hash)[..16]}";
    }
}
