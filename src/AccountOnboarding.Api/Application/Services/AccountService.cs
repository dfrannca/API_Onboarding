using AccountOnboarding.Api.Domain;
using AccountOnboarding.Api.Domain.Entities;
using AccountOnboarding.Api.Domain.Enums;
using AccountOnboarding.Api.Application.Exceptions;
using AccountOnboarding.Api.Application.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using AccountOnboarding.Api.Application.Interface;
using AccountOnboarding.Api.Application.Responses;
using AccountOnboarding.Api.Application.Validators;
using AccountOnboarding.Api.Application.Mappers;

namespace AccountOnboarding.Api.Application.Services;

public sealed class AccountService(
    IUnitOfWorkRepository repository,
    IMemoryCache cache,
    IAccountEventPublisher eventPublisher) : IAccountService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    // -------------------------------------------------------------------------
    // Criação
    // -------------------------------------------------------------------------

    public async Task<AccountResponse> CreateAsync(
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var cpf = AccountValidator.NormalizeCpf(request.Cpf);

        if (await repository.Get<CustomerAccount>(account => account.Cpf == cpf && account.DeletedAtUtc == null)
                .AnyAsync(cancellationToken))
        {
            throw new DuplicateCpfException();
        }

        var now = DateTimeOffset.UtcNow;
        var account = new CustomerAccount
        {
            Id = Guid.NewGuid(),
            HolderName = AccountRules.NormalizeHolderName(request.HolderName),
            Cpf = cpf,
            Status = request.Status,
            CreatedAtUtc = now,
            Version = 1
        };

        var auditEvent = NewAudit(
            account.Id,
            AccountOperation.Created,
            now);

        var integrationEvent = NewIntegrationEvent(
            account.Id,
            AccountOperation.Created,
            now);

        account.AuditEvents.Add(auditEvent);
        await repository.Create(account, cancellationToken);
        await repository.Create(integrationEvent, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var response = AccountMapper.ToResponse(account);
        cache.Set(CacheKey(account.Id), response, CacheTtl);

        await eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        return response;
    }

    // -------------------------------------------------------------------------
    // Consultas
    // -------------------------------------------------------------------------

    public async Task<AccountResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey(id), out AccountResponse? cached) && cached is not null)
        {
            return cached;
        }

        var account = await repository.Get<CustomerAccount>(item => item.Id == id && item.DeletedAtUtc == null)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AccountNotFoundException(id);

        var response = AccountMapper.ToResponse(account);
        cache.Set(CacheKey(id), response, CacheTtl);

        return response;
    }

    public async Task<IReadOnlyList<AccountResponse>> ListAsync(
        string? cpf,
        AccountStatus? status,
        CancellationToken cancellationToken)
    {
        var query = repository.GetAll<CustomerAccount>()
            .AsNoTracking()
            .Where(account => account.DeletedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(cpf))
        {
            var normalizedCpf = AccountValidator.NormalizeCpf(cpf);
            query = query.Where(account => account.Cpf == normalizedCpf);
        }

        if (status.HasValue)
        {
            query = query.Where(account => account.Status == status.Value);
        }

        var accounts = await query
            .OrderBy(account => account.CreatedAtUtc)
            .ThenBy(account => account.Id)
            .ToListAsync(cancellationToken);

        return accounts
            .Select(AccountMapper.ToResponse)
            .ToArray();
    }

    public async Task<IReadOnlyList<AccountAuditResponse>> GetHistoryAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        _ = await AccountValidator.FindIncludingDeletedAsync(repository, id, cancellationToken);

        return await repository.Get<AccountAuditEvent>(item => item.AccountId == id)
            .AsNoTracking()
            .Where(item => item.AccountId == id)
            .OrderBy(item => item.OccurredAtUtc)
            .ThenBy(item => item.Id)
            .Select(item => new AccountAuditResponse(
                item.Id,
                item.AccountId,
                item.Operation,
                item.OccurredAtUtc))
            .ToListAsync(cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Atualização
    // -------------------------------------------------------------------------

    public async Task<AccountResponse> UpdateAsync(
        Guid id,
        UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var existingAccount = await AccountValidator.FindIncludingDeletedAsync(repository, id, cancellationToken);

        AccountValidator.EnsureNotDeleted(existingAccount, id);
        AccountRules.EnsureExpectedVersion(existingAccount.Version, request.ExpectedVersion);

        var cpf = AccountValidator.NormalizeCpf(request.Cpf);

        if (await repository.Get<CustomerAccount>(item => item.Id != id && item.Cpf == cpf && item.DeletedAtUtc == null)
                .AnyAsync(cancellationToken))
        {
            throw new DuplicateCpfException();
        }

        var now = DateTimeOffset.UtcNow;

        existingAccount.HolderName = AccountRules.NormalizeHolderName(request.HolderName);
        existingAccount.Cpf = cpf;
        existingAccount.Status = request.Status;
        existingAccount.UpdatedAtUtc = now;
        existingAccount.Version++;

        await repository.Create(
            NewAudit(id, AccountOperation.Updated, now), cancellationToken);

        var integrationEvent = NewIntegrationEvent(
            id,
            AccountOperation.Updated,
            now);

        await repository.Create(integrationEvent, cancellationToken);
        await repository.Update(existingAccount);

        await repository.SaveChangesAsync(cancellationToken);

        cache.Remove(CacheKey(id));

        await eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        return AccountMapper.ToResponse(existingAccount);
    }

    // -------------------------------------------------------------------------
    // Exclusão lógica
    // -------------------------------------------------------------------------

    public async Task DeleteAsync(
        Guid id,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var account = await AccountValidator.FindIncludingDeletedAsync(repository, id, cancellationToken);

        AccountValidator.EnsureNotDeleted(account, id);
        AccountRules.EnsureExpectedVersion(account.Version, expectedVersion);

        var now = DateTimeOffset.UtcNow;

        account.DeletedAtUtc = now;
        account.UpdatedAtUtc = now;
        account.Version++;

        await repository.Create(
            NewAudit(id, AccountOperation.Deleted, now), cancellationToken);

        var integrationEvent = NewIntegrationEvent(
            id,
            AccountOperation.Deleted,
            now);

        await repository.Create(integrationEvent, cancellationToken);
        await repository.Update(account);

        await repository.SaveChangesAsync(cancellationToken);

        cache.Remove(CacheKey(id));

        await eventPublisher.PublishAsync(integrationEvent, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // -------------------------------------------------------------------------
    // Mapeamento e criação de eventos
    // -------------------------------------------------------------------------

    private static AccountAuditEvent NewAudit(
        Guid accountId,
        AccountOperation operation,
        DateTimeOffset occurredAtUtc)
    {
        return new AccountAuditEvent
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Operation = operation,
            OccurredAtUtc = occurredAtUtc
        };
    }

    private static AccountIntegrationEvent NewIntegrationEvent(
        Guid accountId,
        AccountOperation operation,
        DateTimeOffset occurredAtUtc)
    {
        return new AccountIntegrationEvent
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Operation = operation,
            OccurredAtUtc = occurredAtUtc
        };
    }

    // -------------------------------------------------------------------------
    // Cache
    // -------------------------------------------------------------------------

    private static string CacheKey(Guid id) => $"account:{id}";
}
