namespace Payment.Application.Abstractions;

public interface IPaymentDecisionService
{
    bool ShouldApprove(Guid orderId);
}
