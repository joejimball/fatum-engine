using Microsoft.Data.Sqlite;
using System;



namespace FatumCommon.Infrastructure.Data
{
    public class SqliteDapperContext : IDapperContext
    {
        private readonly ISQLiteConnectionFactory _connectionFactory;

        // Flag: Has Dispose already been called?
        private bool disposed = false;
        

        public SqliteDapperContext(ISQLiteConnectionFactory _connectionFactory)
        {
            this._connectionFactory = _connectionFactory;
        }

        ~SqliteDapperContext()
        {
            Dispose(false);
        }

        public ISQLiteConnectionFactory ConnectionFactory => _connectionFactory;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void SaveChanges()
        {
            
        }


        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }
    }
}
