using System;
using System.Collections.Generic;

namespace FatumCommon.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Devuelve un iterable en pares de elementos consecutivos.
        /// Por ejemplo, para [1, 2, 3, 4], producirá (1, 2), (3, 4).
        /// </summary>
        /// <typeparam name="T">El tipo de los elementos en el iterable.</typeparam>
        /// <param name="source">El IEnumerable de origen.</param>
        /// <returns>Un IEnumerable de tuplas que contienen pares de elementos.</returns>
        public static IEnumerable<(T First, T Second)> Pairwise<T>(this IEnumerable<T> source)
        {
            // Si la fuente es nula, lanza una excepción.
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // Obtiene un enumerador de la fuente.
            using (IEnumerator<T> enumerator = source.GetEnumerator())
            {
                // Mientras haya un siguiente elemento (el "primero" del par)
                while (enumerator.MoveNext())
                {
                    T first = enumerator.Current; // El primer elemento del par

                    // Intenta mover al siguiente elemento (el "segundo" del par)
                    if (enumerator.MoveNext())
                    {
                        T second = enumerator.Current; // El segundo elemento del par
                        yield return (first, second); // Devuelve el par
                    }
                    else
                    {
                        // Si no hay un segundo elemento, significa que el número de elementos es impar
                        // y el último elemento no tiene un par. En este caso, simplemente terminamos.
                        yield break;
                    }
                }
            }
        }
    }
}
