namespace Inventory.Application.Abstractions;

public interface IStockReservationMetrics
{
    void RecordReservationApproved();

    void RecordReservationFailed();
}
