using AccountOnboarding.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AccountOnboarding.Tests.Integration;

public sealed class AccountApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        Client = CreateClient();
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        Client.Dispose();
        await connection.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AccountsDbContext>>();
            services.RemoveAll<AccountsDbContext>();
            services.AddDbContext<AccountsDbContext>(options => options.UseSqlite(connection));
        });
    }
}
