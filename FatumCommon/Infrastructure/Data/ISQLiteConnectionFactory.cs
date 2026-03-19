using Microsoft.Data.Sqlite;

namespace FatumCommon.Infrastructure.Data;

/// <summary>
/// Crea conexiones SQLite para la base de datos de la aplicación.
/// La base de datos se almacena en el directorio de datos de la app.
/// </summary>
public interface ISQLiteConnectionFactory
{
    /// <summary>
    /// Ruta absoluta del archivo de base de datos.
    /// </summary>
    string DatabasePath { get; }

    /// <summary>
    /// Crea una nueva conexión abierta. El llamador debe cerrarla y disponerla.
    /// </summary>
    SqliteConnection CreateConnection();
}


