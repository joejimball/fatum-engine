using Google.OpenLocationCode;
using System.Text;

namespace FatumCommon.Helps
{
    public static class LocationCodesHelps
    {
        /// <summary>
        /// Open Location Code (Plus Codes).
        /// </summary>
        public static string GetOpenLocationCode(double latitude, double longitude, int codeLength = OpenLocationCode.CodePrecisionNormal)
        {
            // La librería permite codificar y decodificar offline.
            return OpenLocationCode.Encode(latitude, longitude, codeLength);
        }

        /// <summary>
        /// Geohash (Precision en caracteres, por defecto 9).
        /// </summary>
        public static string GetGeoHash(double latitude, double longitude, int precision = 9)
        {
            if (precision < 1) precision = 1;

            const string base32 = "0123456789bcdefghjkmnpqrstuvwxyz";

            double[] latRange = { -90.0, 90.0 };
            double[] lonRange = { -180.0, 180.0 };

            var hash = new StringBuilder(precision);
            bool isEven = true;

            int bit = 0;
            int ch = 0;

            while (hash.Length < precision)
            {
                double mid;
                if (isEven)
                {
                    mid = (lonRange[0] + lonRange[1]) / 2.0;
                    if (longitude > mid)
                    {
                        ch = (ch << 1) + 1;
                        lonRange[0] = mid;
                    }
                    else
                    {
                        ch = ch << 1;
                        lonRange[1] = mid;
                    }
                }
                else
                {
                    mid = (latRange[0] + latRange[1]) / 2.0;
                    if (latitude > mid)
                    {
                        ch = (ch << 1) + 1;
                        latRange[0] = mid;
                    }
                    else
                    {
                        ch = ch << 1;
                        latRange[1] = mid;
                    }
                }

                isEven = !isEven;
                bit++;

                if (bit == 5)
                {
                    hash.Append(base32[ch]);
                    bit = 0;
                    ch = 0;
                }
            }

            return hash.ToString();
        }
    }
}

