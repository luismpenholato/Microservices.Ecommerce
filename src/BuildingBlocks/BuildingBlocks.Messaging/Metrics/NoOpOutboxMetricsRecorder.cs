namespace BuildingBlocks.Messaging.Metrics;

public sealed class NoOpOutboxMetricsRecorder : IOutboxMetricsRecorder
{
    public void RecordPublished() { }

    public void RecordPublishFailure() { }

    public void RecordExhausted() { }
}
