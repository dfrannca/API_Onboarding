using AccountOnboarding.Api.Domain.Enums;

namespace AccountOnboarding.Api.Application.Responses;

public sealed record AccountAuditResponse(Guid Id, Guid AccountId, AccountOperation Operation, DateTimeOffset OccurredAtUtc);