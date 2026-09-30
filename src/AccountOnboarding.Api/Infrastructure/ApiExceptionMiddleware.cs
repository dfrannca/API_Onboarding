using AccountOnboarding.Api.Application.Exceptions;
using AccountOnboarding.Api.Domain;

namespace AccountOnboarding.Api.Infrastructure;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var status = exception switch
            {
                AccountNotFoundException => StatusCodes.Status404NotFound,
                DuplicateCpfException => StatusCodes.Status409Conflict,
                AccountConcurrencyException => StatusCodes.Status409Conflict,
                InvalidCpfException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            var title = status switch
            {
                StatusCodes.Status400BadRequest => "Bad Request",
                StatusCodes.Status404NotFound => "Not Found",
                StatusCodes.Status409Conflict => "Conflict",
                StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
                _ => "Internal Error"
            };

            if (status == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Erro inesperado ao processar requisição.");
            }

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                status,
                title,
                message = status == StatusCodes.Status500InternalServerError
                    ? "Ocorreu um erro interno."
                    : exception.Message
            }, cancellationToken: context.RequestAborted);
        }
    }
}