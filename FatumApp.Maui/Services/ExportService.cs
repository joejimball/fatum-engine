using System.Globalization;
using System.Text;
using System.Text.Json;
using FatumCommon.Domain;
using FatumCommon.Enums;
using FatumCommon.Helps;
using FatumCommon;
using FatumApp.Maui.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using FatumCommon.Infrastructure.Data;

namespace FatumApp.Maui.Services;

public interface IExportService
{
    Task<string> ExportAsJsonAsync(CancellationToken cancellationToken = default);
    Task<string> ExportAsCsvAsync(CancellationToken cancellationToken = default);
    Task ShareExportAsync(string fileName, string content, string title, CancellationToken cancellationToken = default);
    Task<int> ImportFromJsonAsync(string json, CancellationToken cancellationToken = default);
}

public sealed class ExportService : IExportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IUnitOfWork unitOfWork,
        ILogger<ExportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<string> ExportAsJsonAsync(CancellationToken cancellationToken = default)
    {
        var fatums = (await _unitOfWork.FatumRepository.GetAllAsync().ConfigureAwait(false)).ToList();
        var anomalias = (await _unitOfWork.AnomaliaRepository.GetAllAsync().ConfigureAwait(false)).ToList();
        var dto = new ExportDto
        {
            ExportedAt = DateTime.UtcNow,
            Fatums = fatums.Select(f => new FatumExportItem
            {
                Id = f.Id,
                Latitude = f.Latitude,
                Longitude = f.Longitude,
                    OpenLocationCode = f.OpenLocationCode,
                    GeoHash = f.GeoHash,
                    What3Words = f.What3Words,
                    GridSize = f.GridSize,
                    Bandwidth = f.Bandwidth,
                Radio = f.Radio,
                NumPuntos = f.NumPuntos,
                TypeRnd = (int)f.TypeRnd,
                CreatedAt = f.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
                Hmac = f.Hmac,
                RandomDataBase64 = f.RandomData != null ? Convert.ToBase64String(f.RandomData) : null
            }).ToList(),
            Anomalias = anomalias.Select(a => new AnomaliaExportItem
            {
                Id = a.Id,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                OpenLocationCode = a.OpenLocationCode,
                GeoHash = a.GeoHash,
                What3Words = a.What3Words,
                DensidadEstimacion = a.DensidadEstimacion,
                Power = a.Power,
                ZScore = a.ZScore,
                ZScoreFinal = a.ZScoreFinal,
                QZScore = a.QZScore,
                RadioAproximado = a.RadioAproximado,
                TipoAnomalia = (int)a.TipoAnomalia,
                IdFatum = a.IdFatum,
                Distancia = a.Distancia,
                QuantumPotential = a.QuantumPotential
            }).ToList()
        };
        return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<string> ExportAsCsvAsync(CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id;Latitude;Longitude;OpenLocationCode;GeoHash;What3Words;GridSize;Bandwidth;Radio;NumPuntos;TypeRnd;CreatedAt;Hmac");
        var fatums = (await _unitOfWork.FatumRepository.GetAllAsync().ConfigureAwait(false)).ToList();
        foreach (var f in fatums)
            sb.AppendLine($"{f.Id};{f.Latitude.ToString(CultureInfo.InvariantCulture)};{f.Longitude.ToString(CultureInfo.InvariantCulture)};{f.OpenLocationCode};{f.GeoHash};{f.What3Words};{f.GridSize};{f.Bandwidth};{f.Radio};{f.NumPuntos};{(int)f.TypeRnd};{f.CreatedAt:O};{f.Hmac}");
        sb.AppendLine();
        sb.AppendLine("Id;Latitude;Longitude;OpenLocationCode;GeoHash;What3Words;DensidadEstimacion;Power;ZScore;ZScoreFinal;QZScore;RadioAproximado;TipoAnomalia;IdFatum;Distancia;QuantumPotential");
        var anomalias = (await _unitOfWork.AnomaliaRepository.GetAllAsync().ConfigureAwait(false)).ToList();
        foreach (var a in anomalias)
            sb.AppendLine($"{a.Id};{a.Latitude.ToString(CultureInfo.InvariantCulture)};{a.Longitude.ToString(CultureInfo.InvariantCulture)};{a.OpenLocationCode};{a.GeoHash};{a.What3Words};{a.DensidadEstimacion.ToString(CultureInfo.InvariantCulture)};{a.Power.ToString(CultureInfo.InvariantCulture)};{a.ZScore.ToString(CultureInfo.InvariantCulture)};{a.ZScoreFinal.ToString(CultureInfo.InvariantCulture)};{a.QZScore.ToString(CultureInfo.InvariantCulture)};{a.RadioAproximado.ToString(CultureInfo.InvariantCulture)};{(int)a.TipoAnomalia};{a.IdFatum};{a.Distancia.ToString(CultureInfo.InvariantCulture)};{a.QuantumPotential.ToString(CultureInfo.InvariantCulture)}");
        return sb.ToString();
    }

    public async Task ShareExportAsync(string fileName, string content, string title, CancellationToken cancellationToken = default)
    {
        try
        {
            var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(tempPath, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = title,
                File = new ShareFile(tempPath)
            }).ConfigureAwait(false);
            _logger.LogInformation("Exportación compartida: {File}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al compartir exportación");
            throw;
        }
    }

    public async Task<int> ImportFromJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        var dto = JsonSerializer.Deserialize<ExportDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (dto?.Fatums == null)
            return 0;

        var oldFatumIdToNew = new Dictionary<long, long>();

        foreach (var item in dto.Fatums)
        {
            var fatum = new Fatum
            {
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                OpenLocationCode = string.IsNullOrWhiteSpace(item.OpenLocationCode)
                    ? LocationCodesHelps.GetOpenLocationCode(item.Latitude, item.Longitude)
                    : item.OpenLocationCode,
                GeoHash = string.IsNullOrWhiteSpace(item.GeoHash)
                    ? LocationCodesHelps.GetGeoHash(item.Latitude, item.Longitude)
                    : item.GeoHash,
                What3Words = item.What3Words,
                GridSize = item.GridSize ?? new KdeCalculator().GetGridSize(item.Radio),
                Bandwidth = item.Bandwidth ?? CalculateSilvermanBandwidth(item.NumPuntos),
                Radio = item.Radio,
                NumPuntos = item.NumPuntos,
                TypeRnd = (EnumTypeRnd)item.TypeRnd,
                CreatedAt = DateTime.Parse(item.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                RandomData = string.IsNullOrEmpty(item.RandomDataBase64) ? null : Convert.FromBase64String(item.RandomDataBase64)
            };
            fatum.Hmac = FatumHmacHelper.ComputeHmacSha256Hex(fatum);
            if (!string.IsNullOrWhiteSpace(item.Hmac) &&
                !string.Equals(item.Hmac.Trim(), fatum.Hmac, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Import JSON: HMAC del Fatum origen Id={OldId} no coincide con los datos; se guarda el hash recalculado.",
                    item.Id);
            }

            var newId = await _unitOfWork.FatumRepository.AddAsync(fatum).ConfigureAwait(false);
            oldFatumIdToNew[item.Id] = newId;
        }

        if (dto.Anomalias == null)
            return dto.Fatums.Count;

        foreach (var item in dto.Anomalias)
        {
            if (!oldFatumIdToNew.TryGetValue(item.IdFatum, out var newFatumId))
                continue;
            var anomalia = new Anomalia
            {
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                OpenLocationCode = string.IsNullOrWhiteSpace(item.OpenLocationCode)
                    ? LocationCodesHelps.GetOpenLocationCode(item.Latitude, item.Longitude)
                    : item.OpenLocationCode,
                GeoHash = string.IsNullOrWhiteSpace(item.GeoHash)
                    ? LocationCodesHelps.GetGeoHash(item.Latitude, item.Longitude)
                    : item.GeoHash,
                What3Words = item.What3Words,
                DensidadEstimacion = item.DensidadEstimacion,
                Power = item.Power,
                ZScore = item.ZScore,
                QZScore = item.QZScore,
                ZScoreFinal = item.ZScoreFinal,
                RadioAproximado = item.RadioAproximado,
                TipoAnomalia = (EnumAnomalyType)item.TipoAnomalia,
                IdFatum = newFatumId,
                Distancia = item.Distancia,
                QuantumPotential = item.QuantumPotential
            };
            await _unitOfWork.AnomaliaRepository.AddAsync(anomalia).ConfigureAwait(false);
        }

        _logger.LogInformation("Importados {FatumCount} fatums y {AnomaliaCount} anomalías", dto.Fatums.Count, dto.Anomalias?.Count ?? 0);
        return dto.Fatums.Count + (dto.Anomalias?.Count ?? 0);
    }

    private sealed class ExportDto
    {
        public DateTime ExportedAt { get; set; }
        public List<FatumExportItem> Fatums { get; set; } = new();
        public List<AnomaliaExportItem> Anomalias { get; set; } = new();
    }

    private sealed class FatumExportItem
    {
        public long Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? OpenLocationCode { get; set; }
        public string? GeoHash { get; set; }
        public string? What3Words { get; set; }
        public int? GridSize { get; set; }
        public double? Bandwidth { get; set; }
        public double Radio { get; set; }
        public long NumPuntos { get; set; }
        public int TypeRnd { get; set; }
        public string CreatedAt { get; set; } = "";
        public string? Hmac { get; set; }
        public string? RandomDataBase64 { get; set; }
    }

    private sealed class AnomaliaExportItem
    {
        public long Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? OpenLocationCode { get; set; }
        public string? GeoHash { get; set; }
        public string? What3Words { get; set; }
        public double DensidadEstimacion { get; set; }
        public double Power { get; set; }
        public double ZScore { get; set; }
        public double ZScoreFinal { get; set; }
        public double RadioAproximado { get; set; }
        public int TipoAnomalia { get; set; }
        public long IdFatum { get; set; }
        public double Distancia { get; set; }
        public double QuantumPotential { get; set; }
        public double QZScore { get; set; }
    }

    private static double CalculateSilvermanBandwidth(long n)
    {
        const double d = 2.0;
        if (n <= 0) return 0;
        return Math.Pow(4.0 / (d + 2.0), 1.0 / (d + 4.0)) * Math.Pow(n, -1.0 / (d + 4.0));
    }
}
