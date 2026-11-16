using FatumCommon.Domain;
using FatumCommon.Enums;
using FatumCommon.Helps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FatumCommon
{
    /// <summary>
    /// Motor KDE + análisis estadístico + potencial cuántico Bohmiano.
    /// </summary>
    public sealed class KdeCalculator : IKdeCalculator
    {
        private const double MetersPerDeg = 111_320.0;   // 1° latitud ≈ 111.32 km
                                                         // Piso de disponibilidad: por debajo no se devuelve destino.
                                                         // Calibrado para que en ruido puro ambas busquedas devuelvan ~100% (1 intento).
        /// <summary>
        /// Umbral del atractor sobre ZScore. El score de FindMaxAttractor es Z, no QZ.
        /// Calibrado para ~98% de disponibilidad (1.03 intentos) en disco uniforme, N=1024, G=120.
        /// </summary>
        private const double ZAttractorThreshold = 1.4;

        /// <summary>
        /// Umbral del vacio sobre |QZScore|. Calibrado para ~95% de disponibilidad
        /// (1.05 intentos) en disco uniforme, N=1024, G=120. Recalibrar si cambian N o G.
        /// </summary>
        private const double QzVoidThreshold = 1.5;


        /// <summary>
        /// Radio de escala interna del kernel: h = RadioEscalaFactor * R.
        /// Referencia del acuerdo de diseño; el ancho de banda lo fija el llamador
        /// al construir el KDE. No participa en el radio mostrado al usuario.
        /// </summary>
        private const double RadioEscalaFactor = 0.1575;

        /// <summary>
        /// P95 medidos en ruido uniforme (N=1024, G=100) para normalizar scores.
        /// </summary>
        private const double ZP95 = 2.45;
        private const double QzP95 = 3.20;
        private const double PowerP95 = 2.35;

        #region 📈 KDE

        public double CalculateSilvermanBandwidth(int n)
        {
            const double d = 2.0;
            if (n <= 0) return 0;
            return Math.Pow(4.0 / (d + 2.0), 1.0 / (d + 4.0))
                 * Math.Pow(n, -1.0 / (d + 4.0));
        }

        public (double lat, double lon, double density)[] CalculateKdeOptimized(
            List<(double lat, double lon)> coordList,
            double? bandwidth = null,
            int gridSize = 100)
        {
            int n = coordList.Count;
            if (n == 0) return Array.Empty<(double, double, double)>();
            if (gridSize < 2) gridSize = 2;

            const double epsilon = 1e-8;

            double[] xData = new double[n];
            double[] yData = new double[n];
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            for (int i = 0; i < n; i++)
            {
                var (lat, lon) = coordList[i];
                xData[i] = lat;
                yData[i] = lon;
                if (lat < minX) minX = lat;
                if (lat > maxX) maxX = lat;
                if (lon < minY) minY = lon;
                if (lon > maxY) maxY = lon;
            }

            double meanX = xData.Average();
            double meanY = yData.Average();

            double covXX = 0, covXY = 0, covYY = 0;
            for (int i = 0; i < n; i++)
            {
                double dx = xData[i] - meanX;
                double dy = yData[i] - meanY;
                covXX += dx * dx;
                covXY += dx * dy;
                covYY += dy * dy;
            }
            if (n > 1)
            {
                covXX /= (n - 1);
                covXY /= (n - 1);
                covYY /= (n - 1);
            }

            double detCov = covXX * covYY - covXY * covXY;
            if (Math.Abs(detCov) < epsilon)
            {
                covXX += epsilon;
                covYY += epsilon;
                detCov = covXX * covYY - covXY * covXY;
            }

            bandwidth ??= CalculateSilvermanBandwidth(n);

            covXX *= bandwidth.Value * bandwidth.Value;
            covYY *= bandwidth.Value * bandwidth.Value;
            covXY *= bandwidth.Value * bandwidth.Value;

            detCov = covXX * covYY - covXY * covXY;
            double invDetCov = 1.0 / detCov;
            double invH11 = covYY * invDetCov;
            double invH12 = -covXY * invDetCov;
            double invH22 = covXX * invDetCov;

            double normFactor = 1.0 / (n * 2 * Math.PI * Math.Sqrt(detCov));

            double xStep = (maxX - minX) / (gridSize - 1);
            double yStep = (maxY - minY) / (gridSize - 1);

            var densityGrid = new (double lat, double lon, double density)[gridSize * gridSize];

            Parallel.For(0, gridSize, i =>
            {
                double x = minX + i * xStep;
                int rowIndex = i * gridSize;
                for (int j = 0; j < gridSize; j++)
                {
                    double y = minY + j * yStep;
                    double total = 0.0;
                    for (int k = 0; k < n; k++)
                    {
                        double dx = x - xData[k];
                        double dy = y - yData[k];
                        double exponent = dx * dx * invH11
                                        + 2 * dx * dy * invH12
                                        + dy * dy * invH22;
                        total += Math.Exp(-0.5 * exponent);
                    }
                    densityGrid[rowIndex + j] = (x, y, normFactor * total);
                }
            });

            return densityGrid;
        }

        public int GetGridSize(double radioMetros)
        {
            return 100;
        }

        #endregion

        #region 🧠 Estadística

        public Anomalia[] CalculaEstadisticas(
            Fatum fatum,
            (double lat, double lon, double density)[] densityGrid)
        {
            double radioMax = fatum.Radio;
            int total = densityGrid.Length;
            int gridSize = (int)Math.Sqrt(total);

            if (gridSize * gridSize != total)
                throw new InvalidOperationException("El densityGrid no es un grid cuadrado.");

            if (gridSize < 3)
                return Array.Empty<Anomalia>();

            double minLat = double.MaxValue, maxLat = double.MinValue;
            double minLon = double.MaxValue, maxLon = double.MinValue;

            for (int k = 0; k < total; k++)
            {
                var p = densityGrid[k];
                if (p.lat < minLat) minLat = p.lat;
                if (p.lat > maxLat) maxLat = p.lat;
                if (p.lon < minLon) minLon = p.lon;
                if (p.lon > maxLon) maxLon = p.lon;
            }

            double xStepDeg = (maxLat - minLat) / (gridSize - 1);
            double yStepDeg = (maxLon - minLon) / (gridSize - 1);

            double[,] Q = ComputeQuantumPotentialRobust(
                densityGrid, xStepDeg, yStepDeg);

            var dentro = new bool[total];

            for (int idx = 0; idx < total; idx++)
            {
                var p = densityGrid[idx];
                dentro[idx] = FatumHelps.GetDistance(
                    fatum.Latitude,
                    fatum.Longitude,
                    p.lat,
                    p.lon) <= radioMax;
            }

            var temp = new List<DensityCell>();

            for (int idx = 0; idx < total; idx++)
            {
                int i = idx / gridSize;
                int j = idx % gridSize;

                if (i == 0 || j == 0 || i == gridSize - 1 || j == gridSize - 1)
                    continue;

                if (!dentro[idx])
                    continue;

                // El centro y sus cuatro vecinos deben quedar dentro del fatum.
                if (!dentro[idx - gridSize] ||
                    !dentro[idx + gridSize] ||
                    !dentro[idx - 1] ||
                    !dentro[idx + 1])
                    continue;

                var p = densityGrid[idx];
                double dist = FatumHelps.GetDistance(
                    fatum.Latitude,
                    fatum.Longitude,
                    p.lat,
                    p.lon);

                temp.Add(new DensityCell(
                    p.lat, p.lon, p.density, Q[i, j], dist));
            }

            int n = temp.Count;
            if (n == 0)
                return Array.Empty<Anomalia>();

            var valuesDensity = temp.Select(x => x.Density).ToList();
            double mediaDensity = valuesDensity.Average();
            double sigmaDensity = Math.Sqrt(
                valuesDensity.Select(v => Math.Pow(v - mediaDensity, 2)).Average());

            var valuesQ = temp.Select(x => x.QuantumPotential).ToList();
            double mediaQ = valuesQ.Average();
            double sigmaQ = Math.Sqrt(
                valuesQ.Select(v => Math.Pow(v - mediaQ, 2)).Average());

            var resultado = new Anomalia[n];

            for (int k = 0; k < n; k++)
            {
                var c = temp[k];

                double z = sigmaDensity > 0
                    ? (c.Density - mediaDensity) / sigmaDensity
                    : 0;

                double qz = sigmaQ > 0
                    ? (c.QuantumPotential - mediaQ) / sigmaQ
                    : 0;

                resultado[k] = new Anomalia
                {
                    Latitude = c.Lat,
                    Longitude = c.Lon,
                    DensidadEstimacion = c.Density,
                    ZScore = z,
                    QZScore = qz,
                    ZScoreFinal = CalculaZScoreFinal(z, qz),
                    QuantumPotential = c.QuantumPotential,
                    TipoAnomalia = z < 0
                        ? EnumAnomalyType.Void
                        : EnumAnomalyType.Attractor,
                    Distancia = c.Distance
                };
            }

            return resultado;
        }


        #endregion

        #region 🎯 Selección


        public Anomalia FindMaxAttractor(Fatum fatum, IList<Anomalia> anomalias)
        {
            Anomalia best = null;
            double maxScore = double.MinValue;

            foreach (var a in anomalias)
            {
                if (a.ZScore < ZAttractorThreshold) continue;   // antes: <= 0.05
                if (a.QZScore <= 0) continue;                   // puerta de coherencia, se mantiene
                if (a.Distancia > fatum.Radio) continue;

                double score = a.ZScore;
                if (score > maxScore) { maxScore = score; best = a; }
            }
            return best;
        }

        public Anomalia FindMaxVoid(Fatum fatum, IList<Anomalia> anomalias)
        {
            Anomalia best = null;
            double maxScore = double.MinValue;

            foreach (var a in anomalias)
            {
                if (a.ZScore >= -0.05) continue;
                if (a.QZScore >= 0) continue;
                if (a.Distancia > fatum.Radio) continue;

                double score = Math.Abs(a.QZScore);
                if (score < QzVoidThreshold) continue;          // antes: 3.0
                if (score > maxScore) { maxScore = score; best = a; }
            }
            return best;
        }

        public Anomalia GetMaxAttractor(
            Fatum fatum,
            IList<(double lat, double lon)> coordList,
            (double lat, double lon, double density)[] densityGrid)
        {
            // coordList ya no se usa: el radio aproximado es fijo y el power sale
            // de Z y QZ. Se conserva la firma por compatibilidad con IKdeCalculator.
            var anomalias = CalculaEstadisticas(fatum, densityGrid);
            var result = FindMaxAttractor(fatum, anomalias);

            return Finalize(result, coordList);
        }

        public Anomalia GetMaxVoid(
            Fatum fatum,
            IList<(double lat, double lon)> coordList,
            (double lat, double lon, double density)[] densityGrid)
        {
            var anomalias = CalculaEstadisticas(fatum, densityGrid);
            var result = FindMaxVoid(fatum, anomalias);

            return Finalize(result, coordList);
        }

        public Anomalia GetMaxAnomalia(
            Fatum fatum,
            IList<(double lat, double lon)> coordList,
            (double lat, double lon, double density)[] densityGrid)
        {
            var anomalias = CalculaEstadisticas(fatum, densityGrid);

            var atractor = FindMaxAttractor(fatum, anomalias);
            var vacio = FindMaxVoid(fatum, anomalias);

            if (atractor == null) return Finalize(vacio, coordList);
            if (vacio == null) return Finalize(atractor, coordList);

            Finalize(atractor, coordList);
            Finalize(vacio, coordList);

            // Selección entre tipos SOLO por score calibrado: cada tipo
            // normalizado por su propio p95. Power es simétrica (50/50 en
            // ruido uniforme) y no sirve para comparar tipos.
            return ScoreCalibrado(atractor) >= ScoreCalibrado(vacio)
                ? atractor
                : vacio;
        }

        private Anomalia Finalize(Anomalia a, IList<(double lat, double lon)> coordList)
        {
            if (a == null) return null;

            // Radio de presentación fijo: 100 m siempre, sin importar el radio
            // de búsqueda. La escala interna del kernel sigue siendo 0.1575·R.
            a.RadioAproximado = FatumHelps.CalculaRadioAproximado(a.Latitude,a.Longitude,coordList);
            a.Power = CalcularPower(a.ZScore, a.QZScore);

            return a;
        }

        public Anomalia GetFatum(
            Fatum fatum,
            List<(double lat, double lon)> coordList,
            (double lat, double lon, double density)[] densityGrid,
            EnumAnomalyType anomalyType,
            double? bandwidth = null,
            int gridSize = 100)
        {
            if (anomalyType == EnumAnomalyType.Void)
                return GetMaxVoid(fatum, coordList, densityGrid);
            if (anomalyType == EnumAnomalyType.Attractor)
                return GetMaxAttractor(fatum, coordList, densityGrid);
            throw new ArgumentException("Invalid anomaly type");
        }

        public Anomalia GetFatum(
            Fatum fatum,
            List<(double lat, double lon)> coordList,
            EnumAnomalyType anomalyType,
            double? bandwidth = null,
            int gridSize = 100)
        {
            var grid = CalculateKdeOptimized(coordList, bandwidth, gridSize);
            return GetFatum(fatum, coordList, grid, anomalyType, bandwidth, gridSize);
        }

        #endregion

        #region 🔧 Helpers

        /// <summary>
        /// Power = |Z·QZ| normalizado por su p95 (2.35) medido en ruido uniforme.
        /// Solo para magnitud y ranking dentro de un mismo tipo; es simétrica
        /// entre atractor y vacío, así que NO debe usarse para elegir entre tipos.
        /// </summary>
        public static double CalcularPower(double z, double qz)
            => Math.Abs(z * qz) / PowerP95;

        /// <summary>
        /// Score calibrado para comparar tipos: atractor → Z/ZP95,
        /// vacío → |QZ|/QzP95. Cada tipo contra su propio p95.
        /// </summary>
        public static double ScoreCalibrado(Anomalia a)
            => a.TipoAnomalia == EnumAnomalyType.Void
                ? Math.Abs(a.QZScore) / QzP95
                : a.ZScore / ZP95;

        /// <summary>
        /// Combina Z y QZ con signo. Si z = 0, hereda el signo de qz
        /// para no perder la información del potencial cuántico.
        /// </summary>
        public double CalculaZScoreFinal(double z, double qz)
        {
            const double alpha = 0.6;
            double sign = Math.Sign(z);
            if (sign == 0) sign = Math.Sign(qz);
            if (sign == 0) return 0;
            return sign * Math.Sqrt(z * z + alpha * qz * qz);
        }

        /// <summary>
        /// Potencial cuántico de Bohm — unidades SI: 1/m².
        /// Laplaciano con métrica esférica local (cos(lat) en el eje longitud).
        /// </summary>
        private static double[,] ComputeQuantumPotentialRobust(
            (double lat, double lon, double density)[] densityGrid,
            double xStepDeg,
            double yStepDeg)
        {
            int total = densityGrid.Length;
            int gridSize = (int)Math.Sqrt(total);
            if (gridSize * gridSize != total)
                throw new InvalidOperationException("densityGrid no es un grid cuadrado.");
            if (gridSize < 3 || xStepDeg <= 0 || yStepDeg <= 0)
                return new double[Math.Max(gridSize, 0), Math.Max(gridSize, 0)];

            int nx = gridSize, ny = gridSize;
            double[,] Q = new double[nx, ny];
            double[,] R = new double[nx, ny];

            const double rhoFloor = 1e-10;   // R_min = sqrt(1e-10) = 1e-5 → división segura

            // 1️⃣ R = sqrt(ρ)  (amplitud, sin unidades)
            Parallel.For(0, nx, i =>
            {
                int rowOffset = i * gridSize;
                for (int j = 0; j < ny; j++)
                {
                    double rho = densityGrid[rowOffset + j].density;
                    if (rho < rhoFloor) rho = rhoFloor;
                    R[i, j] = Math.Sqrt(rho);
                }
            });

            // 2️⃣ Laplaciano (metros) con anisotropía lat/lon
            Parallel.For(1, nx - 1, i =>
            {
                double latDeg = densityGrid[i * gridSize].lat;
                double cosLat = Math.Cos(latDeg * Math.PI / 180.0);
                if (cosLat < 1e-6) cosLat = 1e-6;   // protección en los polos

                double xStepM = xStepDeg * MetersPerDeg;
                double yStepM = yStepDeg * MetersPerDeg * cosLat;

                double invXStep2 = 1.0 / (xStepM * xStepM);
                double invYStep2 = 1.0 / (yStepM * yStepM);

                for (int j = 1; j < ny - 1; j++)
                {
                    double center = R[i, j];
                    double lap =
                        (R[i + 1, j] - 2.0 * center + R[i - 1, j]) * invXStep2 +
                        (R[i, j + 1] - 2.0 * center + R[i, j - 1]) * invYStep2;

                    Q[i, j] = -lap / center;    // 1/m²
                }
            });

            return Q;
        }

        #endregion
    }
}
