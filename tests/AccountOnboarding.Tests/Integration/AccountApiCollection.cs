namespace AccountOnboarding.Tests.Integration;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AccountApiCollection : ICollectionFixture<AccountApiFactory>
{
    public const string Name = "Account API";
}