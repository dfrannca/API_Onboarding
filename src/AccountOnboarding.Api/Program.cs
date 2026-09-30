using System.Text.Json.Serialization;
using AccountOnboarding.Api.Application.Events;
using AccountOnboarding.Api.Application.Services;
using AccountOnboarding.Api.Application.Interface;
using AccountOnboarding.Api.Infrastructure;
using AccountOnboarding.Api.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Accounts")
    ?? throw new InvalidOperationException("Connection string 'Accounts' is not configured.");

builder.Services.AddDbContext<AccountsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IUnitOfWorkRepository, UnitOfWorkRepository>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAccountService, AccountService>();
// Mock de integração: registra no log onde um broker real publicaria os eventos.
builder.Services.AddSingleton<IAccountEventPublisher, MockAccountEventPublisher>();
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
