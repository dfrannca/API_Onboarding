using AccountOnboarding.Api.Application.Responses;
using AccountOnboarding.Api.Domain;
using AccountOnboarding.Api.Domain.Entities;

namespace AccountOnboarding.Api.Application.Mappers;

public static class AccountMapper
{
    public static AccountResponse ToResponse(CustomerAccount account)
    {
        return new AccountResponse(
            account.Id,
            account.HolderName,
            CpfNormalizer.Mask(account.Cpf),
            account.Status.ToString(),
            account.CreatedAtUtc,
            account.UpdatedAtUtc,
            account.DeletedAtUtc,
            account.Version);
    }
}
