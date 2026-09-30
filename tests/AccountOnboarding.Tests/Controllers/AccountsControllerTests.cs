using AccountOnboarding.Api.Application.Interface;
using AccountOnboarding.Api.Application.Requests;
using AccountOnboarding.Api.Application.Responses;
using AccountOnboarding.Api.Controllers.v1;
using AccountOnboarding.Api.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AccountOnboarding.Tests.Controllers;

public sealed class AccountsControllerTests
{
    private readonly AccountsController controller;
    private readonly Mock<IAccountService> _mockAccountService;

    public AccountsControllerTests()
    {
        _mockAccountService = new Mock<IAccountService>();
        controller = new AccountsController(
            _mockAccountService.Object,
            NullLogger<AccountsController>.Instance);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedResult_WhenSuccess()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            HolderName = "Ana Silva",
            Cpf = "52998224725",
            Status = AccountStatus.Active
        };

        var response = new AccountResponse(
            Guid.NewGuid(),
            "Ana Silva",
            "***.***.***-25",
            nameof(AccountStatus.Active),
            DateTimeOffset.UtcNow,
            null,
            null,
            1);

        _mockAccountService
            .Setup(service => service.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);

        var valueProperty = result.GetType().GetProperty("Value");
        Assert.NotNull(valueProperty);

        var value = valueProperty.GetValue(result);
        Assert.NotNull(value);

        var messageProperty = value.GetType().GetProperty("Message");
        Assert.NotNull(messageProperty);

        var message = messageProperty.GetValue(value)?.ToString();
        Assert.Equal("Conta criada com sucesso.", message);
    }

    [Fact]
    public async Task GetById_ShouldReturnOkResult_WhenSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var response = new AccountResponse(
            id,
            "Ana Silva",
            "***.***.***-25",
            nameof(AccountStatus.Active),
            DateTimeOffset.UtcNow,
            null,
            null,
            1);

        _mockAccountService
            .Setup(service => service.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await controller.GetById(id, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);

        var valueProperty = result.GetType().GetProperty("Value");
        Assert.NotNull(valueProperty);

        var value = valueProperty.GetValue(result);
        Assert.NotNull(value);

        var messageProperty = value.GetType().GetProperty("Message");
        Assert.NotNull(messageProperty);

        var message = messageProperty.GetValue(value)?.ToString();
        Assert.Equal("Conta consultada com sucesso.", message);
    }

    [Fact]
    public async Task List_ShouldReturnOkResult_WhenSuccess()
    {
        // Arrange
        var accounts = new[]
        {
            new AccountResponse(
                Guid.NewGuid(),
                "Ana Silva",
                "***.***.***-25",
                nameof(AccountStatus.Active),
                DateTimeOffset.UtcNow,
                null,
                null,
                1)
        };

        _mockAccountService
            .Setup(service => service.ListAsync("52998224725", AccountStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        // Act
        var result = await controller.List("52998224725", AccountStatus.Active, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);

        var valueProperty = result.GetType().GetProperty("Value");
        Assert.NotNull(valueProperty);

        var value = valueProperty.GetValue(result);
        Assert.NotNull(value);

        var messageProperty = value.GetType().GetProperty("Message");
        Assert.NotNull(messageProperty);

        var message = messageProperty.GetValue(value)?.ToString();
        Assert.Equal("Contas consultadas com sucesso.", message);
    }

    [Fact]
    public async Task Update_ShouldReturnOkResult_WhenSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateAccountRequest
        {
            HolderName = "Ana Souza",
            Cpf = "52998224725",
            Status = AccountStatus.Inactive,
            ExpectedVersion = 1
        };

        var response = new AccountResponse(
            id,
            "Ana Souza",
            "***.***.***-25",
            nameof(AccountStatus.Inactive),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            2);

        _mockAccountService
            .Setup(service => service.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await controller.Update(id, request, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);

        var valueProperty = result.GetType().GetProperty("Value");
        Assert.NotNull(valueProperty);

        var value = valueProperty.GetValue(result);
        Assert.NotNull(value);

        var messageProperty = value.GetType().GetProperty("Message");
        Assert.NotNull(messageProperty);

        var message = messageProperty.GetValue(value)?.ToString();
        Assert.Equal("Conta atualizada com sucesso.", message);
    }

    [Fact]
    public async Task Delete_ShouldReturnOkResult_WhenSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mockAccountService
            .Setup(service => service.DeleteAsync(id, 1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await controller.Delete(id, 1, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);

        var valueProperty = result.GetType().GetProperty("Value");
        Assert.NotNull(valueProperty);

        var value = valueProperty.GetValue(result);
        Assert.NotNull(value);

        var messageProperty = value.GetType().GetProperty("Message");
        Assert.NotNull(messageProperty);

        var message = messageProperty.GetValue(value)?.ToString();
        Assert.Equal("Conta removida com sucesso.", message);
    }

    [Fact]
    public async Task History_ShouldReturnOkResult_WhenSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var history = new[]
        {
            new AccountAuditResponse(
                Guid.NewGuid(),
                id,
                AccountOperation.Created,
                DateTimeOffset.UtcNow)
        };

        _mockAccountService
            .Setup(service => service.GetHistoryAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await controller.History(id, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);

        var valueProperty = result.GetType().GetProperty("Value");
        Assert.NotNull(valueProperty);

        var value = valueProperty.GetValue(result);
        Assert.NotNull(value);

        var messageProperty = value.GetType().GetProperty("Message");
        Assert.NotNull(messageProperty);

        var message = messageProperty.GetValue(value)?.ToString();
        Assert.Equal("Histórico consultado com sucesso.", message);
    }
}
