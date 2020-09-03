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
    }
}