using System.Net.Http.Json;
using Basket.Application.Abstractions;
using Basket.Application.Checkout;
using BuildingBlocks.Web;
using Microsoft.Extensions.Logging;

namespace Basket.Infrastructure.Http;

public sealed class OrderingHttpClient(HttpClient httpClient, ILogger<OrderingHttpClient> logger)
    : IOrderingClient
{
    public async Task<CheckoutResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new
            {
                items = request.Items.Select(x => new
                {
                    productId = x.ProductId,
                    productName = x.ProductName,
                    quantity = x.Quantity,
                    unitPrice = x.UnitPrice
                })
            })
        };

        httpRequest.Headers.TryAddWithoutValidation(IdempotencyKeyConstants.HeaderName, request.IdempotencyKey);

        var response = await httpClient.SendAsync(httpRequest, cancellationToken);

        if ((int)response.StatusCode == 409)
        {
            throw new InvalidOperationException("Checkout idempotency conflict with Ordering service.");
        }

        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Invalid order response.");

        logger.LogInformation(
            "Order {OrderId} created for customer {CustomerId}. IdempotencyKey={IdempotencyKey}",
            order.Id,
            request.CustomerId,
            request.IdempotencyKey);

        return new CheckoutResult(order.Id, order.Status);
    }

    private sealed record OrderResponse(Guid Id, string Status);
}
