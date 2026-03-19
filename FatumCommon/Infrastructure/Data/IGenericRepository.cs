using FatumCommon.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FatumCommon.Infrastructure.Data
{
    public interface IGenericRepository<T> where T : class
    {
        Task AddAsync(IEnumerable<T> items);
        Task<long> AddAsync(T entity);
        Task<bool> DeleteAllAsync();
        Task<bool> DeleteAsync(T entity);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(long id);
        Task<bool> UpdateAsync(T entity);
    }

    public interface IFatumRepository : IGenericRepository<Fatum> { }
    public interface IAnomaliaRepository : IGenericRepository<Anomalia> { }
    public interface IFatumCsvRepository : IGenericRepository<FatumCsv> { }
    public interface IGpsPointRepository : IGenericRepository<GpsPoint> { }
}
