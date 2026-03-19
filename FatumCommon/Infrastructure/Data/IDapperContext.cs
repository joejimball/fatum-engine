using Microsoft.Data.Sqlite;
using System;

namespace FatumCommon.Infrastructure.Data
{
    public interface IDapperContext : IDisposable
    {
        ISQLiteConnectionFactory ConnectionFactory  { get; }
        void SaveChanges();
    }
}
