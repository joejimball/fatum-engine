using FatumCommon.Domain;
using FatumCommon.Enums;
using FatumCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace FatumCommon.Helps
{
    public static class FatumHelps
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const int EARTH_RADIUS = 6371; // km
        public const int EARTH_RADIUS_METROS = EARTH_RADIUS * 1000;
        public const double ONE_DEGREE = EARTH_RADIUS * 2 * Math.PI / 360 * 1000; // 1° latitud en metros

        private const string ApiUrl = "https://api.quantumnumbers.anu.edu.au";
        private const int MaxNumbersPerRequest = 1024;
        private const int RequestDelayMs = 1100;
        private const int MaxIntentosPorBloque = 5;

        /// <summary>
        /// Bytes consumidos por punto: 2 ushort por punto (Pairwise) x 2 bytes por ushort.
        /// Cualquier valor que se pase a GetArrayBytesRandomAsync debe ser multiplo de esto.
        /// </summary>
        public const int BytesPorPunto = 4;

        private static readonly HttpClient Client = CrearCliente();

        private static HttpClient CrearCliente()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = 4
            };
            return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        }

        #region QRNG

        public static async Task<byte[]> GetQuantumRandomBytes(int totalBytes, string apiKey)
        {
            if (totalBytes <= 0) return Array.Empty<byte>();
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("Se requiere apiKey para el QRNG.", nameof(apiKey));

            var allBytes = new List<byte>(totalBytes);
            bool firstRequest = true;
            int intentosFallidos = 0;

            while (allBytes.Count < totalBytes)
            {
                if (!firstRequest) await Task.Delay(RequestDelayMs).ConfigureAwait(false);
                firstRequest = false;

                int remainingBytes = totalBytes - allBytes.Count;
                const int size = 8;
                int numbersToGet = (int)Math.Ceiling(remainingBytes / (double)size);
                numbersToGet = Math.Min(numbersToGet, MaxNumbersPerRequest);

                string url = $"{ApiUrl}?length={numbersToGet}&type=hex16&size={size}";

                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("x-api-key", apiKey);

                    using var response = await Client.SendAsync(request).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        if (++intentosFallidos >= MaxIntentosPorBloque)
                            throw new HttpRequestException(
                                $"QRNG devolvio {(int)response.StatusCode} tras {intentosFallidos} intentos.");

                        log.Warn($"QRNG {(int)response.StatusCode}. Reintento {intentosFallidos}/{MaxIntentosPorBloque}.");
                        continue;
                    }

                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    ProcesarRndJson(json, allBytes);
                    intentosFallidos = 0;
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    if (++intentosFallidos >= MaxIntentosPorBloque)
                        throw new HttpRequestException(
                            $"QRNG fallo tras {intentosFallidos} intentos: {ex.Message}", ex);

                    log.Warn($"QRNG error ({ex.GetType().Name}). Reintento {intentosFallidos}/{MaxIntentosPorBloque}.");
                }
            }

            if (allBytes.Count > totalBytes)
                allBytes.RemoveRange(totalBytes, allBytes.Count - totalBytes);

            return allBytes.ToArray();
        }

        public static void ProcesarRndJson(string json, List<byte> allBytes)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement data = doc.RootElement.GetProperty("data");

            foreach (JsonElement hexElement in data.EnumerateArray())
            {
                string hex = hexElement.GetString();
                if (string.IsNullOrEmpty(hex)) continue;
                allBytes.AddRange(HexToBytes(hex));
            }
        }

        public static byte[] HexToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("La cadena hexadecimal debe tener longitud par");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

            return bytes;
        }

        #endregion

        #region Geometria

        public static double GetDistance(double lat0, double lon0, double lat1, double lon1)
        {
            double dlon = (lon1 - lon0) * Math.PI / 180;
            double dlat = (lat1 - lat0) * Math.PI / 180;

            double lat0Rad = lat0 * Math.PI / 180;
            double lat1Rad = lat1 * Math.PI / 180;

            double sinHalfDlat = Math.Sin(dlat / 2);
            double sinHalfDlon = Math.Sin(dlon / 2);

            double a = (sinHalfDlat * sinHalfDlat)
                     + Math.Cos(lat0Rad) * Math.Cos(lat1Rad) * (sinHalfDlon * sinHalfDlon);

            a = Math.Max(0, Math.Min(1, a));

            double angle = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return angle * EARTH_RADIUS_METROS;
        }

        /// <summary>
        /// Fraccion del disco de radio <paramref name="radioConsulta"/> centrado a
        /// <paramref name="distanciaCentro"/> del centro del Fatum que cae dentro del Fatum.
        /// Corrige el sesgo de borde del estimador kNN: cerca del limite el vecino k-esimo
        /// esta mas lejos porque solo hay puntos de un lado, y sin esta correccion la
        /// densidad local se subestima y aparece un vacio falso en el perimetro.
        /// </summary>
        public static double FraccionDentroDelDisco(double distanciaCentro, double radioConsulta, double radioDisco)
        {
            if (radioConsulta <= 0 || radioDisco <= 0) return 1.0;

            double d = Math.Abs(distanciaCentro);
            double r = radioConsulta;
            double R = radioDisco;

            if (d >= R + r) return 0.0;
            if (d + r <= R) return 1.0;
            if (d < 1e-12) return Math.Min(1.0, (R * R) / (r * r));

            double prod = (-d + r + R) * (d + r - R) * (d - r + R) * (d + r + R);
            if (prod < 0) prod = 0;

            double a1 = Math.Acos(Math.Clamp((d * d + r * r - R * R) / (2 * d * r), -1.0, 1.0));
            double a2 = Math.Acos(Math.Clamp((d * d + R * R - r * r) / (2 * d * R), -1.0, 1.0));

            double area = r * r * a1 + R * R * a2 - 0.5 * Math.Sqrt(prod);
            double frac = area / (Math.PI * r * r);

            return Math.Clamp(frac, 0.0, 1.0);
        }

        #endregion

        #region Conversion

        public static List<ushort> ConvertirHexStringToListNumeros(string hexString)
        {
            var numbers = new List<ushort>();
            for (int i = 0; i + 4 <= hexString.Length; i += 4)
                numbers.Add(Convert.ToUInt16(hexString.Substring(i, 4), 16));

            return numbers;
        }

        /// <summary>
        /// Agrupa bytes en ushort. Si el array tiene longitud impar se descarta el
        /// ultimo byte y se registra un warning, en lugar de lanzar ArgumentException
        /// desde BitConverter.ToUInt16.
        /// </summary>
        public static List<ushort> ConvertirArrayBytesToListNumeros(byte[] arr)
        {
            if (arr == null || arr.Length == 0) return new List<ushort>();

            if (arr.Length % 2 != 0)
                log.Warn($"ConvertirArrayBytesToListNumeros: {arr.Length} bytes (impar), se descarta el ultimo byte.");

            int limite = arr.Length - (arr.Length % 2);
            var numbers = new List<ushort>(limite / 2);

            for (int i = 0; i < limite; i += 2)
                numbers.Add(BitConverter.ToUInt16(arr, i));

            return numbers;
        }

        public static int PuntosDesdeBytes(int bytes) => Math.Max(0, bytes) / BytesPorPunto;

        public static int BytesDesdePuntos(int puntos) => Math.Max(0, puntos) * BytesPorPunto;

        #endregion

        #region TestPoint

        public static AnomalyAnalysisResult TestPoint(
            double lat,
            double lon,
            double radius,
            IList<(double lat, double lon)> testpoints,
            EnumAnomalyType anomalyType)
        {
            var np = TestPoint(lat, lon, radius, testpoints);

            if (anomalyType == EnumAnomalyType.Attractor)
                np.IntensidadAnomalia = np.DensidadRelativa;
            else if (anomalyType == EnumAnomalyType.Void)
                np.IntensidadAnomalia = 1.0 / (np.DensidadRelativa + 1e-9);

            return np;
        }

        /// <summary>
        /// Version con centro explicito del Fatum. Usar esta desde KdeCalculator.
        /// </summary>
        public static AnomalyAnalysisResult TestPoint(
            double lat,
            double lon,
            double radius,
            IList<(double lat, double lon)> testpoints,
            EnumAnomalyType anomalyType,
            double centerLat,
            double centerLon)
        {
            var np = TestPoint(lat, lon, radius, testpoints, centerLat, centerLon);

            if (anomalyType == EnumAnomalyType.Attractor)
                np.IntensidadAnomalia = np.DensidadRelativa;
            else if (anomalyType == EnumAnomalyType.Void)
                np.IntensidadAnomalia = 1.0 / (np.DensidadRelativa + 1e-9);

            return np;
        }

        /// <summary>
        /// Sin centro explicito: se estima como el centroide de la nube de puntos.
        /// Aceptable para un disco uniforme (error ~0.7R/sqrt(N)), sesgado si hay clusters.
        /// </summary>
        public static AnomalyAnalysisResult TestPoint(
            double lat,
            double lon,
            double radius,
            IList<(double lat, double lon)> testpoints)
        {
            if (testpoints == null || testpoints.Count == 0 || radius <= 0)
                return new AnomalyAnalysisResult();

            double sumLat = 0, sumLon = 0;
            for (int i = 0; i < testpoints.Count; i++)
            {
                sumLat += testpoints[i].lat;
                sumLon += testpoints[i].lon;
            }

            return TestPoint(lat, lon, radius, testpoints,
                sumLat / testpoints.Count, sumLon / testpoints.Count);
        }

        public static AnomalyAnalysisResult TestPoint(
            double lat,
            double lon,
            double radius,
            IList<(double lat, double lon)> testpoints,
            double centerLat,
            double centerLon)
        {
            var np = new AnomalyAnalysisResult();

            if (testpoints == null || testpoints.Count == 0) return np;
            if (radius <= 0) return np;

            int N = testpoints.Count;

            int k = (int)Math.Sqrt(N);
            if (k < 3) k = 3;
            if (k > 200) k = 200;
            if (k > N) k = N;

            var distancias = new double[N];
            for (int i = 0; i < N; i++)
            {
                var c = testpoints[i];
                distancias[i] = GetDistance(lat, lon, c.lat, c.lon);
            }

            double distanciaMin = double.MaxValue;
            for (int i = 0; i < N; i++)
                if (distancias[i] < distanciaMin) distanciaMin = distancias[i];
            np.DistanciaMasCercana = distanciaMin;

            var ordenadas = (double[])distancias.Clone();
            Array.Sort(ordenadas);

            double rk = ordenadas[k - 1];
            if (rk < 1e-2) rk = 1e-2;
            np.RadioAproximado = rk;

            int puntosCercanos = 0;
            for (int i = 0; i < N; i++)
                if (distancias[i] <= rk) puntosCercanos++;

            double areaLocal = Math.PI * rk * rk;
            double areaGlobal = Math.PI * radius * radius;
            const double epsilon = 1e-9;

            // Correccion de frontera: area efectiva del vecindario dentro del Fatum.
            double distCentro = GetDistance(centerLat, centerLon, lat, lon);
            double fraccion = FraccionDentroDelDisco(distCentro, rk, radius);
            if (fraccion < 1e-3) fraccion = 1e-3;

            // Sin pseudocount: el +1 inflaba la densidad un 14% en el centro
            // y favorecia artificialmente a los atractores.
            double densidadLocal = puntosCercanos / (areaLocal * fraccion + epsilon);
            double densidadGlobal = N / (areaGlobal + epsilon);

            np.DensidadRelativa = densidadLocal / densidadGlobal;
            return np;
        }

        #endregion

        #region Radio aproximado

        public static double CalculaRadioAproximado(
            double lat,
            double lon,
            IList<(double lat, double lon)> testpoints,
            EnumAnomalyType anomalyType)
        {
            int minPoints = anomalyType == EnumAnomalyType.Void ? 3 : 12;

            if (testpoints == null || !testpoints.Any()) return 0;

            var distancias = testpoints
                .Select(coord => GetDistance(lat, lon, coord.lat, coord.lon))
                .ToArray();

            int numPts = Math.Min(minPoints, distancias.Length);
            var distanciasOrdenadas = distancias.OrderBy(d => d).ToArray();

            return distanciasOrdenadas[numPts - 1];
        }

        #endregion

        #region Conteo de puntos

        /// <summary>
        /// Devuelve BYTES, no puntos. El llamador pasa el resultado directo a
        /// GetArrayBytesRandomAsync, que produce PuntosDesdeBytes(resultado) puntos.
        /// </summary>
        public static int CalcularPuntosOptimos(double radioMetros)
        {
            double radioKm = radioMetros / 1000.0;

            if (radioKm < 1) radioKm = 1;
            if (radioKm > 25) radioKm = 25;

            int puntos;
            if (radioKm <= 1) puntos = 1024;
            else if (radioKm <= 2) puntos = 2560;
            else puntos = 5120;

            return BytesDesdePuntos(puntos);
        }

        /// <summary>
        /// Devuelve BYTES, no puntos. Ver CalcularPuntosOptimos.
        /// </summary>
        public static int CalcularPuntosOptimosMejorado(double radioMetros)
        {
            double radioKm = radioMetros / 1000.0;

            if (radioKm < 1) radioKm = 1;
            if (radioKm > 25) radioKm = 25;

            if (radioKm <= 10)
            {
                var radioPuntosOriginales = new Dictionary<int, int>
                {
                    { 1, 1024 },  // 326 pts/km2
                    { 2, 2048 },  // 163 pts/km2
                    { 3, 3072 },  // 109 pts/km2
                    { 4, 4096 },  //  82 pts/km2
                    { 5, 5120 },  //  65 pts/km2
                    { 6, 6144 },  //  54 pts/km2
                    { 7, 7168 },  //  47 pts/km2
                    { 8, 8192 },  //  41 pts/km2
                    { 9, 9216 },  //  36 pts/km2
                    { 10, 10240 } //  33 pts/km2
                };

                int radioRedondeado = (int)Math.Round(radioKm);
                if (radioRedondeado < 1) radioRedondeado = 1;
                if (radioRedondeado > 10) radioRedondeado = 10;

                return BytesDesdePuntos(radioPuntosOriginales[radioRedondeado]);
            }

            double areaKm2 = Math.PI * radioKm * radioKm;
            double puntosBase = 40.0 * areaKm2;

            int multiplo = (int)Math.Round(puntosBase / 1024.0);
            if (multiplo < 1) multiplo = 1;

            return BytesDesdePuntos(multiplo * 1024);
        }

        #endregion

        #region Generacion de puntos

        public static List<(double lat, double lon)> GetRandomPoints(Fatum fatum)
        {
            return GetRandomPoints_Impl(fatum.Latitude, fatum.Longitude, fatum.Radio, fatum.RandomData);
        }

        /// <summary>
        /// Keystream ChaCha20 derivado de la semilla. El nonce se deriva de
        /// (semilla, contador de bloque) para que sea unico por bloque y reproducible;
        /// el nonce fijo en ceros reutilizaba el keystream entre llamadas.
        /// </summary>
        public static byte[] ChaCha20Stream(byte[] seed, int byteCount)
        {
            if (seed == null || seed.Length != 32)
                throw new ArgumentException("La semilla debe tener exactamente 32 bytes.", nameof(seed));

            if (byteCount <= 0) return Array.Empty<byte>();

            var salida = new byte[byteCount];
            var nonce = new byte[12];

            using var cipher = new ChaCha20Poly1305(seed);

            int offset = 0;
            uint contador = 0;

            while (offset < byteCount)
            {
                using (var hmac = new HMACSHA256(seed))
                {
                    var etiqueta = BitConverter.GetBytes(contador);
                    var hash = hmac.ComputeHash(etiqueta);
                    Buffer.BlockCopy(hash, 0, nonce, 0, 12);
                }

                int chunk = Math.Min(byteCount - offset, 65536);
                var pt = new byte[chunk];
                var ct = new byte[chunk];
                var tag = new byte[16];

                cipher.Encrypt(nonce, pt, ct, tag);
                Buffer.BlockCopy(ct, 0, salida, offset, chunk);

                offset += chunk;
                contador++;
            }

            return salida;
        }

        public static double IntegerToFloat(ushort i) => i / 65536.0;

        public static (double randomLat, double randomLon) RandomLocation(
            double startLat,
            double startLon,
            double maxRadius,
            double randFloat1,
            double randFloat2)
        {
            double rLen = maxRadius * Math.Sqrt(randFloat1);
            double theta = randFloat2 * 2 * Math.PI;

            double dX = rLen * Math.Cos(theta);
            double dY = rLen * Math.Sin(theta);

            double randomLat = startLat + dY / ONE_DEGREE;
            double randomLon = startLon + dX / (ONE_DEGREE * Math.Cos(startLat * Math.PI / 180.0));

            return (randomLat, randomLon);
        }

        public static async Task<byte[]> GetArrayBytesRandomAsync(int tamArray, EnumTypeRnd typeRnd, string apiKey)
        {
            byte[] arr;

            switch (typeRnd)
            {
                case Enums.EnumTypeRnd.QRNG:
                    arr = await GetQuantumRandomBytes(tamArray, apiKey).ConfigureAwait(false);
                    break;

                case Enums.EnumTypeRnd.REG:
                case Enums.EnumTypeRnd.PRNG:
                default:
                    arr = new byte[tamArray];
                    RandomHelps.RndLocal.NextBytes(arr);
                    break;
            }

            return arr;
        }

        private static List<(double lat, double lon)> GetRandomPoints_Impl(
            double latitude,
            double longitude,
            double radio,
            byte[] randomData)
        {
            var coordList = new List<(double lat, double lon)>();

            if (randomData == null || randomData.Length < BytesPorPunto)
            {
                log.Warn("GetRandomPoints: datos aleatorios insuficientes.");
                return coordList;
            }

            int puntosEsperados = PuntosDesdeBytes(randomData.Length);
            var numeros = ConvertirArrayBytesToListNumeros(randomData);

            foreach ((ushort value1, ushort value2) in numeros.Pairwise())
            {
                var randFloat1 = IntegerToFloat(value1);
                var randFloat2 = IntegerToFloat(value2);
                coordList.Add(RandomLocation(latitude, longitude, radio, randFloat1, randFloat2));
            }

            if (coordList.Count != puntosEsperados)
                log.Warn($"GetRandomPoints: {coordList.Count} puntos generados, {puntosEsperados} esperados.");

            return coordList;
        }

        #endregion
    }
}