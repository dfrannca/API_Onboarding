using System.ComponentModel.DataAnnotations;
using AccountOnboarding.Api.Application.Validators;
using AccountOnboarding.Api.Domain.Enums;

namespace AccountOnboarding.Api.Application.Requests;

public sealed class CreateAccountRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public required string HolderName { get; init; }

    [Required, CpfValidator]
    public required string Cpf { get; init; }

    public AccountStatus Status { get; init; } = AccountStatus.Active;
}