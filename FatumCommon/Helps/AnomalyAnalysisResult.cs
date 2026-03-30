using FatumCommon.Enums;

namespace FatumCommon.Helps
{
    /// <summary>
    /// Clase extendida para soportar ambos tipos de anomalías
    /// </summary>
    public class AnomalyAnalysisResult
    {
        /// <summary>
        /// 0 - distancia más cercana
        /// </summary>
        public double DistanciaMasCercana { get; set; }

        /// <summary>
        /// 1 - radio aproximado
        /// </summary>
        public double RadioAproximado { get; set; }

        /// <summary>
        /// 2 - densidad relativa, >1 = atractor, <1 = vacío
        /// </summary>
        public double DensidadRelativa { get; set; }
        public double IntensidadAnomalia { get; set; } // = DensidadRelativa (atractores) o 1/DensidadRelativa (vacíos)
    }
}
