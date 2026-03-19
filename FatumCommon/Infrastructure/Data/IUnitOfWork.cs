using System;
using System.Threading.Tasks;


namespace FatumCommon.Infrastructure.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IFatumCsvRepository FatumCsvRepository { get; }
        IFatumRepository FatumRepository { get; }
        IAnomaliaRepository AnomaliaRepository { get; }
        IGpsPointRepository GpsPointRepository { get; }

        void BeginTransaction();

        void Commit();
    }

}
