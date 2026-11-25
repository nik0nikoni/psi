using TicketGenerator.Core.Validation;

namespace TicketGenerator.UnitTests;

public sealed class StudentNameValidatorTests
{
    private readonly StudentNameValidator _validator = new();

    [Theory]
    [InlineData("Иванов", "Иванов")]
    [InlineData("  Иванов  ", "Иванов")]
    [InlineData("Анна Мария", "Анна Мария")]
    public void TryValidate_ValidName_ReturnsTrimmedValue(string input, string expected)
    {
        var isValid = _validator.TryValidate(input, out var normalizedName);

        Assert.True(isValid);
        Assert.Equal(expected, normalizedName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t \r\n")]
    public void TryValidate_EmptyOrWhitespaceName_IsRejected(string input)
    {
        var isValid = _validator.TryValidate(input, out var normalizedName);

        Assert.False(isValid);
        Assert.Equal(string.Empty, normalizedName);
    }

    [Fact]
    public void TryValidate_NullName_IsRejected()
    {
        var isValid = _validator.TryValidate(null, out var normalizedName);

        Assert.False(isValid);
        Assert.Equal(string.Empty, normalizedName);
    }
}
