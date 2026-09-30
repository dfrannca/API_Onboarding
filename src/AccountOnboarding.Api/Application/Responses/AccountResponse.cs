namespace AccountOnboarding.Api.Application.Responses;

public sealed record AccountResponse(
    Guid Id,
    string HolderName,
    string Cpf,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? DeletedAtUtc,
    long Version);
