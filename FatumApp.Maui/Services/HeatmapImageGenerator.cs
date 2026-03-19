using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FatumApp.Maui.Services;

/// <summary>
/// Genera una imagen PNG del mapa de calor KDE usando la misma lógica que el visor WPF:
/// escala azul → verde → amarillo → rojo (0.33, 0.66).
/// </summary>
public static class HeatmapImageGenerator
{
    /// <summary>
    /// Genera PNG del heatmap y devuelve los límites geográficos del grid.
    /// </summary>
    /// <param name="densityGrid">Grid (lat, lon, density) en orden row-major; tamaño GridSize*GridSize.</param>
    /// <returns>(pngBytes, minLat, maxLat, minLon, maxLon)</returns>
    public static (byte[] PngBytes, double MinLat, double MaxLat, double MinLon, double MaxLon) Generate(
        (double lat, double lon, double density)[] densityGrid, int gridSize = 100)
    {
        if (densityGrid == null || densityGrid.Length != gridSize * gridSize)
            throw new ArgumentException("densityGrid debe tener tamaño GridSize*GridSize.", nameof(densityGrid));

        var minDensity = densityGrid.Min(p => p.density);
        var maxDensity = densityGrid.Max(p => p.density);
        var minLat = densityGrid.Min(p => p.lat);
        var maxLat = densityGrid.Max(p => p.lat);
        var minLon = densityGrid.Min(p => p.lon);
        var maxLon = densityGrid.Max(p => p.lon);

        var pixels = new Rgba32[gridSize * gridSize];
        for (int i = 0; i < densityGrid.Length; i++)
        {
            int x = i % gridSize;
            int y = i / gridSize;
            var (_, _, density) = densityGrid[i];
            var (r, g, b) = GetColorForDensity(density, minDensity, maxDensity);
            pixels[y * gridSize + x] = new Rgba32(r, g, b, 255);
        }

        // Leaflet: imagen con fila 0 = norte (maxLat). Nuestro grid tiene fila 0 = minLat (sur), así que volteamos verticalmente.
        var flipped = new Rgba32[gridSize * gridSize];
        for (int row = 0; row < gridSize; row++)
        {
            int srcRow = gridSize - 1 - row;
            for (int col = 0; col < gridSize; col++)
                flipped[row * gridSize + col] = pixels[srcRow * gridSize + col];
        }

        using var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(flipped, gridSize, gridSize);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return (ms.ToArray(), minLat, maxLat, minLon, maxLon);
    }

    /// <summary>
    /// Misma escala que el visor WPF: azul → verde → amarillo → rojo (0.33, 0.66).
    /// </summary>
    private static (byte R, byte G, byte B) GetColorForDensity(double value, double min, double max)
    {
        double range = max - min;
        if (range < 1e-12) range = 1.0;
        double t = (value - min) / range;
        t = Math.Clamp(t, 0.0, 1.0);

        if (t < 0.33)
        {
            double ratio = t / 0.33;
            return (
                (byte)(0 * (1 - ratio) + 0 * ratio),
                (byte)(0 * (1 - ratio) + 255 * ratio),
                (byte)(255 * (1 - ratio) + 0 * ratio));
        }
        if (t < 0.66)
        {
            double ratio = (t - 0.33) / 0.33;
            return (
                (byte)(0 * (1 - ratio) + 255 * ratio),
                255,
                0);
        }
        double ratioR = (t - 0.66) / 0.34;
        return (
            255,
            (byte)(255 * (1 - ratioR) + 0 * ratioR),
            0);
    }
}
