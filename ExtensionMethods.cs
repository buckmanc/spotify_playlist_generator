using CsvHelper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        /// <param name="Take"></param>
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
            catch (SpotifyAPI.Web.APIException ex)
            {
                //if the spotify api throws an exception, just eat it
                //TODO either refine this a bit or supress the compiler warning
                Console.WriteLine("APIException thrown in ToListAsync: " + ex.ToString());
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
        /// <returns></returns>
        /// emulates an old VB function, which was very convenient
        public static string Join(this IEnumerable<string> value)
        {
            return string.Join(string.Empty, value.ToArray());
        }
        public static string Join(this IEnumerable<char> value)
        {
            return string.Join(string.Empty, value.ToArray());
        }

        /// <summary>
        /// Removes the specified string from the beginning of a string.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="trimString"></param>
        /// <param name="comparisonType"></param>
        /// <returns></returns>
        public static string TrimStart(this string value, string trimString = " ", StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(trimString)) return value;

            while (value.StartsWith(trimString, comparisonType))
            {
                value = value.Substring(trimString.Length);
            }

            return value;

        }

        /// <summary>
        /// Removes the specified string from the beginning of a string.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="trimString"></param>
        /// <param name="comparisonType"></param>
        /// <returns></returns>
        public static string TrimEnd(this string value, string trimString = " ", StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(trimString)) return value;

            while (value.EndsWith(trimString, comparisonType))
            {
                value = value.Substring(trimString.Length);
            }

            return value;

        }

        /// <summary>
        /// Removes the specified string from the beginning of a string.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="trimString"></param>
        /// <param name="comparisonType"></param>
        /// <returns></returns>
        public static string Trim(this string value, string trimString = " ", StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
        {
            value = value.TrimStart(trimString, comparisonType);
            value = value.TrimEnd(trimString, comparisonType);

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
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(search) || !value.Contains(search)) return 0;

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
        public static List<List<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
        {
            //pulled from https://stackoverflow.com/questions/11463734/split-a-list-into-smaller-lists-of-n-size
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        /// <summary>
        /// Removes the elements in the collection from the List&lt;T&gt;.
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
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionaryTo"></param>
        /// <param name="dictionaryFrom"></param>
        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionaryTo, IDictionary<TKey, TValue> dictionaryFrom)
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

            return returnString.TrimEnd('0').TrimEnd('.');
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

        public static string Remove(this string value, string stringToRemove)
        {
            return value.Remove(new string[] { stringToRemove });
        }

        public static string Remove(this string value, IEnumerable<string> stringsToRemove)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            foreach (var removey in stringsToRemove)
            {
                value = value.Replace(removey, string.Empty);
            }

            return value;
        }
        public static void Remove<TSource>(this List<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }

            var itemsToRemove = source.Where(item => predicate(item)).ToList();
            source.RemoveRange(itemsToRemove);
        }

        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> value, TKey key)
        {
            if (value.TryGetValue(key, out TValue result))
            {
                return result;
            }

            return default(TValue);
        }

        public static List<TValue> TryGetValues<TKey, TValue>(this IDictionary<TKey, TValue> value, IEnumerable<TKey> keys)
        {
            var output = new List<TValue>();

            foreach (var key in keys)
            {
                if (value.TryGetValue(key, out TValue result))
                {
                    output.Add(result);
                }
            }

            return output;
        }

        public static void TryRemove<TKey, TValue>(this IDictionary<TKey, TValue> value, IEnumerable<TKey> keys)
        {
            foreach (var key in keys)
            {
                if (value.ContainsKey(key))
                    value.Remove(key, out TValue result);
            }
        }

        //found here
        //https://stackoverflow.com/a/4146349
        /// <summary>
        /// Compares the string against a given pattern.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="pattern">The pattern to match, where "*" means any sequence of characters, and "?" means any single character.</param>
        /// <returns><c>true</c> if the string matches the given pattern; otherwise <c>false</c>.</returns>
        public static bool Like(this string str, string pattern)
        {
            if (string.IsNullOrWhiteSpace(str) || string.IsNullOrWhiteSpace(pattern))
                return false;

            // intensive normalization
            // this is specifically to handle
            // 1) complex album names with inconsistent spacing, ellipses, or punctuation
            // 2) words with accent marks that are difficult to type
            str = str.Standardize();
            pattern = pattern.Standardize();

            var output = new Regex(
                "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            ).IsMatch(str);

            return output;
        }
        public static string Indent(this string value, int spaceCount = 4)
        {
            var spaces = new string(' ', spaceCount);

            var output = value
                .ReplaceLineEndings()
                .Split(Environment.NewLine)
                .Select(line => spaces + line)
                .Join(Environment.NewLine)
                ;

            return output;
        }
        public static string RemoveAccents(this string text)
        {
            StringBuilder sbReturn = new StringBuilder();
            var arrayText = text.Normalize(NormalizationForm.FormD).ToCharArray();
            foreach (char letter in arrayText)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(letter) != UnicodeCategory.NonSpacingMark)
                    sbReturn.Append(letter);
            }
            return sbReturn.ToString();
        }

        private static char[] _wildcards = new char[] { '*', '?' };
        public static string AlphanumericOnly(this string value, bool preserveWildcards = false)
        {
            return value.Where(c =>
                char.IsLetterOrDigit(c) ||
                (preserveWildcards && _wildcards.Contains(c))
            ).Join();
        }

        public static TSource Random<TSource>(this IList<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            else if (!source.Any())
                return default(TSource);

            var rnd = new Random();
            if (!source.TryGetNonEnumeratedCount(out var elementCount))
                elementCount = source.Count();

            var index = rnd.Next(0, elementCount - 1);

            return source[index];
        }
        public static string ToShortDateTimeString(this DateTime value)
        {
            return value.ToShortDateString() + " " + value.ToLongTimeString();
        }

        public static ConcurrentBag<T> ToConcurrentBag<T>(this IEnumerable<T> source) => new ConcurrentBag<T>(source);

        //https://www.dotnetperls.com/levenshtein
        public static int LevenshteinDistance(this string s, string t)
        {
            s = s.Standardize();
            t = t.Standardize();

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Verify arguments.
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Initialize arrays.
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Begin looping.
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    // Compute cost.
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
                }
            }
            // Return cost.
            return d[n, m];
        }

        public static double LevenshteinPercentChange(this string s, string t)
        {
            var dist = s.LevenshteinDistance(t);
            var perc = dist * 1.0 / Math.Max(s.Length, t.Length);
            return perc;
        }
        public static string AddOrdinal(this int num)
        {
            //https://stackoverflow.com/a/20175

            if (num <= 0) return num.ToString();

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return num + "th";
            }

            switch (num % 10)
            {
                case 1:
                    return num + "st";
                case 2:
                    return num + "nd";
                case 3:
                    return num + "rd";
                default:
                    return num + "th";
            }
        }

        public static string Standardize(this string value)
        {
            return value
                .Trim()
                .TrimStart("the ")
                .Replace(" & ", " and ")
                .RemoveAccents()
                .AlphanumericOnly(preserveWildcards: true)
                .ToLower()
                ;
        }

        public static string NullIfEmpty(this string s)
        {
            return string.IsNullOrEmpty(s) ? null : s;
        }
        public static string NullIfWhiteSpace(this string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        public static string SoftSubstring(this string s, int startIndex, int length)
        {
            if (s.Length < startIndex) return null;

            if (s.Length < startIndex + length) length = s.Length - startIndex;

            return s.Substring(startIndex, length);
        }
        public static bool StartsWith(this string s, IEnumerable<string> values)
        {
            return values.Any(x => s.StartsWith(x));
        }
        public static string ToTitleCase(this string s)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);
        }
        public static string PrettyPrint(this Dictionary<string,string> value, string delimiter = ":")
        {
            if (value == null || !value.Any())
                return string.Empty;

            //limit the key length
            var kvps = value.Select(kvp => new
            {
                //Key = kvp.Key.Clipsis(7),
                Key = kvp.Key.SoftSubstring(0, 7),
                kvp.Value
            }).ToArray();

            var padLen = kvps.Max(kvp => kvp.Key.Length + 2);
            var sb = new StringBuilder();
            foreach (var kvp in kvps)
            {
                // sb.AppendLine(kvp.Key.PadRight(padLen) + " " + delimiter + " " + kvp.Value);
                sb.AppendLine((kvp.Key + delimiter + " ").PadRight(padLen) + kvp.Value);
            }

            return sb.ToString().TrimEnd(Environment.NewLine.First()).TrimEnd(Environment.NewLine.Last());
        }
        public static DateOnly DateFromStringWithMissingParts(this string s)
        {

            var year = s.SoftSubstring(0, 4).NullIfEmpty() ?? "1900";
            var month = s.SoftSubstring(5, 2).NullIfEmpty() ?? "01";
            var day = s.SoftSubstring(8, 2).NullIfEmpty() ?? "01";
            var output = new DateOnly(int.Parse(year), int.Parse(month), int.Parse(day));

            return output;
        }

        public static void ToCSV<T>(this IEnumerable<T> records, string path)
        {
            //gronk checks
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            var ext = System.IO.Path.GetExtension(path);
            if (ext.ToLower() != ".csv")
                path = System.IO.Path.ChangeExtension(path, ".csv");

            using (var writer = new StreamWriter(path))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }
        }

        public static string Clipsis(this string value, int maxLength)
        {
            var ellipsis = "..";
            var output = value;
            if (value.Length > maxLength && value.Length > ellipsis.Length)
                output = value.Substring(0, maxLength - ellipsis.Length) + ellipsis;

            return output;
        }
    }
}
