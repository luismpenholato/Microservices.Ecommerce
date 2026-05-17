namespace BuildingBlocks.Messaging;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollIntervalSeconds { get; set; } = 2;

    public int BatchSize { get; set; } = 20;

    public int MaxPublishRetries { get; set; } = 5;
}
