using AccountOnboarding.Api.Application.Exceptions;
using AccountOnboarding.Api.Domain;

namespace AccountOnboarding.Api.Application.Results;

public sealed record ResultError(int Status, string Title, string Message)
{
    public static ResultError FromException(Exception exception) => exception switch
    {
        AccountNotFoundException => new(404, "Not Found", exception.Message),
        DuplicateCpfException => new(409, "Conflict", exception.Message),
        AccountConcurrencyException => new(409, "Conflict", exception.Message),
        InvalidCpfException => new(400, "Bad Request", exception.Message),
        _ => new(500, "Internal Error", "Ocorreu um erro interno.")
    };
}

public sealed class Result<T>
{
    private Result(bool isSuccess, T? view, ResultError? error, string? message = null)
    {
        IsSuccess = isSuccess;
        View = view;
        Error = error;
        Message = message;
    }

    public bool IsSuccess { get; }
    public T? View { get; }
    public ResultError? Error { get; }
    public string? Message { get; }

    public static Result<T> Success(T view, string? message = null) => new(true, view, null, message);

    public static Result<T> Failure(Exception exception) =>
        new(false, default, ResultError.FromException(exception));

}
