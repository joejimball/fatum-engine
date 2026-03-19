using Dapper.Contrib.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FatumCommon.Infrastructure.Data
{
    public abstract class SqliteGenericRepository<T> : IGenericRepository<T> where T : class 
    {
        protected readonly IDapperContext Context;

        public SqliteGenericRepository(IDapperContext _context)
        {
            this.Context = _context;
        }
        public async Task<long> AddAsync(T entity)
        {
            await using var conn = this.Context.ConnectionFactory.CreateConnection();
            return await conn.InsertAsync(entity);
        }

        public async Task AddAsync(IEnumerable<T> items)
        {
            await using var conn = this.Context.ConnectionFactory.CreateConnection();
            await conn.InsertAsync(items);
        }

        public async Task<bool> DeleteAsync(T entity)
        {
            await using var conn = this.Context.ConnectionFactory.CreateConnection();
            return await conn.DeleteAsync(entity);
        }

        public async Task<bool> DeleteAllAsync()
        {
            await using var conn = this.Context.ConnectionFactory.CreateConnection();
            return await conn.DeleteAllAsync<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            await using var conn = this.Context.ConnectionFactory.CreateConnection();
            return await conn.GetAllAsync<T>();
        }

        public async Task<T?> GetByIdAsync(long id)
        {
            await using var conn = this.Context.ConnectionFactory.CreateConnection();
            return await conn.GetAsync<T>(id);
        }

        public async Task<bool> UpdateAsync(T entity)
        {
            await using var conn = this.Context.ConnectionFactory.CreateConnection();
            return await conn.UpdateAsync(entity);
        }
    }
}
