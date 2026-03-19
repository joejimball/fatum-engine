using System;



namespace FatumCommon.Infrastructure.Data
{
    public class SqliteDapperUnitOfWork : IUnitOfWork
    {
        private readonly IDapperContext _context;
        private IFatumCsvRepository _fatumCsvRepository;
        private IGpsPointRepository _gpsPointRepository;
        private IFatumRepository _fatumRepository;
        private IAnomaliaRepository _anomaliaRepository;

        private bool disposed = false;
        //private SQLiteTransaction _currentTransaction;

        public SqliteDapperUnitOfWork(IDapperContext _context)
        {
            this._context = _context;
        }

        public IFatumCsvRepository FatumCsvRepository => this._fatumCsvRepository ?? new SqliteFatumCsvRepository(this._context);
        public IGpsPointRepository GpsPointRepository => this._gpsPointRepository ?? new GpsPointRepository(this._context);
        public IFatumRepository FatumRepository => this._fatumRepository ?? new SqliteFatumRepository(this._context);
        public IAnomaliaRepository AnomaliaRepository => this._anomaliaRepository ?? new SqliteAnomaliaRepository(this._context);

        public void BeginTransaction()
        {
            //    if (this._currentTransaction != null)
            //        throw new InvalidOperationException("A transaction has already been started.");
            //    this._currentTransaction = this._context.Connection.BeginTransaction();
        }

        public void Commit()
        {
            //if (this._currentTransaction == null)
            //    throw new InvalidOperationException("A transaction has not been started.");

            //try
            //{
            //    this._currentTransaction.Commit();
            //    this._currentTransaction.Dispose();
            //    this._currentTransaction = null;
            //}
            //catch (Exception)
            //{
            //    if (this._currentTransaction != null)
            //        this._currentTransaction.Rollback();
            //    throw;
            //}
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this._context.Dispose();
                    //this._currentTransaction?.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

}
