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
        private const double Epsilon = 1e-12;
        private const double SigmaThreshold = 3.5;

        #region 📈 KDE

        public double CalculateSilvermanBandwidth(int n)
        {
            const double d = 2.0;
            if (n <= 0) return 0;
            return Math.Pow(4.0 / (d + 2.0), 1.0 / (d + 4.0)) * Math.Pow(n, -1.0 / (d + 4.0));
        }


        public (double lat, double lon, double density)[] CalculateKdeOptimized(List<(double lat, double lon)> coordList, double? bandwidth = null, int gridSize = 100)
        {
            int n = coordList.Count;
            if (n == 0) return Array.Empty<(double, double, double)>();

            const double d = 2.0;
            const double epsilon = 1e-8;

            // Extraer coordenadas y rangos
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

            // Media
            double meanX = xData.Average();
            double meanY = yData.Average();

            // Covarianza
            double covXX = 0, covXY = 0, covYY = 0;
            for (int i = 0; i < n; i++)
            {
                double dx = xData[i] - meanX;
                double dy = yData[i] - meanY;
                covXX += dx * dx;
                covXY += dx * dy;
                covYY += dy * dy;
            }
            covXX /= (n - 1);
            covXY /= (n - 1);
            covYY /= (n - 1);

            double detCov = covXX * covYY - covXY * covXY;
            if (Math.Abs(detCov) < epsilon)
            {
                covXX += epsilon;
                covYY += epsilon;
                detCov = covXX * covYY - covXY * covXY;
            }

            if (bandwidth == null)
            {
                // Bandwidth Silverman multivariante
                bandwidth = CalculateSilvermanBandwidth(n);
            }

            covXX *= bandwidth.Value * bandwidth.Value;
            covYY *= bandwidth.Value * bandwidth.Value;
            covXY *= bandwidth.Value * bandwidth.Value;

            // Matriz H e inversa
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
                        double exponent = dx * dx * invH11 + 2 * dx * dy * invH12 + dy * dy * invH22;
                        total += Math.Exp(-0.5 * exponent);
                    }

                    densityGrid[rowIndex + j] = (x, y, normFactor * total);
                }
            });

            return densityGrid;
        }


        public int GetGridSize(double radioMetros)
        {
            if (radioMetros <= 3000) return 120;
            else if (radioMetros <= 6000) return 160;
            else if (radioMetros <= 10000) return 200;
            else return 300;
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

            // ─────────────────────────────
            // 1️⃣ Obtener límites en una sola pasada
            // ─────────────────────────────
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

            double xStep = (maxLat - minLat) / (gridSize - 1);
            double yStep = (maxLon - minLon) / (gridSize - 1);

            // ─────────────────────────────
            // 2️⃣ Potencial cuántico
            // ─────────────────────────────
            double[,] Q = ComputeQuantumPotentialRobust(densityGrid, xStep, yStep);

            // ─────────────────────────────
            // 3️⃣ Filtrar + acumular estadística en una sola pasada
            // ─────────────────────────────
            var temp = new List<DensityCell>();

            for (int idx = 0; idx < total; idx++)
            {
                int i = idx / gridSize;
                int j = idx % gridSize;

                if (i == 0 || j == 0 || i == gridSize - 1 || j == gridSize - 1)
                    continue;

                var p = densityGrid[idx];

                double dist = FatumHelps.GetDistance(
                    fatum.Latitude, fatum.Longitude,
                    p.lat, p.lon);

                if(dist <= radioMax)temp.Add(new DensityCell(p.lat, p.lon, p.density, Q[i, j], dist));
            }

            int n = temp.Count;
            if (n == 0)
                return Array.Empty<Anomalia>();

            // ─────────────────────────────
            // 4️⃣ Estadística POBLACIONAL
            // ─────────────────────────────
            var valuesZ = temp.Select(x => x.Density).ToList();
            double mediaZ = valuesZ.Average();
            double desviacionZ = Math.Sqrt(valuesZ.Select(v => Math.Pow(v - mediaZ, 2)).Average());

            var valuesQ = temp.Select(x => x.QuantumPotential).ToList();
            double mediaQ = valuesQ.Average();
            double desviacionQ = Math.Sqrt(valuesQ.Select(v => Math.Pow(v - mediaQ, 2)).Average());

            // ─────────────────────────────
            // 5️⃣ Construcción final
            // ─────────────────────────────
            var resultado = new Anomalia[n];

            for (int k = 0; k < n; k++)
            {
                var c = temp[k];

                double z = desviacionZ > 0
                    ? (c.Density - mediaZ) / desviacionZ
                    : 0;

                double qz = desviacionQ > 0
                    ? (c.QuantumPotential - mediaQ) / desviacionQ
                    : 0;

                resultado[k] = new Anomalia
                {
                    Latitude = c.Lat,
                    Longitude = c.Lon,
                    DensidadEstimacion = c.Density,
                    ZScore = z,
                    QZScore = qz,
                    ZScoreFinal = CalculaZScoreFinal(z,qz),
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
                if (a.ZScore <= 0) continue;
                if (a.QZScore <= 0) continue;
                if (a.Distancia > fatum.Radio) continue;
                double score = Math.Max(0, a.ZScore);

                if (score > maxScore)
                {
                    maxScore = score;
                    best = a;
                }
            }

            return best;
        }

        public Anomalia FindMaxVoid(Fatum fatum, IList<Anomalia> anomalias)
        {
            Anomalia best = null;
            double maxScore = double.MinValue;

            foreach (var a in anomalias)
            {
                if (a.ZScore >= 0) continue;
                if (a.QZScore >= -0.5) continue;
                if (a.Distancia > fatum.Radio) continue;

                double score = Math.Abs(Math.Min(0, a.QZScore));
                double bordeFactor = 1.0 - (a.Distancia / fatum.Radio);
                score *= bordeFactor;

                if (score > maxScore)
                {
                    maxScore = score;
                    best = a;
                }
            }

            return best;
        }

        public Anomalia GetMaxAttractor(
            Fatum fatum,
            IList<(double lat, double lon)> coordList,
            (double lat, double lon, double density)[] densityGrid)
        {
            var anomalias = CalculaEstadisticas(fatum, densityGrid);
            var result = FindMaxAttractor(fatum, anomalias);

            if (result != null)
            {
                result.RadioAproximado = FatumHelps.CalculaRadioAproximado(fatum.Latitude, fatum.Longitude, coordList, result.TipoAnomalia);
                result.Power = CalcularPower(result);
            }

            return result;
        }

        public Anomalia GetMaxVoid(
            Fatum fatum,
            IList<(double lat, double lon)> coordList,
            (double lat, double lon, double density)[] densityGrid)
        {
            var anomalias = CalculaEstadisticas(fatum, densityGrid);
            var result = FindMaxVoid(fatum, anomalias);

            if (result != null)
            {
                result.RadioAproximado = FatumHelps.CalculaRadioAproximado(fatum.Latitude, fatum.Longitude, coordList, result.TipoAnomalia);
                result.Power = CalcularPower(result);
            }

            return result;
        }


        public Anomalia GetMaxAnomalia(
            Fatum fatum,
            IList<(double lat, double lon)> coordList,
            (double lat, double lon, double density)[] densityGrid)
        {
            var anomalias = CalculaEstadisticas(fatum, densityGrid);
            var atractor = FindMaxAttractor(fatum, anomalias);
            var vacio = FindMaxVoid(fatum, anomalias);

            if (atractor != null) atractor.Power = CalcularPower(atractor);
            if (vacio != null) vacio.Power = CalcularPower(vacio);

            if (atractor == null) return Finalize(fatum, coordList, vacio);
            if (vacio == null) return Finalize(fatum, coordList, atractor);

            // Compiten por Power — desempate por potencial cuántico absoluto
            Anomalia result;
            if (Math.Abs(atractor.Power - vacio.Power) < 0.01)
                result = Math.Abs(atractor.QuantumPotential) >= Math.Abs(vacio.QuantumPotential)
                    ? atractor : vacio;
            else
                result = atractor.Power >= vacio.Power ? atractor : vacio;

            return Finalize(fatum, coordList, result);
        }

        private Anomalia Finalize(
            Fatum fatum,
            IList<(double lat, double lon)> coordList,
            Anomalia a)
        {
            if (a == null) return null;
            a.RadioAproximado = FatumHelps.CalculaRadioAproximado(
                fatum.Latitude, fatum.Longitude, coordList, a.TipoAnomalia);
            return a;
        }

        public Anomalia GetFatum(
            Fatum fatum,
            List<(double lat, double lon)> coordList,
            (double lat, double lon, double density)[] densityGrid,
            EnumAnomalyType anomalyType, double? bandwidth = null, int gridSize = 100)
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
            EnumAnomalyType anomalyType, double? bandwidth = null, int gridSize = 100)
        {
            var grid = CalculateKdeOptimized(coordList, bandwidth, gridSize);
            return GetFatum(fatum, coordList, grid, anomalyType, bandwidth, gridSize);
        }

        #endregion

        private static double CalcularPower(Anomalia a)
        {
            double norm = Math.Abs(a.ZScoreFinal) / SigmaThreshold;
            if (norm > 1.0) norm = 1.0;

            return Math.Round(norm * 10.0, 2);
        }

        public double CalculaZScoreFinal(double z, double qz)
        {
            double alpha = 0.60;

            double Zfinal = Math.Sign(z) *
                Math.Sqrt(z * z + alpha * qz * qz);

            return Zfinal;
        }

        private static double[,] ComputeQuantumPotentialRobust(
            (double lat, double lon, double density)[] densityGrid,
            double xStep,
            double yStep)
        {
            int total = densityGrid.Length;
            int gridSize = (int)Math.Sqrt(total);

            if (gridSize * gridSize != total)
                throw new InvalidOperationException("densityGrid no es un grid cuadrado.");

            int nx = gridSize;
            int ny = gridSize;

            double[,] Q = new double[nx, ny];
            double[,] R = new double[nx, ny];

            const double rhoFloor = 1e-10;
            const double rFloor = 1e-12; // evita explosión numérica

            double invXStep2 = 1.0 / (xStep * xStep);
            double invYStep2 = 1.0 / (yStep * yStep);

            // ─────────────────────────────────────────────
            // 1️⃣ Construir R = sqrt(ρ)
            // ─────────────────────────────────────────────
            Parallel.For(0, nx, i =>
            {
                int rowOffset = i * gridSize;

                for (int j = 0; j < ny; j++)
                {
                    double rho = densityGrid[rowOffset + j].density;

                    if (rho < rhoFloor)
                        rho = rhoFloor;

                    R[i, j] = Math.Sqrt(rho);
                }
            });

            // ─────────────────────────────────────────────
            // 2️⃣ Calcular Q (SIN reflectivo artificial)
            //     Solo interior real (más estable)
            // ─────────────────────────────────────────────
            Parallel.For(1, nx - 1, i =>
            {
                for (int j = 1; j < ny - 1; j++)
                {
                    double center = R[i, j];

                    if (center < rFloor)
                    {
                        Q[i, j] = 0;
                        continue;
                    }

                    double lap =
                        (R[i + 1, j] - 2.0 * center + R[i - 1, j]) * invXStep2 +
                        (R[i, j + 1] - 2.0 * center + R[i, j - 1]) * invYStep2;

                    Q[i, j] = -lap / center;
                }
            });

            // Bordes = 0 (más estable que reflectivo)
            return Q;
        }
    }
}
