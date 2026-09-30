using System.Linq.Expressions;
using AccountOnboarding.Api.Application.Exceptions;
using AccountOnboarding.Api.Application.Interface;
using AccountOnboarding.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AccountOnboarding.Api.Infrastructure.Repositories;

public sealed class UnitOfWorkRepository(AccountsDbContext dbContext) : IUnitOfWorkRepository
{
    private DbSet<TEntity> BuildDataSet<TEntity>() where TEntity : class =>
        dbContext.Set<TEntity>();

    public Task Create<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
    {
        return BuildDataSet<TEntity>().AddAsync(entity, cancellationToken).AsTask();
    }

    public Task Update<TEntity>(TEntity entity) where TEntity : class
    {
        dbContext.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public IQueryable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class =>
        BuildDataSet<TEntity>().Where(predicate);

    public IQueryable<TEntity> GetAll<TEntity>() where TEntity : class =>
        BuildDataSet<TEntity>();

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AccountConcurrencyException();
        }
        catch (DbUpdateException exception) when (IsPostgresUniqueViolation(exception))
        {
            throw new DuplicateCpfException();
        }
    }

    private static bool IsPostgresUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}
