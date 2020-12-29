using GrammarNazi.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Repositories
{
    public class InMemoryRepository<T> : IRepository<T> where T : class
    {
        private static readonly List<T> _list = new();

        public Task Add(T entity)
        {
            _list.Add(entity);
            return Task.CompletedTask;
        }

        public Task<bool> Any(Expression<Func<T, bool>> filter = default)
        {
            if (filter == default)
                return Task.FromResult(_list.Count > 0);

            return Task.FromResult(_list.Any(filter.Compile()));
        }

        public Task Delete(T entity)
        {
            _list.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> filter)
        {
            return Task.FromResult(_list.Where(filter.Compile()));
        }

        public Task<T> GetFirst(Expression<Func<T, bool>> filter)
        {
            var item = _list.FirstOrDefault(filter.Compile());
            return Task.FromResult(item);
        }

        public Task<TResult> Max<TResult>(Expression<Func<T, TResult>> selector)
        {
            return Task.FromResult(_list.Max(selector.Compile()));
        }

        public Task Update(T entity, Expression<Func<T, bool>> identifier)
        {
            //TODO: Find way to update without removing
            var obj = _list.FirstOrDefault(identifier.Compile());

            if (obj != default)
                _list.Remove(obj);

            _list.Add(entity);

            return Task.CompletedTask;
        }
    }
}