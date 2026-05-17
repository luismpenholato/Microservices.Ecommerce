namespace BuildingBlocks.Messaging.Metrics;

public interface IConsumerMetricsRecorder
{
    void RecordProcessed(string consumerName);

    void RecordFailed(string consumerName);
}
