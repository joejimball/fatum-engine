using FatumCommon;
using FatumCommon.Domain;
using FatumCommon.Enums;
using FatumCommon.Helps;
using FatumCommon.Infrastructure.Data;
using FatumCommon.Services;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;


namespace FatumApp.Maui.Services;

/// <summary>
/// Genera un Fatum y su anomalía usando IKdeCalculator (FatumCommon).
/// Serializa los puntos aleatorios en RandomData (JSON → UTF8 → byte[]).
/// </summary>
public interface IFatumGeneratorService
{
    /// <summary>
    /// Genera puntos aleatorios, ejecuta KDE y búsqueda de anomalía, guarda Fatum y Anomalía.
    /// </summary>
    /// <param name="latitude">Latitud del centro (GPS o selección manual).</param>
    /// <param name="longitude">Longitud del centro.</param>
    /// <param name="radiusMeters">Radio en metros (ej. 1000 = 1 km).</param>
    /// <param name="anomalyType">Atractor, Vacío o Anomalía (máximo entre ambos).</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>La anomalía encontrada o null si no hubo resultado.</returns>
    Task<Anomalia?> GenerateAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        EnumAnomalyType anomalyType,
        CancellationToken cancellationToken = default);
}

public sealed class FatumGeneratorService : IFatumGeneratorService
{
    private readonly IKdeCalculator _kdeCalculator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FatumGeneratorService> _logger;
    private readonly IQuantumSettingsService _quantumSettingsService;
    private readonly IRandomBytesProvider _randomProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly Random Rnd = new();

    public FatumGeneratorService(
        IKdeCalculator kdeCalculator,
        IUnitOfWork unitOfWork,
        ILogger<FatumGeneratorService> logger,
        IQuantumSettingsService quantumSettingsService,
        IRandomBytesProvider randomProvider,
        IHttpClientFactory httpClientFactory)
    {
        _kdeCalculator = kdeCalculator;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _quantumSettingsService = quantumSettingsService;
        _randomProvider = randomProvider;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Anomalia?> GenerateAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        EnumAnomalyType anomalyType,
        CancellationToken cancellationToken = default)
    {
        var typeRnd = await _quantumSettingsService.GetTypeRnd();
        int numPuntos = CalcularPuntosOptimos(radiusMeters);

        string? apiKey = await _quantumSettingsService.GetApiKeyAsync();
        string? what3WordsApiKey = await _quantumSettingsService.GetWhat3WordsApiKeyAsync();
        byte[]? randomData = null;
        if (typeRnd == EnumTypeRnd.QRNG)
        {
            randomData = await this._randomProvider.GetRandomBytesAsync(numPuntos * 4, apiKey, cancellationToken);
        }
        else
        {
            randomData = new byte[numPuntos * 4];
            RandomHelps.RndLocal.NextBytes(randomData);
        }

        var fatum = new Fatum
        {
            Latitude = latitude,
            Longitude = longitude,
            OpenLocationCode = LocationCodesHelps.GetOpenLocationCode(latitude, longitude),
            GeoHash = LocationCodesHelps.GetGeoHash(latitude, longitude),
            Radio = radiusMeters,
            NumPuntos = numPuntos,
            TypeRnd = typeRnd,
            CreatedAt = DateTime.UtcNow,
            RandomData = randomData
        };

        if (!string.IsNullOrWhiteSpace(what3WordsApiKey))
        {
            fatum.What3Words = await ConvertToWhat3WordsAsync(latitude, longitude, what3WordsApiKey!, cancellationToken)
                .ConfigureAwait(false);
        }

        List<(double lat, double lon)> coordList = FatumHelps.GetRandomPoints(fatum);
        if (coordList == null || coordList.Count == 0)
        {
            _logger.LogWarning("No se generaron puntos aleatorios para el Fatum.");
            return null;
        }

        _logger.LogInformation("KDE con {Count} puntos, radio {Radius}m, tipo {Type}", coordList.Count, radiusMeters, anomalyType);
        int gridSize = _kdeCalculator.GetGridSize(radiusMeters);
        double bandwidth = _kdeCalculator.CalculateSilvermanBandwidth(coordList.Count);
        fatum.GridSize = gridSize;
        fatum.Bandwidth = bandwidth;
        (double lat, double lon, double density)[] densityGrid = _kdeCalculator.CalculateKdeOptimized(coordList, bandwidth, gridSize: gridSize);
        Anomalia? result;

        switch (anomalyType)
        {
            case EnumAnomalyType.Attractor:
                result = _kdeCalculator.GetMaxAttractor(fatum, coordList, densityGrid);
                break;
            case EnumAnomalyType.Void:
                result = _kdeCalculator.GetMaxVoid(fatum, coordList, densityGrid);
                break;
            case EnumAnomalyType.None:
            default:
                result = _kdeCalculator.GetMaxAnomalia(fatum, coordList, densityGrid);
                break;
        }

        if (result == null)
        {
            _logger.LogInformation("No se encontró anomalía para este Fatum.");
            return null;
        }

        result.OpenLocationCode = LocationCodesHelps.GetOpenLocationCode(result.Latitude, result.Longitude);
        result.GeoHash = LocationCodesHelps.GetGeoHash(result.Latitude, result.Longitude);
        if (!string.IsNullOrWhiteSpace(what3WordsApiKey))
        {
            result.What3Words = await ConvertToWhat3WordsAsync(result.Latitude, result.Longitude, what3WordsApiKey!, cancellationToken)
                .ConfigureAwait(false);
        }

        long fatumId = await this._unitOfWork.FatumRepository.AddAsync(fatum).ConfigureAwait(false);
        result.IdFatum = fatumId;
        await this._unitOfWork.AnomaliaRepository.AddAsync(result).ConfigureAwait(false);

        _logger.LogInformation("Fatum {FatumId} y Anomalía guardados. Power={Power}", fatumId, result.Power);
        return result;
    }

    
    private async Task<string?> ConvertToWhat3WordsAsync(
        double latitude,
        double longitude,
        string apiKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var inv = CultureInfo.InvariantCulture;
            var latStr = latitude.ToString(inv);
            var lonStr = longitude.ToString(inv);
            var coordinates = $"{latStr},{lonStr}";

            var httpClient = _httpClientFactory.CreateClient("What3Words");
            var url =
                $"https://api.what3words.com/v3/convert-to-3wa?coordinates={Uri.EscapeDataString(coordinates)}&key={Uri.EscapeDataString(apiKey)}&language=en&format=json";

            using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (doc.RootElement.TryGetProperty("words", out var wordsEl))
                return wordsEl.GetString();

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error convirtiendo coordenadas a what3words");
            return null;
        }
    }



    public int CalcularPuntosOptimos(int radiusMeters)
    {
        if (radiusMeters <= 1000) return 1024;
        else if (radiusMeters <= 2000) return 4096;
        else if (radiusMeters <= 7000) return 5120;
        else return 10240;
    }
}


