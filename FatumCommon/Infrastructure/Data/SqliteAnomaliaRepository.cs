using FatumCommon.Domain;

namespace FatumCommon.Infrastructure.Data
{
    public class SqliteAnomaliaRepository : SqliteGenericRepository<Anomalia>, IAnomaliaRepository
    {
        public SqliteAnomaliaRepository(IDapperContext _context) : base(_context) { }
    }
}
