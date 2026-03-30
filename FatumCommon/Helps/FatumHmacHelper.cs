using FatumCommon.Domain;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FatumCommon.Helps;

/// <summary>
/// Integridad del <see cref="Fatum.RandomData"/>: HMAC-SHA256 con clave derivada de parámetros del experimento.
/// </summary>
public static class FatumHmacHelper
{
    /// <summary>
    /// Construye la clave HMAC a partir de latitud, longitud, radio, número de puntos, tipo de RNG y fecha de creación.
    /// </summary>
    public static byte[] BuildKeyBytes(Fatum fatum)
    {
        var inv = CultureInfo.InvariantCulture;
        var created = fatum.CreatedAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(fatum.CreatedAt, DateTimeKind.Utc)
            : fatum.CreatedAt.ToUniversalTime();
        var keyMaterial = string.Join("|",
            fatum.Latitude.ToString("G17", inv),
            fatum.Longitude.ToString("G17", inv),
            fatum.Radio.ToString("G17", inv),
            fatum.NumPuntos.ToString(inv),
            ((int)fatum.TypeRnd).ToString(inv),
            created.ToString("o", inv));
        return Encoding.UTF8.GetBytes(keyMaterial);
    }

    /// <summary>
    /// Calcula HMAC-SHA256(RandomData) en hexadecimal (64 caracteres).
    /// </summary>
    public static string ComputeHmacSha256Hex(Fatum fatum)
    {
        var key = BuildKeyBytes(fatum);
        var data = fatum.RandomData ?? Array.Empty<byte>();
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(data));
    }

    /// <summary>
    /// Comprueba si <see cref="Fatum.Hmac"/> coincide con un cálculo actual (comparación a tiempo constante en los bytes del hash).
    /// </summary>
    public static bool Verify(Fatum fatum)
    {
        if (string.IsNullOrWhiteSpace(fatum.Hmac))
            return false;
        byte[] expected;
        try
        {
            expected = Convert.FromHexString(ComputeHmacSha256Hex(fatum));
        }
        catch
        {
            return false;
        }

        byte[] actual;
        try
        {
            actual = Convert.FromHexString(fatum.Hmac.Trim());
        }
        catch
        {
            return false;
        }

        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
