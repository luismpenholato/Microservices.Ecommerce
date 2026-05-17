namespace Ordering.Application.Abstractions;

public interface IOrderMetrics
{
    void RecordOrderCreated();

    void RecordOrderCompleted();

    void RecordOrderCancelled();

    void RecordOrderFailed();
}
