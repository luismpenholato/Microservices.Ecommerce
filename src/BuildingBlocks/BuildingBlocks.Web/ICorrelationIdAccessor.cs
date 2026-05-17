namespace BuildingBlocks.Web;

public interface ICorrelationIdAccessor
{
    Guid Get();
}
