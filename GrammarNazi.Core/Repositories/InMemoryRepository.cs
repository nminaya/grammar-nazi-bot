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
        private static readonly List<T> _list = new List<T>();

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

        public Task<T> GetFirst(Expression<Func<T, bool>> filter)
        {
            var item = _list.FirstOrDefault(filter.Compile());
            return Task.FromResult(item);
        }

        public Task<TResult> Max<TResult>(Expression<Func<T, TResult>> selector)
        {
            return Task.FromResult(_list.Max(selector.Compile()));
        }
    }
}