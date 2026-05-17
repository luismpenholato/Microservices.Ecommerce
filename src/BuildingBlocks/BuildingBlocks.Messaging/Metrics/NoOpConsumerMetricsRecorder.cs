namespace BuildingBlocks.Messaging.Metrics;

public sealed class NoOpConsumerMetricsRecorder : IConsumerMetricsRecorder
{
    public void RecordProcessed(string consumerName)
    {
    }

    public void RecordFailed(string consumerName)
    {
    }
}
