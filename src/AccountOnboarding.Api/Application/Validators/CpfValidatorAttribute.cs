using System.ComponentModel.DataAnnotations;
using AccountOnboarding.Api.Domain;

namespace AccountOnboarding.Api.Application.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class CpfValidatorAttribute : ValidationAttribute
{
    public CpfValidatorAttribute() : base("CPF inválido.")
    {
    }

    public override bool IsValid(object? value) => value is null || value is string cpf && CpfNormalizer.TryNormalize(cpf, out _);
}
