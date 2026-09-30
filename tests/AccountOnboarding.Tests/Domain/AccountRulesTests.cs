using AccountOnboarding.Api.Domain;

namespace AccountOnboarding.Tests.Domain;

public sealed class AccountRulesTests
{
    [Theory]
    [InlineData("  Ana Silva  ", "Ana Silva")]
    [InlineData("Jo", "Jo")]
    public void NormalizeHolderName_TrimsValidName(string input, string expected)
    {
        // Arrange

        // Act
        var normalizedName = AccountRules.NormalizeHolderName(input);

        // Assert
        Assert.Equal(expected, normalizedName);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("A")]
    public void NormalizeHolderName_RejectsNamesShorterThanTwoCharacters(string input)
    {
        // Arrange

        // Act
        var action = () => AccountRules.NormalizeHolderName(input);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void EnsureExpectedVersion_RejectsStaleVersion()
    {
        // Arrange
        const int currentVersion = 4;
        const int expectedVersion = 3;

        // Act
        var action = () => AccountRules.EnsureExpectedVersion(currentVersion, expectedVersion);

        // Assert
        Assert.Throws<AccountConcurrencyException>(action);
    }

    [Fact]
    public void EnsureExpectedVersion_AcceptsCurrentVersion()
    {
        // Arrange
        const int currentVersion = 4;
        const int expectedVersion = 4;

        // Act
        var action = () => AccountRules.EnsureExpectedVersion(currentVersion, expectedVersion);

        // Assert
        action();
    }
}
