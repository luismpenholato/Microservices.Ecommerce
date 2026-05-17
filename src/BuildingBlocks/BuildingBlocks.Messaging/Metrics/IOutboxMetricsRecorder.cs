namespace BuildingBlocks.Messaging.Metrics;

public interface IOutboxMetricsRecorder
{
    void RecordPublished();

    void RecordPublishFailure();

    void RecordExhausted();
}
