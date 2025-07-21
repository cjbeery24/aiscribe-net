using Xunit;
using SermonTranscription.Application.Services;

namespace SermonTranscription.Tests.Unit.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public void HashPassword_WithValidPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "testpassword123";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.NotEmpty(hashedPassword);
        Assert.NotEqual(password, hashedPassword);
    }

    [Fact]
    public void HashPassword_WithSamePassword_ShouldReturnDifferentHashes()
    {
        // Arrange
        var password = "testpassword123";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // Different salts should produce different hashes
    }

    [Fact]
    public void HashPassword_WithEmptyPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.NotEmpty(hashedPassword);
    }

    [Fact]
    public void HashPassword_WithSpecialCharacters_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "test@#$%^&*()_+{}|:<>?[]\\;'\",./<>?";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.NotEmpty(hashedPassword);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "testpassword123";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var correctPassword = "testpassword123";
        var wrongPassword = "wrongpassword123";
        var hashedPassword = _passwordHasher.HashPassword(correctPassword);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ShouldReturnFalse()
    {
        // Arrange
        var correctPassword = "testpassword123";
        var emptyPassword = "";
        var hashedPassword = _passwordHasher.HashPassword(correctPassword);

        // Act
        var result = _passwordHasher.VerifyPassword(emptyPassword, hashedPassword);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithEmptyHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "testpassword123";
        var emptyHash = "";

        // Act & Assert
        Assert.False(_passwordHasher.VerifyPassword(password, emptyHash));
    }

    [Fact]
    public void VerifyPassword_WithInvalidHashFormat_ShouldReturnFalse()
    {
        // Arrange
        var password = "testpassword123";
        var invalidHash = "invalid_hash_format";

        // Act
        var result = _passwordHasher.VerifyPassword(password, invalidHash);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("password123")]
    [InlineData("")]
    [InlineData("test@#$%^&*()")]
    [InlineData("verylongpasswordwithlotsofcharacters123456789")]
    public void HashAndVerifyPassword_WithVariousPasswords_ShouldWorkCorrectly(string password)
    {
        // Arrange & Act
        var hashedPassword = _passwordHasher.HashPassword(password);
        var verificationResult = _passwordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        Assert.True(verificationResult);
    }
}
