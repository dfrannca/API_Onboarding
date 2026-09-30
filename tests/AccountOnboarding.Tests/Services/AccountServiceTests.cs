using AccountOnboarding.Api.Application.Exceptions;
using AccountOnboarding.Api.Application.Interface;
using AccountOnboarding.Api.Application.Requests;
using AccountOnboarding.Api.Application.Services;
using AccountOnboarding.Api.Domain;
using AccountOnboarding.Api.Domain.Entities;
using AccountOnboarding.Api.Domain.Enums;
using AccountOnboarding.Api.Infrastructure;
using AccountOnboarding.Api.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace AccountOnboarding.Tests.Services;

public sealed class AccountServiceTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AccountsDbContext dbContext;
    private readonly IMemoryCache cache;
    private readonly Mock<IAccountEventPublisher> eventPublisher;
    private readonly AccountService service;

    public AccountServiceTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlite(connection)
            .Options;

        dbContext = new AccountsDbContext(options);
        dbContext.Database.EnsureCreated();

        cache = new MemoryCache(new MemoryCacheOptions());
        eventPublisher = new Mock<IAccountEventPublisher>();
        eventPublisher
            .Setup(publisher => publisher.PublishAsync(It.IsAny<AccountIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        service = new AccountService(new UnitOfWorkRepository(dbContext), cache, eventPublisher.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistAccount_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            HolderName = "Ana Silva",
            Cpf = "52998224725",
            Status = AccountStatus.Active
        };

        // Act
        var result = await service.CreateAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Ana Silva", result.HolderName);
        Assert.Equal("***.***.***-25", result.Cpf);
        Assert.Equal(nameof(AccountStatus.Active), result.Status);
        Assert.Equal(1, result.Version);
        Assert.Equal(1, await dbContext.Accounts.CountAsync());

        eventPublisher.Verify(
            publisher => publisher.PublishAsync(
                It.Is<AccountIntegrationEvent>(item => item.AccountId == result.Id && item.Operation == AccountOperation.Created),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowDuplicateCpfException_WhenCpfAlreadyExists()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            HolderName = "Ana Silva",
            Cpf = "52998224725",
            Status = AccountStatus.Active
        };

        await service.CreateAsync(request, CancellationToken.None);

        var duplicateRequest = new CreateAccountRequest
        {
            HolderName = "Ana Souza",
            Cpf = "52998224725",
            Status = AccountStatus.Active
        };

        // Act
        var exception = await Assert.ThrowsAsync<DuplicateCpfException>(() =>
            service.CreateAsync(duplicateRequest, CancellationToken.None));

        // Assert
        Assert.Equal("Já existe uma conta cadastrada para este CPF.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_ShouldAllowCpfReuse_WhenPreviousAccountWasSoftDeleted()
    {
        var cpf = "52998224725";
        var firstAccount = await service.CreateAsync(new CreateAccountRequest
        {
            HolderName = "Ana Silva",
            Cpf = cpf,
            Status = AccountStatus.Active
        }, CancellationToken.None);

        await service.DeleteAsync(firstAccount.Id, firstAccount.Version, CancellationToken.None);

        var replacement = await service.CreateAsync(new CreateAccountRequest
        {
            HolderName = "Ana Souza",
            Cpf = cpf,
            Status = AccountStatus.Active
        }, CancellationToken.None);

        Assert.NotEqual(firstAccount.Id, replacement.Id);
        Assert.Equal(2, await dbContext.Accounts.CountAsync());
        Assert.Single(await dbContext.Accounts.Where(account => account.Cpf == cpf && account.DeletedAtUtc == null).ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_ShouldKeepCpfUniqueConstraint_WhenDuplicateInsertReachesDatabase()
    {
        // Arrange
        var firstAccount = new CustomerAccount
        {
            Id = Guid.NewGuid(),
            HolderName = "Ana Silva",
            Cpf = "52998224725",
            Status = AccountStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Version = 1
        };

        dbContext.Accounts.Add(firstAccount);
        await dbContext.SaveChangesAsync();

        dbContext.Accounts.Add(new CustomerAccount
        {
            Id = Guid.NewGuid(),
            HolderName = "Ana Souza",
            Cpf = "52998224725",
            Status = AccountStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Version = 1
        });

        // Act
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            dbContext.SaveChangesAsync());

        // Assert
        Assert.Contains("UNIQUE", exception.InnerException?.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowAccountConcurrencyException_WhenExpectedVersionIsStale()
    {
        // Arrange
        var account = new CustomerAccount
        {
            Id = Guid.NewGuid(),
            HolderName = "Ana Silva",
            Cpf = "52998224725",
            Status = AccountStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Version = 1
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        var request = new UpdateAccountRequest
        {
            HolderName = "Ana Souza",
            Cpf = "52998224725",
            Status = AccountStatus.Inactive,
            ExpectedVersion = 99
        };

        // Act
        var exception = await Assert.ThrowsAsync<AccountConcurrencyException>(() =>
            service.UpdateAsync(account.Id, request, CancellationToken.None));

        // Assert
        Assert.Equal("A conta foi alterada por outra operação. Consulte a versão atual e tente novamente.", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteAccount_WhenVersionMatches()
    {
        // Arrange
        var account = new CustomerAccount
        {
            Id = Guid.NewGuid(),
            HolderName = "Ana Silva",
            Cpf = "52998224725",
            Status = AccountStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Version = 1
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        // Act
        await service.DeleteAsync(account.Id, 1, CancellationToken.None);

        // Assert
        var deleted = await dbContext.Accounts.FirstAsync(item => item.Id == account.Id);
        Assert.NotNull(deleted.DeletedAtUtc);
        Assert.Equal(2, deleted.Version);
        Assert.Contains(dbContext.AuditEvents, item => item.AccountId == account.Id && item.Operation == AccountOperation.Deleted);

        eventPublisher.Verify(
            publisher => publisher.PublishAsync(
                It.Is<AccountIntegrationEvent>(item => item.AccountId == account.Id && item.Operation == AccountOperation.Deleted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnAuditEvents_WhenAccountExists()
    {
        // Arrange
        var account = new CustomerAccount
        {
            Id = Guid.NewGuid(),
            HolderName = "Ana Silva",
            Cpf = "52998224725",
            Status = AccountStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Version = 1
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        dbContext.AuditEvents.Add(new AccountAuditEvent
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Operation = AccountOperation.Created,
            OccurredAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();

        // Act
        var history = await service.GetHistoryAsync(account.Id, CancellationToken.None);

        // Assert
        Assert.Single(history);
        Assert.Equal(AccountOperation.Created, history[0].Operation);
    }

    public void Dispose()
    {
        dbContext.Dispose();
        connection.Dispose();
        cache.Dispose();
    }
}
