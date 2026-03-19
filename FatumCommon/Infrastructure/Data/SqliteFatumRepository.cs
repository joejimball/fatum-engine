using FatumCommon.Domain;

namespace FatumCommon.Infrastructure.Data
{
    public class SqliteFatumRepository : SqliteGenericRepository<Fatum>, IFatumRepository
    {
        public SqliteFatumRepository(IDapperContext _context) : base(_context) { }
    }
}
