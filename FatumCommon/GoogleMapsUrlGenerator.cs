using System.Globalization;

namespace FatumCommon
{
    public class GoogleMapsUrlGenerator
    {
        public static string GenerateUrl(double latitude, double longitude)
        {
            return $"https://www.google.com/maps?q={FormatCoordinate(latitude)},{FormatCoordinate(longitude)}";
        }

        private static string FormatCoordinate(double coordinate)
        {
            // Asegura formato con punto decimal y 6 decimales de precisión
            return coordinate.ToString("F6", CultureInfo.InvariantCulture);
        }
    }
}