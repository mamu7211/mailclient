using Feirb.Api.Data;
using Feirb.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Feirb.Api.Tests.Services;

public class EmailServiceTests : IDisposable
{
    private readonly FeirbDbContext _db;
    private readonly EmailService _service;

    public EmailServiceTests()
    {
        var options = new DbContextOptionsBuilder<FeirbDbContext>()
            .UseInMemoryDatabase($"EmailServiceTest-{Guid.NewGuid()}")
            .Options;
        _db = new FeirbDbContext(options);

        var dataProtection = DataProtectionProvider.Create("Tests");
        var logger = NullLogger<EmailService>.Instance;

        _service = new EmailService(_db, dataProtection, logger);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SendAsync_NoSmtpSettings_ReturnsFalseAsync()
    {
        var result = await _service.SendAsync("test@example.com", "Test", "<p>Test</p>");

        result.Should().BeFalse();
    }
}
