using Microsoft.AspNetCore.Http;

namespace AccountOnboarding.Api.Application.Responses;

public sealed record ApiResponse<T>(
	int Status,
	string Title,
	string Message,
	T? Data);
