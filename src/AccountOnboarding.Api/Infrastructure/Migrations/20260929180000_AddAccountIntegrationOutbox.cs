using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AccountOnboarding.Api.Infrastructure;

#nullable disable

namespace AccountOnboarding.Api.Infrastructure.Migrations;

[DbContext(typeof(AccountsDbContext))]
[Migration("20260929180000_AddAccountIntegrationOutbox")]
public sealed class AddAccountIntegrationOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "account_integration_outbox",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Attempts = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_account_integration_outbox", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_account_integration_outbox_PublishedAtUtc_OccurredAtUtc",
            table: "account_integration_outbox",
            columns: new[] { "PublishedAtUtc", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "account_integration_outbox");
}
