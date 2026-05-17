using Payment.Application.Abstractions;

namespace Payment.Infrastructure.Services;

public sealed class PaymentDecisionService : IPaymentDecisionService
{
    public bool ShouldApprove(Guid orderId) => orderId != Guid.Empty && orderId.ToString()[^1] != '0';
}
