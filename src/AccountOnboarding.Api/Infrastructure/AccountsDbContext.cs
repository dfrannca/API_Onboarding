using AccountOnboarding.Api.Domain.Entities;
using AccountOnboarding.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AccountOnboarding.Api.Infrastructure;

public sealed class AccountsDbContext(DbContextOptions<AccountsDbContext> options) : DbContext(options)
{
    public DbSet<CustomerAccount> Accounts => Set<CustomerAccount>();
    public DbSet<AccountAuditEvent> AuditEvents => Set<AccountAuditEvent>();
    public DbSet<AccountIntegrationEvent> IntegrationEvents => Set<AccountIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var utcConverter = new ValueConverter<DateTimeOffset, DateTime>(
            value => value.UtcDateTime,
            value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        var account = modelBuilder.Entity<CustomerAccount>();
        account.ToTable("customer_accounts");
        account.HasKey(item => item.Id);
        account.Property(item => item.HolderName).HasMaxLength(150).IsRequired();
        account.Property(item => item.Cpf).HasMaxLength(11).IsRequired();
        account.HasIndex(item => item.Cpf)
            .IsUnique()
            .HasFilter("\"DeletedAtUtc\" IS NULL");
        account.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        account.Property(item => item.Version).IsConcurrencyToken();
        account.Property(item => item.CreatedAtUtc).HasConversion(utcConverter);
        account.Property(item => item.UpdatedAtUtc).HasConversion(utcConverter);
        account.Property(item => item.DeletedAtUtc).HasConversion(utcConverter);
        account.HasMany(item => item.AuditEvents).WithOne(item => item.Account).HasForeignKey(item => item.AccountId);

        var audit = modelBuilder.Entity<AccountAuditEvent>();
        audit.ToTable("account_audit_events");
        audit.HasKey(item => item.Id);
        audit.Property(item => item.Operation).HasConversion<string>().HasMaxLength(20);
        audit.Property(item => item.OccurredAtUtc).HasConversion(utcConverter);
        audit.HasIndex(item => new { item.AccountId, item.OccurredAtUtc });

        var integrationEvent = modelBuilder.Entity<AccountIntegrationEvent>();
        integrationEvent.ToTable("account_integration_outbox");
        integrationEvent.HasKey(item => item.Id);
        integrationEvent.Property(item => item.Operation).HasConversion<string>().HasMaxLength(20);
        integrationEvent.Property(item => item.OccurredAtUtc).HasConversion(utcConverter);
        integrationEvent.Property(item => item.PublishedAtUtc).HasConversion(new ValueConverter<DateTimeOffset?, DateTime?>(
            value => value.HasValue ? value.Value.UtcDateTime : null,
            value => value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null));
        integrationEvent.HasIndex(item => new { item.PublishedAtUtc, item.OccurredAtUtc });
    }
}
