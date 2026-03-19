using FatumCommon.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace FatumApp.Maui.Infrastructure.Data
{
    public sealed class MauiSQLiteConnectionFactory : ISQLiteConnectionFactory
    {
        private readonly string _databasePath;

        public MauiSQLiteConnectionFactory()
        {
            var dir = FileSystem.AppDataDirectory;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            _databasePath = Path.Combine(dir, "fatum.db");
        }

        public string DatabasePath => _databasePath;

        public SqliteConnection CreateConnection()
        {
            var conn = new SqliteConnection($"Data Source={_databasePath}");
            conn.Open();
            return conn;
        }
    }
}
