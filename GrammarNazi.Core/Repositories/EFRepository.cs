using GrammarNazi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Repositories
{
    public class EFRepository<T> : IRepository<T> where T : class
    {
        private readonly DbContext _dbContext;

        public EFRepository(IServiceScopeFactory serviceScopeFactory)
        {
            _dbContext = serviceScopeFactory.CreateScope().ServiceProvider.GetService<DbContext>();
        }

        public async Task Add(T entity)
        {
            await _dbContext.Set<T>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> Any(Expression<Func<T, bool>> filter = default)
        {
            if (filter == default)
                return await _dbContext.Set<T>().AnyAsync();

            return await _dbContext.Set<T>().AnyAsync(filter);
        }

        public async Task Delete(T entity)
        {
            _dbContext.Set<T>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> filter)
        {
            return await _dbContext.Set<T>().Where(filter).ToListAsync();
        }

        public async Task<T> GetFirst(Expression<Func<T, bool>> filter)
        {
            return await _dbContext.Set<T>().AsNoTracking().FirstOrDefaultAsync(filter);
        }

        public async Task<TResult> Max<TResult>(Expression<Func<T, TResult>> selector)
        {
            return await _dbContext.Set<T>().MaxAsync(selector);
        }

        public async Task Update(T entity, Expression<Func<T, bool>> identifier)
        {
            //TODO: Find way to update without removing
            var obj = await _dbContext.Set<T>().FirstOrDefaultAsync(identifier);
            if (obj != default)
                _dbContext.Set<T>().Remove(obj);

            await _dbContext.Set<T>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}