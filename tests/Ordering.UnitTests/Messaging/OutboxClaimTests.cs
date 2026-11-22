using BuildingBlocks.Messaging;
using FluentAssertions;

namespace Ordering.UnitTests.Messaging;

public class OutboxClaimTests
{
    [Fact]
    public void BuildClaimSql_Should_Use_Skip_Locked_For_Concurrent_Dispatchers()
    {
        var sql = OutboxClaim.BuildClaimSql("outbox_messages");

        sql.Should().Contain("FOR UPDATE SKIP LOCKED");
        sql.Should().Contain("ProcessedAtUtc");
        sql.Should().Contain("outbox_messages");
        sql.Should().Contain("ORDER BY \"CreatedAtUtc\"");
    }
}
