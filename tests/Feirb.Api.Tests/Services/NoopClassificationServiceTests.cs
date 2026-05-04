using Feirb.Api.Data.Entities;
using Feirb.Api.Services;
using FluentAssertions;

namespace Feirb.Api.Tests.Services;

public class NoopClassificationServiceTests
{
    [Fact]
    public async Task Classify_ReturnsSuccessWithEmptyLabelsArrayAsync()
    {
        var service = new NoopClassificationService();
        var message = new CachedMessage
        {
            Id = Guid.NewGuid(),
            MailboxId = Guid.NewGuid(),
            MessageId = "<test@example.com>",
            Subject = "Test",
            From = "sender@example.com",
            To = "recipient@example.com",
            Date = DateTimeOffset.UtcNow,
            SyncedAt = DateTimeOffset.UtcNow,
        };

        var result = await service.ClassifyAsync(message);

        result.Success.Should().BeTrue();
        // Must be a JSON array to match the contract ClassificationService enforces and
        // that the classify endpoint persists into ClassificationResult.Result.
        result.Result.Should().Be("[]");
        result.Error.Should().BeNull();
    }
}
