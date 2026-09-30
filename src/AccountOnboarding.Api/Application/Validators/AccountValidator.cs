using AccountOnboarding.Api.Application.Exceptions;
using AccountOnboarding.Api.Application.Interface;
using AccountOnboarding.Api.Domain;
using AccountOnboarding.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountOnboarding.Api.Application.Validators;

public static class AccountValidator
{
    public static async Task<CustomerAccount> FindIncludingDeletedAsync(
        IUnitOfWorkRepository repository,
        Guid id,
        CancellationToken cancellationToken)
    {
        return await repository.Get<CustomerAccount>(item => item.Id == id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AccountNotFoundException(id);
    }

    public static void EnsureNotDeleted(CustomerAccount account, Guid id)
    {
        if (account.DeletedAtUtc is not null)
        {
            throw new AccountNotFoundException(id);
        }
    }

    public static string NormalizeCpf(string cpf)
    {
        return CpfNormalizer.TryNormalize(cpf, out var normalized)
            ? normalized
            : throw new InvalidCpfException();
    }
}
