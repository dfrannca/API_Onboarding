using AccountOnboarding.Api.Application.Requests;
using AccountOnboarding.Api.Application.Responses;
using AccountOnboarding.Api.Domain.Enums;

namespace AccountOnboarding.Api.Application.Interface;

public interface IAccountService
{
    Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken);
    Task<AccountResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountResponse>> ListAsync(string? cpf, AccountStatus? status, CancellationToken cancellationToken);
    Task<AccountResponse> UpdateAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, long expectedVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountAuditResponse>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
}