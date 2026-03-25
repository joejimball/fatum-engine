using Dapper.Contrib.Extensions;
using FatumCommon.Enums;
using System;

namespace FatumCommon.Domain
{
    [Serializable]
    [Table("anomalias")]
    public class Anomalia
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
        public double RadioAproximado { get; set; }
        public EnumAnomalyType TipoAnomalia { get; set; }
        public long IdFatum { get; set; }
        public double Distancia { get; set; }
        public double QuantumPotential { get; set; }
        public double QZScore { get; set; }
        public double ZScoreFinal { get; set; }

        public (AnomalyClassification, string) ClasificarAnomalia()
        {
            int distanciaMetros = (int)Math.Round(this.Distancia);
            int radioAproximadoMetros = (int)Math.Round(this.RadioAproximado);

            AnomalyClassification clasificacion;
            string tipo = this.TipoAnomalia == EnumAnomalyType.Attractor ? "Atracción" : "Vacío";
            string intensidad = "";
            string descripcion = "";
            string emoji = "";
            string hallazgo = "";  // Nueva: descripción de posibles hallazgos

            if (this.Power < 2)
            {
                clasificacion = AnomalyClassification.Neutra;
                intensidad = "neutra";
                descripcion = "No se detectó una anomalía significativa";
                emoji = "🌫️";
                hallazgo = "Un lugar común sin características destacables";
            }
            else if (this.Power < 4)
            {
                clasificacion = AnomalyClassification.Debil;
                intensidad = "débil";
                descripcion = "Se observa una señal sutil";
                emoji = this.TipoAnomalia == EnumAnomalyType.Attractor ? "🌱" : "🌫️";
                hallazgo = this.TipoAnomalia == EnumAnomalyType.Attractor
                    ? "Objetos pequeños, coincidencias curiosas o señales discretas"
                    : "Espacios tranquilos, lugares ligeramente aislados";
            }
            else if (this.Power < 6)
            {
                clasificacion = AnomalyClassification.Moderada;
                intensidad = "moderada";
                descripcion = "Se detecta una anomalía notable";
                emoji = this.TipoAnomalia == EnumAnomalyType.Attractor ? "🔍" : "🌀";
                hallazgo = this.TipoAnomalia == EnumAnomalyType.Attractor
                    ? "Objetos perdidos, arte callejero interesante, pequeños tesoros"
                    : "Áreas silenciosas, lugares con poca actividad humana";
            }
            else if (this.Power < 8)
            {
                clasificacion = AnomalyClassification.Fuerte;
                intensidad = "fuerte";
                descripcion = "¡Una anomalía poderosa detectada!";
                emoji = this.TipoAnomalia == EnumAnomalyType.Attractor ? "✨" : "🌌";
                hallazgo = this.TipoAnomalia == EnumAnomalyType.Attractor
                    ? "Lugares con alta actividad, eventos inusuales, objetos valiosos"
                    : "Zonas abandonadas, espacios naturales aislados, lugares con energía tranquila";
            }
            else
            {
                clasificacion = AnomalyClassification.Extrema;
                intensidad = "extrema";
                descripcion = "¡ALERTA! Anomalía de intensidad excepcional";
                emoji = this.TipoAnomalia == EnumAnomalyType.Attractor ? "⚡" : "🕳️";
                hallazgo = this.TipoAnomalia == EnumAnomalyType.Attractor
                    ? "Puntos de encuentro significativos, arte urbano destacado, objetos con historia personal"
                    : "Vacíos profundos, lugares con historia misteriosa, espacios con energía única";
            }

            // Mensaje descriptivo completo
            string mensaje = $"{emoji} *{tipo} {intensidad.ToUpper()}*\n" +
                             $"{descripcion}\n\n" +
                             $"🔍 **Qué podrías encontrar:**\n" +
                             $"{hallazgo}\n\n" +
                             $"⚡ **Power:** {this.Power:F2}\n" +
                             $" Z **Z-score:** {this.ZScore:F2}\n" +
                             $"📍 **Distancia:** {distanciaMetros}m\n" +
                             $"📍 **Radio aproximada:** {radioAproximadoMetros}m\n";

            // Sugerencia basada en la intensidad
            string consejo = "";
            if (clasificacion >= AnomalyClassification.Fuerte)
            {
                consejo = this.TipoAnomalia == EnumAnomalyType.Attractor
                    ? "⚠️ ¡Máxima atención! Podrías encontrar algo significativo"
                    : "⚠️ Observa con cuidado, los vacíos extremos a menudo guardan secretos";
            }
            else if (clasificacion >= AnomalyClassification.Moderada)
            {
                consejo = this.TipoAnomalia == EnumAnomalyType.Attractor
                    ? "ℹ️ Explora con detenimiento, busca objetos inusuales"
                    : "ℹ️ Disfruta de la tranquilidad, observa tu entorno con paciencia";
            }

            if (!string.IsNullOrEmpty(consejo))
            {
                mensaje += $"\n\n{consejo}";
            }

            return (clasificacion, mensaje);
        }

    }
}
