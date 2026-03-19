using System.Threading;
using System.Threading.Tasks;

namespace FatumCommon.Infrastructure.Data;

/// <summary>
/// Crea las tablas fatums y anomalias si no existen.
/// Respeta los nombres y columnas indicados por el usuario.
/// </summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
