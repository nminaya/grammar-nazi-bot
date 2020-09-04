using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task Add(T entity);

        Task Delete(T entity);

        Task<T> GetFirst(Expression<Func<T, bool>> filter);

        Task<bool> Any(Expression<Func<T, bool>> filter = default);

        Task<TResult> Max<TResult>(Expression<Func<T, TResult>> selector);
    }
}
