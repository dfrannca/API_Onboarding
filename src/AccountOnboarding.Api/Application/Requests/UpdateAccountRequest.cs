using System.ComponentModel.DataAnnotations;
using AccountOnboarding.Api.Domain.Enums;

namespace AccountOnboarding.Api.Application.Requests;

public sealed class UpdateAccountRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public required string HolderName { get; init; }

    [Required]
    public required string Cpf { get; init; }

    public AccountStatus Status { get; init; }

    [Range(1, long.MaxValue)]
    public long ExpectedVersion { get; init; }
}
