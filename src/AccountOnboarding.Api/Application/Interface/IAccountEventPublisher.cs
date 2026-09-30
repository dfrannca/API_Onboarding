using AccountOnboarding.Api.Domain.Entities;

namespace AccountOnboarding.Api.Application.Interface;

/// <summary>Abstraction point for a future broker publisher (SNS/SQS, RabbitMQ, etc.).</summary>
public interface IAccountEventPublisher
{
    Task PublishAsync(AccountIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}