using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spotify_playlist_generator
{
    static class ExtensionMethods
    {
        /// <summary>
        /// Forces iteration of an IAsyncEnumerable; a shortcut against a manual for each.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> values)
        {
            //force iteration
            var returnValues = new List<T>();
            await foreach (var value in values)
            {
                returnValues.Add(value);
            }

            return returnValues;

        }
        /// <summary>
        /// Returns a string of the contained elements joined with the specified separator.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string Join(this IEnumerable<string> value, string separator)
        {
            return string.Join(separator, value.ToArray());
        }
        /// <summary>
        /// Returns a string of the contained elements joined.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string Join(this IEnumerable<string> value)
        {
            return string.Join(string.Empty, value.ToArray());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="chunkSize"></param>
        /// <returns></returns>
        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
        {
            //pulled from https://stackoverflow.com/questions/11463734/split-a-list-into-smaller-lists-of-n-size
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
        /// <summary>
        /// Removes the elements in the collection from the List<T>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="collection"></param>
        /// Adding in the suspiciously missing RemoveRange, counterpart to AddRange
        public static void RemoveRange<T>(this List<T> values, IEnumerable<T> collection)
        {
            foreach (var item in collection)
                values.Remove(item);
        }
    }
}
