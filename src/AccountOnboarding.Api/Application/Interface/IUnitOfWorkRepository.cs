using System.Linq.Expressions;

namespace AccountOnboarding.Api.Application.Interface;

public interface IUnitOfWorkRepository
{
    Task Create<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
    Task Update<TEntity>(TEntity entity) where TEntity : class;
    IQueryable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class;
    IQueryable<TEntity> GetAll<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
