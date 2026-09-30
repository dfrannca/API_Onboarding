namespace AccountOnboarding.Api.Domain;

public static class AccountRules
{
    public static string NormalizeHolderName(string holderName)
    {
        var normalized = holderName.Trim();
        if (normalized.Length is < 2 or > 150)
        {
            throw new ArgumentException("O nome do titular deve ter entre 2 e 150 caracteres.", nameof(holderName));
        }

        return normalized;
    }

    public static void EnsureExpectedVersion(long currentVersion, long expectedVersion)
    {
        if (currentVersion != expectedVersion)
        {
            throw new AccountConcurrencyException();
        }
    }
}
