using System.Text.Json;
using BuildingBlocks.Contracts;

namespace BuildingBlocks.Messaging;

public static class IntegrationEventSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<TEvent>(TEvent integrationEvent) where TEvent : IntegrationEvent =>
        JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), Options);

    public static IntegrationEvent Deserialize(string payload, string eventType)
    {
        var type = ResolveType(eventType)
            ?? throw new InvalidOperationException($"Unknown event type '{eventType}'.");

        return (JsonSerializer.Deserialize(payload, type, Options) as IntegrationEvent)
            ?? throw new InvalidOperationException($"Could not deserialize event '{eventType}'.");
    }

    private static Type? ResolveType(string eventType) =>
        Type.GetType($"BuildingBlocks.Contracts.{eventType}, BuildingBlocks.Contracts");
}
