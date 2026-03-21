using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Feirb.Api.Data.Entities;
using Feirb.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Feirb.Api.Tests.Services;

public class AuthServiceTests
{
    private static readonly JwtSettings _testJwtSettings = new()
    {
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        Key = "test-secret-key-that-is-at-least-32-characters-long!",
        AccessTokenExpiryMinutes = 60,
        RefreshTokenExpiryDays = 7,
    };

    private readonly AuthService _sut = new(Options.Create(_testJwtSettings));

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashedString()
    {
        var hash = _sut.HashPassword("MySecurePassword123");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("MySecurePassword123");
        hash.Should().StartWith("$2");
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var password = "MySecurePassword123";
        var hash = _sut.HashPassword(password);

        _sut.VerifyPassword(password, hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = _sut.HashPassword("CorrectPassword");

        _sut.VerifyPassword("WrongPassword", hash).Should().BeFalse();
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ProducesDifferentHashes()
    {
        var password = "MySecurePassword123";

        var hash1 = _sut.HashPassword(password);
        var hash2 = _sut.HashPassword(password);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GenerateTokens_ValidUser_ReturnsTokenResponse()
    {
        var user = CreateTestUser();

        var result = _sut.GenerateTokens(user);

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateTokens_ValidUser_AccessTokenContainsExpectedClaims()
    {
        var user = CreateTestUser();

        var result = _sut.GenerateTokens(user);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.AccessToken);

        token.Issuer.Should().Be(_testJwtSettings.Issuer);
        token.Audiences.Should().Contain(_testJwtSettings.Audience);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.Username);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
    }

    [Fact]
    public void GenerateTokens_CalledTwice_ProducesDifferentRefreshTokens()
    {
        var user = CreateTestUser();

        var result1 = _sut.GenerateTokens(user);
        var result2 = _sut.GenerateTokens(user);

        result1.RefreshToken.Should().NotBe(result2.RefreshToken);
    }

    [Fact]
    public void ValidateAccessTokenAsync_ValidToken_ReturnsUserId()
    {
        var user = CreateTestUser();
        var tokens = _sut.GenerateTokens(user);

        var result = _sut.ValidateAccessTokenAsync(tokens.AccessToken);

        result.Should().Be(user.Id);
    }

    [Fact]
    public void ValidateAccessTokenAsync_InvalidToken_ReturnsNull()
    {
        var result = _sut.ValidateAccessTokenAsync("invalid-token");

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessTokenAsync_TokenFromDifferentKey_ReturnsNull()
    {
        var otherSettings = new JwtSettings
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            Key = "a-completely-different-key-that-is-also-32-chars!!",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7,
        };
        var otherService = new AuthService(Options.Create(otherSettings));
        var tokens = otherService.GenerateTokens(CreateTestUser());

        var result = _sut.ValidateAccessTokenAsync(tokens.AccessToken);

        result.Should().BeNull();
    }

    private static User CreateTestUser() => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Username = "testuser",
        Email = "test@example.com",
        PasswordHash = "hashed",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };
}
