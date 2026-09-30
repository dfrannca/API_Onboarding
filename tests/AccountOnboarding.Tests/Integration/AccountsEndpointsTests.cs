using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AccountOnboarding.Api.Application.Responses;
using AccountOnboarding.Api.Domain.Enums;

namespace AccountOnboarding.Tests.Integration;

[Collection(AccountApiCollection.Name)]
public sealed class AccountsEndpointsTests(AccountApiFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task AccountLifecycle_TracksHistoryAndInvalidatesCache()
    {
        var created = await CreateAccountAsync("529.982.247-25", "Ana Silva");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createResponse = await created.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>(JsonOptions);
        var account = createResponse!.Data;
        Assert.NotNull(account);
        Assert.Equal("***.***.***-25", account.Cpf);

        var firstReadResponse = await factory.Client.GetFromJsonAsync<ApiResponse<AccountResponse>>($"/api/v1/accounts/{account.Id}", JsonOptions);
        var firstRead = firstReadResponse!.Data;
        Assert.Equal("Ana Silva", firstRead!.HolderName);

        var update = await factory.Client.PutAsJsonAsync($"/api/v1/accounts/{account.Id}", new
        {
            holderName = "Ana Souza",
            cpf = "52998224725",
            status = AccountStatus.Inactive,
            expectedVersion = account.Version
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var updateResponse = await update.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>(JsonOptions);
        var updated = updateResponse!.Data;
        Assert.Equal(account.Version + 1, updated!.Version);
        var cachedResponse = await factory.Client.GetFromJsonAsync<ApiResponse<AccountResponse>>($"/api/v1/accounts/{account.Id}", JsonOptions);
        var cachedRead = cachedResponse!.Data;
        Assert.Equal("Ana Souza", cachedRead!.HolderName);
        Assert.Equal(nameof(AccountStatus.Inactive), cachedRead.Status);

        var staleUpdate = await factory.Client.PutAsJsonAsync($"/api/v1/accounts/{account.Id}", new
        {
            holderName = "Ana Silva",
            cpf = "52998224725",
            status = AccountStatus.Active,
            expectedVersion = account.Version
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);

        var deletion = await factory.Client.DeleteAsync($"/api/v1/accounts/{account.Id}?expectedVersion={updated.Version}");
        Assert.Equal(HttpStatusCode.OK, deletion.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.Client.GetAsync($"/api/v1/accounts/{account.Id}")).StatusCode);

        var historyResponse = await factory.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AccountAuditResponse>>>($"/api/v1/accounts/{account.Id}/history", JsonOptions);
        var history = historyResponse!.Data;
        Assert.Equal([AccountOperation.Created, AccountOperation.Updated, AccountOperation.Deleted], history!.Select(item => item.Operation));
    }

    [Fact]
    public async Task List_FiltersByCpfAndStatus_AndRejectsDuplicateCpf()
    {
        var created = await CreateAccountAsync("111.444.777-35", "Bruno Lima");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var duplicate = await CreateAccountAsync("11144477735", "Bruno Outro");
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var duplicateBody = await duplicate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Conflict", duplicateBody.GetProperty("title").GetString());
        Assert.Equal(409, duplicateBody.GetProperty("status").GetInt32());
        Assert.Equal("Já existe uma conta cadastrada para este CPF.", duplicateBody.GetProperty("message").GetString());
        Assert.False(duplicateBody.TryGetProperty("detail", out _));
        Assert.False(duplicateBody.TryGetProperty("instance", out _));
        Assert.False(duplicateBody.TryGetProperty("data", out _));

        var filteredResponse = await factory.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AccountResponse>>>(
            "/api/v1/accounts?cpf=11144477735&status=Active", JsonOptions);
        var filtered = filteredResponse!.Data;
        Assert.Single(filtered!);

        var invalidCpf = await factory.Client.GetAsync("/api/v1/accounts?cpf=11111111111");
        Assert.Equal(HttpStatusCode.BadRequest, invalidCpf.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsNotFoundBeforeCpfValidation_WhenAccountIdDoesNotExist()
    {
        var id = Guid.NewGuid();

        var response = await factory.Client.PutAsJsonAsync($"/api/v1/accounts/{id}", new
        {
            holderName = "Ana Silva",
            cpf = "11111111111",
            status = AccountStatus.Active,
            expectedVersion = 1
        }, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal($"A conta com ID '{id}' não existe no banco de dados.", body.GetProperty("message").GetString());
    }

    private Task<HttpResponseMessage> CreateAccountAsync(string cpf, string holderName) =>
        factory.Client.PostAsJsonAsync("/api/v1/accounts", new
        {
            holderName,
            cpf,
            status = AccountStatus.Active
        }, JsonOptions);
}
