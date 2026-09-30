using AccountOnboarding.Api.Domain.Enums;

namespace AccountOnboarding.Api.Domain.Entities;

public sealed class AccountIntegrationEvent
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public AccountOperation Operation { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public int Attempts { get; set; }
}