using AccountOnboarding.Api.Application.Results;
using Microsoft.AspNetCore.Http;

namespace AccountOnboarding.Api.Application.Responses;

public static class ApiResponseExtensions
{
    public static IResult ToSuccess<T>(this Result<T> result, string message) =>
        Microsoft.AspNetCore.Http.Results.Ok(new ApiResponse<T>(200, "Success", message, result.View));
}
