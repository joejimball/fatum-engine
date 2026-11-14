using System;
using System.Collections.Generic;

namespace FatumCommon.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Devuelve pares de elementos consecutivos no solapados.
        /// Para [1, 2, 3, 4] produce (1, 2), (3, 4).
        /// </summary>
        public static IEnumerable<(T First, T Second)> Pairwise<T>(this IEnumerable<T> source)
            => Pairwise(source, false);

        /// <summary>
        /// Igual que Pairwise, pero si <paramref name="throwOnOdd"/> es true lanza
        /// excepcion cuando el numero de elementos es impar, en lugar de descartar
        /// el ultimo en silencio.
        /// </summary>
        public static IEnumerable<(T First, T Second)> Pairwise<T>(
            this IEnumerable<T> source,
            bool throwOnOdd)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using IEnumerator<T> enumerator = source.GetEnumerator();

            while (enumerator.MoveNext())
            {
                T first = enumerator.Current;

                if (enumerator.MoveNext())
                {
                    yield return (first, enumerator.Current);
                }
                else
                {
                    if (throwOnOdd)
                        throw new ArgumentException(
                            "El numero de elementos es impar: el ultimo no tiene par.",
                            nameof(source));

                    yield break;
                }
            }
        }
    }
}
