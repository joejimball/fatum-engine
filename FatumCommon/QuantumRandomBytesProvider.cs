using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FatumCommon.Services;

public interface IRandomBytesProvider
{
    /// <summary>
    /// Obtiene un array de bytes aleatorios desde la API de números cuánticos.
    /// </summary>
    /// <param name="totalBytes">Número total de bytes requeridos.</param>
    /// <param name="apiKey">Clave de API para autenticación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Array de bytes aleatorios.</returns>
    Task<byte[]> GetRandomBytesAsync(int totalBytes, string apiKey, CancellationToken cancellationToken = default);
}

public class QuantumRandomBytesProvider : IRandomBytesProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<QuantumRandomBytesProvider> _logger;

    // Constantes de configuración (pueden externalizarse a IConfiguration si se desea)
    private const string ApiUrl = "https://api.quantumnumbers.anu.edu.au";
    private const int MaxNumbersPerRequest = 1024;   // Máximo de números por petición
    private const int MaxSizePerNumber = 10;         // Máximo "size" permitido (1-10)
    private const int RequestDelayMs = 1100;          // Retardo entre peticiones (respetar límite de tasa)
    private const int MaxRetries = 3;                 // Reintentos máximos por petición
    private const string HttpClientName = "QuantumRandom";

    public QuantumRandomBytesProvider(IHttpClientFactory httpClientFactory, ILogger<QuantumRandomBytesProvider> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<byte[]> GetRandomBytesAsync(int totalBytes, string apiKey, CancellationToken cancellationToken = default)
    {
        if (totalBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalBytes), "Debe solicitar al menos 1 byte.");
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentNullException(nameof(apiKey));

        // Preasignamos el array final con el tamaño exacto necesario
        byte[] result = new byte[totalBytes];
        int bytesCollected = 0;
        bool firstRequest = true;

        while (bytesCollected < totalBytes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Respetar límite de tasa entre peticiones (excepto la primera)
            if (!firstRequest)
                await Task.Delay(RequestDelayMs, cancellationToken).ConfigureAwait(false);
            firstRequest = false;

            int remainingBytes = totalBytes - bytesCollected;
            int numbersToGet = (int)Math.Ceiling((double)remainingBytes / MaxSizePerNumber);
            numbersToGet = Math.Min(numbersToGet, MaxNumbersPerRequest);

            byte[] chunk = await RequestRandomBytesChunkAsync(numbersToGet, apiKey, cancellationToken).ConfigureAwait(false);

            int bytesToCopy = Math.Min(chunk.Length, remainingBytes);
            Array.Copy(chunk, 0, result, bytesCollected, bytesToCopy);
            bytesCollected += bytesToCopy;
        }

        return result;
    }

    /// <summary>
    /// Solicita un bloque de números aleatorios a la API y los convierte a bytes.
    /// Implementa reintentos con backoff exponencial.
    /// </summary>
    private async Task<byte[]> RequestRandomBytesChunkAsync(int numbersToGet, string apiKey, CancellationToken cancellationToken)
    {
        int retryCount = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var httpClient = _httpClientFactory.CreateClient(HttpClientName);

                string url = $"{ApiUrl}?length={numbersToGet}&type=hex16&size={MaxSizePerNumber}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-api-key", apiKey);

                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                                      .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    return await ParseHex16ResponseAsync(stream, numbersToGet, cancellationToken).ConfigureAwait(false);
                }

                // Si no es exitoso, lanzar excepción para que entre en el catch
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex) when (retryCount < MaxRetries)
            {
                retryCount++;
                _logger.LogWarning(ex, "Error en petición a la API (intento {RetryCount}/{MaxRetries}).", retryCount, MaxRetries);
                // Backoff exponencial: 1s, 2s, 4s...
                int delay = RequestDelayMs * (1 << retryCount); // 1 << retryCount equivale a 2^retryCount
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Número máximo de reintentos alcanzado ({MaxRetries}) para la petición.", MaxRetries);
                throw; // Relanzar después de los reintentos fallidos
            }
        }
    }

    /// <summary>
    /// Parsea la respuesta JSON en formato hex16 y devuelve un array de bytes.
    /// Utiliza System.Text.Json para una lectura eficiente.
    /// </summary>
    private static async Task<byte[]> ParseHex16ResponseAsync(Stream jsonStream, int expectedNumbers, CancellationToken cancellationToken)
    {
        using var doc = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!doc.RootElement.TryGetProperty("data", out var dataArray))
            throw new InvalidOperationException("La respuesta JSON no contiene la propiedad 'data'.");

        // Preasignamos una capacidad aproximada para reducir redimensionamientos
        var bytes = new System.Collections.Generic.List<byte>(expectedNumbers * MaxSizePerNumber);

        foreach (var item in dataArray.EnumerateArray())
        {
            string hex = item.GetString() ?? throw new InvalidOperationException("Valor nulo en array data.");
            // Convertir hex a bytes (asumiendo que la cadena tiene longitud par)
            for (int i = 0; i < hex.Length; i += 2)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bytes.Add(Convert.ToByte(hex.Substring(i, 2), 16));
            }
        }

        return bytes.ToArray();
    }
}
