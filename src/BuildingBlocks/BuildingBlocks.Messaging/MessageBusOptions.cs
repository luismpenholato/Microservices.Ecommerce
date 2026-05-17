namespace BuildingBlocks.Messaging;

public sealed class MessageBusOptions
{
    public const string SectionName = "MessageBus";

    public int RetryLimit { get; set; } = 3;

    public int RetryIntervalSeconds { get; set; } = 2;
}
