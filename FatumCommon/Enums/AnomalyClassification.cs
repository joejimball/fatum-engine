namespace FatumCommon.Enums
{
    // Enums necesarios
    public enum AnomalyClassification
    {
        Neutra,     // Power < 0.5
        Debil,      // 0.5 ≤ Power < 1.5
        Moderada,   // 1.5 ≤ Power < 2.5
        Fuerte,     // 2.5 ≤ Power < 4.0
        Extrema     // Power ≥ 4.0
    }
}
