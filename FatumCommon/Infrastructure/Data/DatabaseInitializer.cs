using FatumCommon.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FatumApp.Maui.Infrastructure.Data
{

    public sealed class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly ISQLiteConnectionFactory _connectionFactory;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(ISQLiteConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await using var conn = _connectionFactory.CreateConnection();
            const string sqlFatum = @"
CREATE TABLE IF NOT EXISTS fatums (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Latitude REAL,
    Longitude REAL,
    OpenLocationCode TEXT,
    GeoHash TEXT,
    What3Words TEXT,
    GridSize INTEGER,
    Bandwidth REAL,
    Radio REAL,
    NumPuntos INTEGER,
    TypeRnd INTEGER,
    CreatedAt TEXT,
    RandomData BLOB
);";
            const string sqlAnomalias = @"
CREATE TABLE IF NOT EXISTS anomalias (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Latitude REAL,
    Longitude REAL,
    OpenLocationCode TEXT,
    GeoHash TEXT,
    What3Words TEXT,
    DensidadEstimacion REAL,
    Power REAL,
    ZScore REAL,
    RadioAproximado REAL,
    TipoAnomalia INTEGER,
    IdFatum INTEGER,
    Distancia REAL,
    QuantumPotential REAL
);";
            var cmdFatum = conn.CreateCommand();
            cmdFatum.CommandText = sqlFatum;
            await cmdFatum.ExecuteNonQueryAsync(cancellationToken);
            var cmdAnomalias = conn.CreateCommand();
            cmdAnomalias.CommandText = sqlAnomalias;
            await cmdAnomalias.ExecuteNonQueryAsync(cancellationToken);

            // Migración simple para instalaciones existentes:
            // añadimos columnas faltantes sin destruir los datos actuales.
            await EnsureColumnAsync(conn, "fatums", "OpenLocationCode", "TEXT", cancellationToken);
            await EnsureColumnAsync(conn, "fatums", "GeoHash", "TEXT", cancellationToken);
            await EnsureColumnAsync(conn, "fatums", "What3Words", "TEXT", cancellationToken);
            await EnsureColumnAsync(conn, "fatums", "GridSize", "INTEGER", cancellationToken);
            await EnsureColumnAsync(conn, "fatums", "Bandwidth", "REAL", cancellationToken);

            await EnsureColumnAsync(conn, "anomalias", "OpenLocationCode", "TEXT", cancellationToken);
            await EnsureColumnAsync(conn, "anomalias", "GeoHash", "TEXT", cancellationToken);
            await EnsureColumnAsync(conn, "anomalias", "What3Words", "TEXT", cancellationToken);
            await EnsureColumnAsync(conn, "anomalias", "QZScore", "REAL", cancellationToken);
            await EnsureColumnAsync(conn, "anomalias", "ZScoreFinal", "REAL", cancellationToken);
            _logger.LogInformation("Base de datos inicializada en {Path}", _connectionFactory.DatabasePath);
        }

        private static async Task EnsureColumnAsync(
            SqliteConnection conn,
            string tableName,
            string columnName,
            string columnType,
            CancellationToken cancellationToken)
        {
            using var pragmaCmd = conn.CreateCommand();
            pragmaCmd.CommandText = $"PRAGMA table_info({tableName});";

            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var reader = await pragmaCmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    // PRAGMA table_info returns: cid, name, type, notnull, dflt_value, pk
                    var name = reader["name"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(name))
                        existingColumns.Add(name);
                }
            }

            if (existingColumns.Contains(columnName))
                return;

            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType};";
            await alterCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

}
