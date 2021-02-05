using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace italki
{
    public static class EnumerableExtension
    {
        /// <summary>
        /// Break a list of items into chunks of a specific size
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (chunkSize < 1) throw new ArgumentException("Chunk size can't be smaller then 1", nameof(chunkSize));

            while (source.Any())
            {
                yield return source.Take(chunkSize);
                source = source.Skip(chunkSize);
            }
        }
    }
}
