namespace AccountOnboarding.Api.Application.Exceptions;

public sealed class AccountNotFoundException(Guid id) : Exception($"A conta com ID '{id}' não existe no banco de dados.");

public sealed class DuplicateCpfException() : Exception("Já existe uma conta cadastrada para este CPF.");

public sealed class InvalidCpfException() : Exception("CPF inválido.");
