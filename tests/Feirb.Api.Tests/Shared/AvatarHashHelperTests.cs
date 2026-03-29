using Feirb.Shared.Avatars;
using FluentAssertions;

namespace Feirb.Api.Tests.Shared;

public class AvatarHashHelperTests
{
    [Fact]
    public void ComputeEmailHash_ValidEmail_ReturnsConsistentHash()
    {
        var hash1 = AvatarHashHelper.ComputeEmailHash("user@example.com");
        var hash2 = AvatarHashHelper.ComputeEmailHash("user@example.com");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeEmailHash_ReturnsLowercaseHexString()
    {
        var hash = AvatarHashHelper.ComputeEmailHash("user@example.com");

        hash.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Theory]
    [InlineData("User@Example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("user@example.com")]
    public void ComputeEmailHash_CaseInsensitive_ReturnsSameHash(string email)
    {
        var expected = AvatarHashHelper.ComputeEmailHash("user@example.com");

        AvatarHashHelper.ComputeEmailHash(email).Should().Be(expected);
    }

    [Theory]
    [InlineData(" user@example.com")]
    [InlineData("user@example.com ")]
    [InlineData("  user@example.com  ")]
    public void ComputeEmailHash_TrimsWhitespace_ReturnsSameHash(string email)
    {
        var expected = AvatarHashHelper.ComputeEmailHash("user@example.com");

        AvatarHashHelper.ComputeEmailHash(email).Should().Be(expected);
    }

    [Fact]
    public void ComputeEmailHash_DifferentEmails_ReturnDifferentHashes()
    {
        var hash1 = AvatarHashHelper.ComputeEmailHash("alice@example.com");
        var hash2 = AvatarHashHelper.ComputeEmailHash("bob@example.com");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeEmailHash_NullEmail_ThrowsArgumentNullException()
    {
        var act = () => AvatarHashHelper.ComputeEmailHash(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
