using AccountOnboarding.Api.Domain.Enums;

namespace AccountOnboarding.Api.Domain.Entities;

public sealed class CustomerAccount
{
    public Guid Id { get; set; }
    public required string HolderName { get; set; }
    public required string Cpf { get; set; }
    public AccountStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public long Version { get; set; }
    public List<AccountAuditEvent> AuditEvents { get; set; } = [];
}