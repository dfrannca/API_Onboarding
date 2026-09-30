using AccountOnboarding.Api.Application.Interface;
using AccountOnboarding.Api.Domain.Entities;

namespace AccountOnboarding.Api.Application.Events;

/// <summary>Simulates delivery by writing an event-shaped message to the application log.</summary>
public sealed class MockAccountEventPublisher(ILogger<MockAccountEventPublisher> logger) : IAccountEventPublisher
{
    public Task PublishAsync(AccountIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[MOCK EVENT PUBLISHED] EventId={EventId} EventType=Account{Operation} AccountId={AccountId} OccurredAtUtc={OccurredAtUtc}. Replace MockAccountEventPublisher with a broker publisher.",
            integrationEvent.Id,
            integrationEvent.Operation,
            integrationEvent.AccountId,
            integrationEvent.OccurredAtUtc);

        return Task.CompletedTask;
    }
}