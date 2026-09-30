using AccountOnboarding.Api.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountOnboarding.Api.Infrastructure.Migrations;

[DbContext(typeof(AccountsDbContext))]
[Migration("20260930120000_AllowCpfReuseAfterSoftDelete")]
public sealed class AllowCpfReuseAfterSoftDelete : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_customer_accounts_Cpf",
            table: "customer_accounts");

        migrationBuilder.CreateIndex(
            name: "IX_customer_accounts_Cpf",
            table: "customer_accounts",
            column: "Cpf",
            unique: true,
            filter: "\"DeletedAtUtc\" IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_customer_accounts_Cpf",
            table: "customer_accounts");

        migrationBuilder.CreateIndex(
            name: "IX_customer_accounts_Cpf",
            table: "customer_accounts",
            column: "Cpf",
            unique: true);
    }
}
