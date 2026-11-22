namespace Ordering.Application.Exceptions;

public sealed class IdempotencyRaceException : Exception
{
    public IdempotencyRaceException(string idempotencyKey)
        : base($"Concurrent create detected for idempotency key '{idempotencyKey}'.")
    {
        IdempotencyKey = idempotencyKey;
    }

    public string IdempotencyKey { get; }
}
