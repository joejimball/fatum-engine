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
        public const int EARTH_RADIUS = 6371;  // km
        public const int EARTH_RADIUS_METROS = EARTH_RADIUS * 1000;
        public const double ONE_DEGREE = EARTH_RADIUS * 2 * Math.PI / 360 * 1000; // 1° latitude in meters
        private const string ApiUrl = "https://api.quantumnumbers.anu.edu.au";
        private const int MaxNumbersPerRequest = 1024;
        private const int RequestDelayMs = 1100;
        public static async Task<byte[]> GetQuantumRandomBytes(int totalBytes, string apiKey)
        {
            List<byte> allBytes = new List<byte>(totalBytes);
            int bytesCollected = 0;
            bool firstRequest = true;

            using (var _client = new HttpClient())
            {
                while (bytesCollected < totalBytes)
                {

                    if (!firstRequest)
                    {
                        await Task.Delay(RequestDelayMs);
                    }
                    firstRequest = false;

                    // Calcular bytes restantes y ajustar chunk
                    int remainingBytes = totalBytes - bytesCollected;
                    int size = 8;//1 Block size, only needed for 'hex8' and 'hex16' data types. Sets the length of each block. Must be between 1-10.
                    int numbersToGet = (int)Math.Ceiling(remainingBytes / (double)size);
                    numbersToGet = Math.Min(numbersToGet, MaxNumbersPerRequest);

                    // Crear URL de solicitud
                    string url = $"{ApiUrl}?length={numbersToGet}&type=hex16&size={size}";

                    // Crear solicitud HTTP con API Key
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("x-api-key", apiKey); // Agregar API Key al header

                    // Realizar solicitud HTTP
                    HttpResponseMessage response = await _client.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        await Task.Delay(RequestDelayMs);
                        continue;
                    }

                    // Procesar respuesta
                    string json = await response.Content.ReadAsStringAsync();
                    ProcesarRndJson(json, allBytes);
                    bytesCollected = allBytes.Count;
                }
            }

            return allBytes.ToArray();
        }

        public static void ProcesarRndJson(string json, List<byte> allBytes)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement data = doc.RootElement.GetProperty("data");

                // Convertir hex a bytes
                foreach (JsonElement hexElement in data.EnumerateArray())
                {
                    string hex = hexElement.GetString();
                    byte[] bytes = HexToBytes(hex);

                    allBytes.AddRange(bytes);
                }
            }
        } 

        // Método auxiliar para convertir hex a bytes (sin cambios)
        public static byte[] HexToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("La cadena hexadecimal debe tener longitud par");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }



        public static double GetDistance(double lat0, double lon0, double lat1, double lon1)
        {
            // Convertir diferencias a radianes
            double dlon = (lon1 - lon0) * Math.PI / 180;
            double dlat = (lat1 - lat0) * Math.PI / 180;

            // Convertir latitudes individuales a radianes
            double lat0Rad = lat0 * Math.PI / 180;
            double lat1Rad = lat1 * Math.PI / 180;

            // Calcular 'a' usando Haversine
            double sinHalfDlat = Math.Sin(dlat / 2);
            double sinHalfDlon = Math.Sin(dlon / 2);
            double a = (sinHalfDlat * sinHalfDlat)
                       + Math.Cos(lat0Rad) * Math.Cos(lat1Rad)
                       * (sinHalfDlon * sinHalfDlon);

            // Evitar errores numéricos (acotar 'a' entre 0 y 1)
            a = Math.Max(0, Math.Min(1, a));

            // Calcular distancia angular
            double angle = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return angle * EARTH_RADIUS_METROS;
        }

       
        public static List<ushort> ConvertirHexStringToListNumeros(string hexString) 
        {
            List<ushort> numbers = new List<ushort>();

            // Dividir el string hexadecimal en chunks de 4 caracteres
            for (int i = 0; i < hexString.Length; i += 4)
            {
                string chunk = hexString.Substring(i, 4);
                // Convertir a ushort (equivalente a uint16 de Python)
                ushort number = Convert.ToUInt16(chunk, 16);
                numbers.Add(number);
            }
            return numbers;
        }

        public static List<ushort> ConvertirArrayBytesToListNumeros(byte[] arr)
        {
            List<ushort> numbers = new List<ushort>();
            // Dividir el string hexadecimal en chunks de 4 caracteres
            for (int i = 0; i < arr.Length; i += 2)
            {
                ushort number = BitConverter.ToUInt16(arr, i);
                numbers.Add(number);
            }
            return numbers;
        }

        public static AnomalyAnalysisResult TestPoint(
            double lat,
            double lon,
            double radius,
            IList<(double lat, double lon)> testpoints,
            EnumAnomalyType anomalyType)
        {
            var np = TestPoint(lat, lon, radius, testpoints);

            if (anomalyType == EnumAnomalyType.Attractor)
            {
                np.IntensidadAnomalia = np.DensidadRelativa;
            }
            else if (anomalyType == EnumAnomalyType.Void)
            {
                np.IntensidadAnomalia = 1 / (np.DensidadRelativa + 1e-9);
            }
            return np;
        }

        public static AnomalyAnalysisResult TestPoint(
           double lat,
           double lon,
           double radius,
           IList<(double lat, double lon)> testpoints)
        {
            var np = new AnomalyAnalysisResult();

            int N = testpoints.Count;

            // Vecinos adaptativos
            int k = Math.Max(3, (int)Math.Sqrt(N));

            // Calcular distancias
            var distancias = testpoints
                .Select(coord => FatumHelps.GetDistance(lat, lon, coord.lat, coord.lon))
                .ToArray();

            np.DistanciaMasCercana = distancias.Min();

            var distanciasOrdenadas = distancias.OrderBy(d => d).ToArray();

            // Radio basado en el k-ésimo vecino
            np.RadioAproximado = distanciasOrdenadas[Math.Min(k, distanciasOrdenadas.Length) - 1];

            int puntosCercanos = distancias.Count(d => d <= np.RadioAproximado);

            // Áreas
            double areaLocal = Math.PI * Math.Pow(np.RadioAproximado, 2);
            double areaGlobal = Math.PI * Math.Pow(radius, 2);

            double epsilon = 1e-9;

            // Densidades con suavizado
            double densidadLocal = (puntosCercanos + 1) / (areaLocal + epsilon);
            double densidadGlobal = (N + 1) / (areaGlobal + epsilon);

            np.DensidadRelativa = densidadLocal / densidadGlobal;

            return np;
        }

        public static double CalculaRadioAproximado(
            double lat,
            double lon,
            IList<(double lat, double lon)> testpoints, EnumAnomalyType anomalyType)
        {
            // Determinar minPoints según tipo de anomalía
            int minPoints = anomalyType == EnumAnomalyType.Void ? 3 : 12;

            // Caso especial: sin puntos en el área
            if (testpoints == null || !testpoints.Any())
            {
                return 0;
            }

            // 1. Calcular distancias
            var distancias = testpoints
                .Select(coord => GetDistance(lat, lon, coord.lat, coord.lon))
                .ToArray();

            // 2. Mejor cálculo del radio aproximado
            int numPts = Math.Min(minPoints, distancias.Length);
            var distanciasOrdenadas = distancias.OrderBy(d => d).ToArray();

            return distanciasOrdenadas[numPts - 1];  
        }

        public static int CalcularPuntosOptimos(double radioMetros)
        {
            // Convertir metros a kilómetros
            double radioKm = radioMetros / 1000;

            // Limitar el radio entre 1 km y 25 km
            if (radioKm < 1) radioKm = 1;
            if (radioKm > 25) radioKm = 25;


            if (radioKm <= 1)
            {
                return 1024;
            }
            else if (radioKm <= 2)
            {
                return 2560;
            }
            else
            {
                return 5120;
            }
        }

        public static int CalcularPuntosOptimosMejorado(double radioMetros)
        {
            // Convertir metros a kilómetros
            double radioKm = radioMetros / 1000;

            // Limitar el radio entre 1 km y 25 km
            if (radioKm < 1) radioKm = 1;
            if (radioKm > 25) radioKm = 25;

            // Calcular el área en km²
            double areaKm2 = Math.PI * Math.Pow(radioKm, 2);

            // Calcular puntos base (40 pts/km² como densidad óptima)
            double puntosBase = 40 * areaKm2;

            // Ajustar al múltiplo de 1024 más cercano (usando redondeo)
            int multiplo = (int)Math.Round(puntosBase / 1024);

            // Garantizar un mínimo de 1 múltiplo (1024 puntos)
            if (multiplo < 1) multiplo = 1;

            // Para radios pequeños (1-10 km), mantener valores originales más densos
            if (radioKm <= 10)
            {
                var radioPuntosOriginales = new Dictionary<double, int>
        {
            { 1, 1024 },   // 326 pts/km²
            { 2, 2048 },   // 163 pts/km²
            { 3, 3072 },   // 109 pts/km²
            { 4, 4096 },   // 82 pts/km²
            { 5, 5120 },   // 65 pts/km²
            { 6, 6144 },   // 54 pts/km²
            { 7, 7168 },   // 47 pts/km²
            { 8, 8192 },   // 41 pts/km²
            { 9, 9216 },   // 36 pts/km²
            { 10, 10240 }  // 33 pts/km²
        };

                // Redondear al km para buscar en el diccionario
                double radioRedondeado = Math.Round(radioKm);
                return radioPuntosOriginales[radioRedondeado];
            }

            return multiplo * 1024;
        }


        public static List<(double lat, double lon)> GetRandomPoints(Fatum fatum)
        {
            return GetRandomPoints_Impl(fatum.Latitude,fatum.Longitude,fatum.Radio, fatum.RandomData);
        }

        public static byte[] ChaCha20Stream(byte[] seed, int byteCount)
        {
            var nonce = new byte[12];
            var plaintext = new byte[byteCount];
            var ciphertext = new byte[byteCount];
            var tag = new byte[16];

            using var cipher = new ChaCha20Poly1305(seed);
            cipher.Encrypt(nonce, plaintext, ciphertext, tag);
            return ciphertext;
        }


        /// <summary>
        /// Convierte un entero de 16 bits en un valor de punto flotante mayor o igual a 0,0 y menor que 1.0.
        /// </summary>
        /// <param name="i">Valor entero de 16 bits sin signo</param>
        /// <returns>Valor en el rango [0, 1)</returns>
        public static double IntegerToFloat(ushort i)
        {
            return i / 65536.0;
        }

        /// <summary>
        /// Convierte 2 valores de punto flotante en coordenadas dentro
        /// del radio definido desde la posición inicial.
        /// </summary>
        /// <param name="startLat">Latitud inicial.</param>
        /// <param name="startLon">Longitud inicial.</param>
        /// <param name="maxRadius">Radio máximo.</param>
        /// <param name="randFloat1">Primer valor flotante aleatorio (entre 0.0 y 1.0).</param>
        /// <param name="randFloat2">Segundo valor flotante aleatorio (entre 0.0 y 1.0).</param>
        /// <returns>Una tupla que contiene la latitud y longitud aleatorias.</returns>
        public static (double randomLat, double randomLon) RandomLocation(
            double startLat,
            double startLon,
            double maxRadius,
            double randFloat1,
            double randFloat2)
        {
            // r_len = max_radius * rand_float_1**0.5
            double rLen = maxRadius * Math.Sqrt(randFloat1);

            // theta = rand_float_2 * 2 * math.pi
            double theta = randFloat2 * 2 * Math.PI;

            // d_x = r_len * math.cos(theta)
            double dX = rLen * Math.Cos(theta);

            // d_y = r_len * math.sin(theta)
            double dY = rLen * Math.Sin(theta);

            // random_lat = start_lat + d_y / ONE_DEGREE
            double randomLat = startLat + dY / ONE_DEGREE;

            // random_lon = start_lon + d_x / (ONE_DEGREE * math.cos(start_lat * math.pi / 180))
            // Asegúrate de que start_lat se convierta a radianes para Math.Cos
            double randomLon = startLon + dX / (ONE_DEGREE * Math.Cos(startLat * Math.PI / 180.0));

            return (randomLat, randomLon);
        }

        public static async Task<byte[]> GetArrayBytesRandomAsync(int tamArray, EnumTypeRnd typeRnd, string apiKey)
        {
            byte[] arr = null;
            switch (typeRnd)
            {
                case Enums.EnumTypeRnd.QRNG:
                    arr = await FatumHelps.GetQuantumRandomBytes(tamArray, apiKey);
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

        private static List<(double lat, double lon)> GetRandomPoints_Impl(double latitude, double longitude, double radio, byte[] randomData)
        {
            List<(double lat, double lon)> coordList = new List<(double lat, double lon)>();
            //Iterate over numbers from QRNG in pairs and convert them to coordinates
            foreach ((ushort value1, ushort value2) in FatumHelps.ConvertirArrayBytesToListNumeros(randomData).Pairwise())
            {
                var randFloat1 = FatumHelps.IntegerToFloat(value1);
                var randFloat2 = FatumHelps.IntegerToFloat(value2);
                var coord = FatumHelps.RandomLocation(latitude, longitude, radio, randFloat1, randFloat2);
                coordList.Add(coord);
            }
            return coordList;
        }
    }
}
