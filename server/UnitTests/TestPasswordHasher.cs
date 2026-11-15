using Service.Security;

namespace UnitTests;

// Uses the actual Argon2 implementation (64 MB / 3 iterations), so each hash takes a moment.
// Kept to a handful of tests on purpose.
public class TestPasswordHasher
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void HashAndSaltPassword_ThenVerifyWithSamePassword_ReturnsTrue()
    {
        var hash = _sut.HashAndSaltPassword("correct-password");

        Assert.True(_sut.VerifyHashedPassword("correct-password", hash));
    }

    [Fact]
    public void VerifyHashedPassword_WithWrongPassword_ReturnsFalse()
    {
        var hash = _sut.HashAndSaltPassword("correct-password");

        Assert.False(_sut.VerifyHashedPassword("wrong-password", hash));
    }

    [Fact]
    public void HashAndSaltPassword_SamePasswordTwice_ProducesDifferentHashes()
    {
        var first = _sut.HashAndSaltPassword("password123");
        var second = _sut.HashAndSaltPassword("password123");

        // Different random salt each time
        Assert.NotEqual(first, second);
        // The plain text must never end up in the stored value
        Assert.DoesNotContain("password123", first);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-dot-separator")]
    [InlineData("too.many.dots")]
    public void VerifyHashedPassword_WithMalformedStoredHash_ReturnsFalse(string malformedHash)
    {
        Assert.False(_sut.VerifyHashedPassword("anything", malformedHash));
    }
}