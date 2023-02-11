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
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> values, int Take = -1)
        {
            //force iteration
            var returnValues = new List<T>();
            var itemCount = 0;

            try
            {
                await foreach (var value in values)
                {

                    if (Take > -1 && itemCount >= Take)
                        break;

                    returnValues.Add(value);
                    itemCount += 1;
                }
            }
            catch (SpotifyAPI.Web.APIException)
            {
                //if the spotify api throws an exception, just eat it
                //TODO either refine this a bit or supress the compiler warning
            }
            return returnValues;

        }
        /// <summary>
        /// Returns a string of the contained elements joined with the specified separator.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        /// emulates an old VB function, which was very convenient
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
        /// emulates an old VB function, which was very convenient
        public static string Join(this IEnumerable<string> value)
        {
            return string.Join(string.Empty, value.ToArray());
        }

        /// <summary>
        /// Removes the specified string from the beginning of a string.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="trimString"></param>
        /// <returns></returns>
        public static string TrimStart(this string value, string trimString = " ")
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(trimString)) return value;

            while (value.StartsWith(trimString))
            {
                value = value.Substring(trimString.Length);
            }

            return value;

        }

        /// <summary>
        /// Count how often one string occurs in another.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        public static int CountOccurrences(this string value, string search)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(search) || !value.Contains(search))  return 0;

            var iReturn = (value.Length - value.Replace(search, string.Empty).Length) / search.Length;

            return iReturn;

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
        /// <summary>
        /// Add elements from one dictionary to another.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="collection"></param>
        public static void AddRange<T>(this Dictionary<string, T> dictionaryTo, Dictionary<string, T> dictionaryFrom)
        {
            //pulled from https://stackoverflow.com/a/6695211 
            dictionaryFrom.ToList().ForEach(x => dictionaryTo.Add(x.Key, x.Value));
        }

        public static string FindTextBetween(this string text, string left, string right)
        {
            // pulled from https://stackoverflow.com/a/43273574
            // TODO: Validate input arguments

            int beginIndex = text.IndexOf(left); // find occurence of left delimiter
            if (beginIndex == -1)
                return string.Empty; // or throw exception?

            beginIndex += left.Length;

            int endIndex = text.IndexOf(right, beginIndex); // find occurence of right delimiter
            if (endIndex == -1)
                return string.Empty; // or throw exception?

            return text.Substring(beginIndex, endIndex - beginIndex).Trim();
        }

        public static string RemoveAfterString(this string value, string RemoveAfter)
        {
            //based on https://stackoverflow.com/a/2660734 
            if (string.IsNullOrEmpty(value))
                return value;

            int index = value.IndexOf(RemoveAfter);
            if (index >= 0)
                return value.Substring(0, index);

            return value;
        }

        public static string ToHumanTimeString(this TimeSpan span, int decimalPlaces = 2)
        {
            //inspired by this one, but more readable
            //https://www.extensionmethod.net/csharp/timespan/timespan-tohumantimestring
            var format = "N" + decimalPlaces;
            string returnString;

            if (span.TotalMinutes < 1)
                returnString = span.TotalSeconds.ToString(format) + " seconds";
            else if (span.TotalHours < 1)
                returnString = span.TotalMinutes.ToString(format) + " minutes";
            else if (span.TotalDays < 1)
                returnString = span.TotalHours.ToString(format) + " hours";
            else
                returnString = span.TotalDays.ToString(format) + " days";

            return returnString;
        }

        public static T ResultSafe<T>(this Task<T> value)
        {
            try
            {
                value.Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                //if the exception is an invalid playlist/artist uri exception from SpotifyAPI, ignore it
                if (!ex.Message.Contains("Not found") && !ex.Message.Contains("non existing id"))
                    throw;
                return default;
            }

            return value.Result;
        }
    }
}
