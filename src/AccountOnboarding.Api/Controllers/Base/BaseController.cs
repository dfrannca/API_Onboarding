using AccountOnboarding.Api.Application.Responses;
using AccountOnboarding.Api.Application.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AccountOnboarding.Api.Controllers.Base;

public abstract class BaseController : ControllerBase
{
    internal readonly ILogger _logger;
    private readonly string? _scopeDisplayName;

    protected BaseController(ILogger logger, string? scopeDisplayName = null)
    {
        _logger = logger;
        _scopeDisplayName = scopeDisplayName;
    }

    [NonAction]
    protected IResult Result<T>(Result<T> result, string message)
    {
        if (!result.IsSuccess)
            return Result(result);

        return Results.Ok(new ApiResponse<T>(200, "Success", message, result.View));
    }

    [NonAction]
    protected IResult Created<T>(string location, Result<T> result, string message)
    {
        if (!result.IsSuccess)
            return Result(result);

        return Results.Created(location, new ApiResponse<T>(201, "Success", message, result.View));
    }

    [NonAction]
    protected IResult Result<T>(Result<T> result)
    {
        var error = result.Error!;
        return Results.Json(
            new { status = error.Status, title = error.Title, message = error.Message },
            statusCode: error.Status);
    }

    [NonAction]
    protected IResult Result(string message) =>
        Results.Ok(new ApiResponse<object>(200, "Success", message, null));

    [NonAction]
    protected IResult Success<T>(T data, string message) =>
        Results.Ok(new ApiResponse<T>(200, "Success", message, data));

    [NonAction]
    protected IResult Created<T>(string location, T data, string message) =>
        Results.Created(location, new ApiResponse<T>(201, "Success", message, data));
}