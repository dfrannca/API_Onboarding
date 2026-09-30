namespace AccountOnboarding.Api.Domain;

public sealed class AccountConcurrencyException() : Exception("A conta foi alterada por outra operação. Consulte a versão atual e tente novamente.");