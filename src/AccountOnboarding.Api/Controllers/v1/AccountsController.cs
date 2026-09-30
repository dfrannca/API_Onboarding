using AccountOnboarding.Api.Application.Exceptions;
using AccountOnboarding.Api.Application.Interface;
using AccountOnboarding.Api.Application.Requests;
using AccountOnboarding.Api.Application.Validators;
using AccountOnboarding.Api.Controllers.Base;
using AccountOnboarding.Api.Domain;
using AccountOnboarding.Api.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AccountOnboarding.Api.Controllers.v1;

[ApiController]
[Route("api/v1/accounts")]
public sealed class AccountsController(
    IAccountService accountService,
    ILogger<AccountsController> logger)
    : BaseController(logger)
{
    [HttpPost]
    public async Task<IResult> Create(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var account = await accountService.CreateAsync(request, cancellationToken);
            return Created($"/api/v1/accounts/{account.Id}", account, "Conta criada com sucesso.");
        }
        catch (Exception ex) when (!IsDomainException(ex))
        {
            return Results.Problem(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var account = await accountService.GetByIdAsync(id, cancellationToken);
            return Success(account, "Conta consultada com sucesso.");
        }
        catch (Exception ex) when (!IsDomainException(ex))
        {
            return Results.Problem(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IResult> List(
        [FromQuery, CpfValidator] string? cpf,
        [FromQuery] AccountStatus? status,
        CancellationToken cancellationToken)
    {
        try
        {
            var accounts = await accountService.ListAsync(cpf, status, cancellationToken);
            return Success(accounts, "Contas consultadas com sucesso.");
        }
        catch (Exception ex) when (!IsDomainException(ex))
        {
            return Results.Problem(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IResult> Update(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var account = await accountService.UpdateAsync(id, request, cancellationToken);
            return Success(account, "Conta atualizada com sucesso.");
        }
        catch (AccountNotFoundException ex)
        {
            return Results.Json(
                new { status = StatusCodes.Status404NotFound, title = "Not Found", message = ex.Message },
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (Exception ex) when (!IsDomainException(ex))
        {
            return Results.Problem(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IResult> Delete(Guid id, [FromQuery, Range(1, long.MaxValue)] long expectedVersion, CancellationToken cancellationToken)
    {
        try
        {
            await accountService.DeleteAsync(id, expectedVersion, cancellationToken);
            return Result("Conta removida com sucesso.");
        }
        catch (Exception ex) when (!IsDomainException(ex))
        {
            return Results.Problem(ex.Message);
        }
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IResult> History(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var history = await accountService.GetHistoryAsync(id, cancellationToken);
            return Success(history, "Histórico consultado com sucesso.");
        }
        catch (Exception ex) when (!IsDomainException(ex))
        {
            return Results.Problem(ex.Message);
        }
    }

    private static bool IsDomainException(Exception exception) => exception is
        AccountNotFoundException or
        DuplicateCpfException or
        InvalidCpfException or
        AccountConcurrencyException;
}
