using AccountOnboarding.Api.Domain.Enums;

namespace AccountOnboarding.Api.Domain.Entities;

public sealed class AccountAuditEvent
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public AccountOperation Operation { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public CustomerAccount Account { get; set; } = null!;
}