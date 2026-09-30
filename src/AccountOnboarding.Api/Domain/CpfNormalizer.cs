using System.Text.RegularExpressions;

namespace AccountOnboarding.Api.Domain;

public static partial class CpfNormalizer
{
    public static bool TryNormalize(string value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = SeparatorsRegex().Replace(value, string.Empty);
        if (digits.Length != 11 || digits.Any(character => character is < '0' or > '9') || digits.All(character => character == digits[0]))
        {
            return false;
        }

        var firstDigit = CalculateCheckDigit(digits.AsSpan(0, 9));
        var secondDigit = CalculateCheckDigit(digits.AsSpan(0, 10));
        if (digits[9] - '0' != firstDigit || digits[10] - '0' != secondDigit)
        {
            return false;
        }

        normalized = digits;
        return true;
    }

    public static string Mask(string normalizedCpf) => $"***.***.***-{normalizedCpf[^2..]}";

    private static int CalculateCheckDigit(ReadOnlySpan<char> digits)
    {
        var weight = digits.Length + 1;
        var sum = 0;
        foreach (var digit in digits)
        {
            sum += (digit - '0') * weight--;
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    [GeneratedRegex("[.\\-/\\s]")]
    private static partial Regex SeparatorsRegex();
}