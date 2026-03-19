using FatumCommon.Domain;
using FatumCommon.Enums;
using System;
using System.Collections.Generic;

namespace FatumCommon
{
    /// <summary>
    /// Define el contrato para el cálculo de KDE,
    /// análisis estadístico y detección de anomalías
    /// tipo Atractor y Void.
    /// </summary>
    public interface IKdeCalculator
    {
        #region 📈 KDE

        /// <summary>
        /// Calcula el Kernel Density Estimation (KDE) gaussiano 2D.
        /// </summary>
        /// <param name="coordList">Lista de coordenadas de entrada</param>
        /// <param name="bandwidth">Bandwidth opcional (si es null usa Silverman)</param>
        /// <param name="gridSize">Resolución del grid</param>
        /// <returns>Grid lineal con lat, lon y densidad</returns>
        (double lat, double lon, double density)[] CalculateKdeOptimized(List<(double lat, double lon)> coordList, double? bandwidth = null, int gridSize = 100);

        /// <summary>
        /// Devuelve el el tamaño del grid para el kde
        /// </summary>
        /// <param name="radioMetros"></param>
        /// <returns></returns>
        int GetGridSize(double radioMetros);


        /// <summary>
        /// Calcula el bandwidth multivariante de Silverman (misma fórmula que usa KdeCalculator cuando bandwidth=null),
        /// para poder persistirlo y reproducir el KDE.
        /// </summary>
        double CalculateSilvermanBandwidth(int n);


        #endregion

        #region 🧠 Estadística

        /// <summary>
        /// Calcula Z-Score y clasificación de anomalías
        /// dentro del radio definido en Fatum.
        /// </summary>
        Anomalia[] CalculaEstadisticas(
            Fatum fatum,
            (double lat, double lon, double density)[] densityGrid);

        #endregion

        #region 🎯 Selección de anomalías

        /// <summary>
        /// Encuentra el atractor más fuerte.
        /// </summary>
        Anomalia FindMaxAttractor(
            Fatum fatum,
            IList<Anomalia> anomalias);

        /// <summary>
        /// Encuentra el vacío más fuerte.
        /// </summary>
        Anomalia FindMaxVoid(
            Fatum fatum,
            IList<Anomalia> anomalias);

        /// <summary>
        /// Devuelve el atractor más potente.
        /// </summary>
        Anomalia GetMaxAttractor(
            Fatum fatum,
            IList<(double lat, double lon)> coordList,
            (double lat, double lon, double density)[] densityGrid);

        /// <summary>
        /// Devuelve el vacío más potente.
        /// </summary>
        Anomalia GetMaxVoid(
            Fatum fatum,
            IList<(double lat, double lon)> coordList,
            (double lat, double lon, double density)[] densityGrid);

        /// <summary>
        /// Devuelve la anomalía más potente
        /// (comparando atractor vs vacío).
        /// </summary>
        Anomalia GetMaxAnomalia(
            Fatum fatum,
            IList<(double lat, double lon)> coordList,
            (double lat, double lon, double density)[] densityGrid);

        /// <summary>
        /// Devuelve anomalía según tipo especificado
        /// usando grid ya calculado.
        /// </summary>
        Anomalia GetFatum(
            Fatum fatum,
            List<(double lat, double lon)> coordList,
            (double lat, double lon, double density)[] densityGrid,
            EnumAnomalyType anomalyType, double? bandwidth = null, int gridSize = 100);

        /// <summary>
        /// Devuelve anomalía según tipo especificado
        /// calculando internamente el KDE.
        /// </summary>
        Anomalia GetFatum(
            Fatum fatum,
            List<(double lat, double lon)> coordList,
            EnumAnomalyType anomalyType, double? bandwidth = null, int gridSize = 100);

        #endregion
    }
}
