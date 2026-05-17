namespace Ordering.Application.Exceptions;

public sealed class IdempotencyConflictException(string idempotencyKey)
    : Exception($"Idempotency key '{idempotencyKey}' was already used with a different request payload.")
{
    public string IdempotencyKey { get; } = idempotencyKey;
}
