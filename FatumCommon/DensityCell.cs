namespace FatumCommon
{
    // ============================================
    // CLASES AUXILIARES AMPLIADAS
    // ============================================

    public struct DensityCell
    {
        public double Lat;
        public double Lon;
        public double Density;
        public double QuantumPotential;
        public double Distance;

        public DensityCell(double lat, double lon, double density, double quantumPotential, double distance)
        {
            this.Lat = lat;
            this.Lon = lon;
            this.Density = density;
            this.QuantumPotential = quantumPotential;
            this.Distance = distance;
        }
    }
}
