namespace FatumApp.Maui.Services;

/// <summary>
/// Permite al usuario elegir una ubicación en el mapa (selección manual).
/// </summary>
public interface IMapPickerService
{
    /// <summary>
    /// Muestra un mapa modal; el usuario toca para elegir ubicación y confirma.
    /// </summary>
    /// <param name="initialLatitude">Latitud inicial del centro del mapa.</param>
    /// <param name="initialLongitude">Longitud inicial.</param>
    /// <param name="radiusKm">Radio en km para dibujar el círculo (opcional).</param>
    /// <returns>Coordenadas elegidas o null si canceló.</returns>
    Task<(double Latitude, double Longitude)?> PickLocationAsync(double initialLatitude, double initialLongitude, double radiusKm = 2);
}
