using AccountOnboarding.Api.Domain;

namespace AccountOnboarding.Tests.Domain;

public sealed class CpfNormalizerTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    public void TryNormalize_ValidCpf_ReturnsDigitsOnly(string cpf, string expected)
    {
        // Arrange

        // Act
        var isValid = CpfNormalizer.TryNormalize(cpf, out var normalized);

        // Assert
        Assert.True(isValid);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("529.982.247-24")]
    [InlineData("abc.982.247-25")]
    public void TryNormalize_InvalidCpf_ReturnsFalse(string cpf)
    {
        // Arrange

        // Act
        var isValid = CpfNormalizer.TryNormalize(cpf, out _);

        // Assert
        Assert.False(isValid);
    }
}