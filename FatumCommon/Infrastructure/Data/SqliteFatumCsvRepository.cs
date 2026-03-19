using FatumCommon.Domain;

namespace FatumCommon.Infrastructure.Data
{
    public class SqliteFatumCsvRepository : SqliteGenericRepository<FatumCsv>, IFatumCsvRepository
    {
        public SqliteFatumCsvRepository(IDapperContext _context) : base(_context) { }

    }

    public class GpsPointRepository : SqliteGenericRepository<GpsPoint>, IGpsPointRepository
    {
        public GpsPointRepository(IDapperContext _context) : base(_context) { }
        
    }
}
