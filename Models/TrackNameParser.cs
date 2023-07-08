using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace spotify_playlist_generator.Models
{
    internal class TrackNameParser
    {
        public TrackNameParser(string trackName, string albumName)
        {
            // just for debugging
            var ogTrackName = trackName;
            var ogAlbumName = albumName;

            // a shitty work around for not knowing how to fix the regex lol
            trackName = trackName
                .Replace("lo-fi", "lofi")
                .Replace("lo-fi", "LoFi", StringComparison.InvariantCultureIgnoreCase);

            var reggies = new List<Regex>();
            var clauseStartPattern = @" (\[|\(|- )";
            var clauseEndPattern = @"(\]|\)|$)";
            reggies.Add(new Regex(clauseStartPattern + @"(?<key> ?From|For|Featuring|Feat|With):?\.? [“""]?(?<value>[^\[\(\]\)]+?)( ost| soundtrack)?[”""]?" + clauseEndPattern, RegexOptions.IgnoreCase));
            reggies.Add(new Regex(clauseStartPattern + @"(?<value>[^\[\(\]\)\-]+?) (?<key>Remix|Mix|Edit|Version|Ver|Cut|Dub|Cover|Edition)\.?(s|es)? ?" + clauseEndPattern, RegexOptions.IgnoreCase));
            reggies.Add(new Regex(clauseStartPattern + @"(piano |guitar )?(?<key>instrumental|acoustic|live|album|bonus|remake|cover|single|piano|guitar)( guitar| piano)?( solo)?( arrangement)?( track)?" + clauseEndPattern, RegexOptions.IgnoreCase));
            var trackClauseMatches = new List<Match>();

            var trackClauseDeets = new Dictionary<string, string>();
            // order here is important; prioritize track name matches first
            trackClauseMatches.AddRange(reggies.SelectMany(r => r.Matches(trackName)).ToArray());
            trackClauseMatches.AddRange(reggies.SelectMany(r => r.Matches(albumName)).ToArray());

            // match a less bounded from clause pattern against the album name only
            var noSwapMatches = new List<Match>();
            noSwapMatches.Add(Regex.Match(albumName, @"\s(?<key>From)\s(?<value>[^\[\(\]\)]+?)$", RegexOptions.IgnoreCase));
            trackClauseMatches.AddRange(noSwapMatches);
            trackClauseMatches = trackClauseMatches.Where(r => r.Success).ToList();

            // parse out the matches to make things easier to use
            var polishedMatches = trackClauseMatches.Select(m => new
            {
                Key = m.Groups["key"].Value.Trim().ToTitleCase(),
                Value = m.Groups["value"].Value.Trim().NullIfEmpty() ?? "yes",
                FullMatch = m.Value,
                NoSwap = noSwapMatches.Contains(m)
            }).ToArray();

            foreach (var match in polishedMatches)
            {
                var addKeyName = new string[] { "remix", "mix" };

                // TODO look at the data and see if this should be something besides a dictionary
                var added = trackClauseDeets.TryAdd(match.Key, match.Value + (addKeyName.Any(x => match.Key.Like(x)) ? " " + match.Key : string.Empty));

                //if (!added && Program.Settings._VerboseDebug && trackClauseDeets[match.Key] != match.Value)
                //{
                //    Console.WriteLine("[Debug] duplicate key found!");
                //    Console.WriteLine("[Debug]\t\ttrackName: " + ogTrackName);
                //    Console.WriteLine("[Debug]\t\talbumName: " + ogAlbumName);
                //    Console.WriteLine("[Debug]\t\tkey:       " + match.Key);
                //    Console.WriteLine("[Debug]\t\toriginal : " + trackClauseDeets[match.Key]);
                //    Console.WriteLine("[Debug]\t\tnew      : " + match.Value);
                //}

                // don't pull the match from the name if it's one of the more permissible ones
                if (!match.NoSwap)
                    trackName = trackName.Replace(match.FullMatch, string.Empty);

                if (!match.NoSwap && match.Key.ToLower() == "from")
                {
                    // the trailing escape char below is to escape the first dash, which Regex.Escape will not escape, but is interpreted as a span/range character instead of literal otherwise
                    // var boundingCharClass = @"[:\s""\" + Program.dashes.Join(string.Empty) + @"]{0,3}";
                    var boundingCharClass = @"(:|\s|""|^|$|\" + Program.dashes.Join("|") + @"){0,3}";
                    var regexString = boundingCharClass + Regex.Escape(match.Value) + boundingCharClass;
                    //Console.WriteLine("[Debug] " + regexString);
                    var reggy = new Regex(regexString, RegexOptions.IgnoreCase);
                    albumName = reggy.Replace(albumName, "...");
                }
            }

            //// tweaks to clauses
            //foreach (var kvp in trackClauseDeets)
            //{
            //    if (kvp.Key.Like("from") && kvp.Value.EndsWith(" ost", StringComparison.InvariantCultureIgnoreCase))
            //        trackClauseDeets[kvp.Key] = kvp.Value.Substring(0, kvp.Value.Length - 4);
            //    else if (kvp.Key.Like("instr") && kvp.Value.Like("umental"))
            //        trackClauseDeets[kvp.Key] = "yes";
            //}

            this.AllClauses = trackClauseDeets;

            this.TrackShortName = trackName;
            this.AlbumShortName = albumName;

            // set values for any property that exists
            // need to check how this handles casing
            foreach (var prop in this.GetType().GetProperties())
            {
                if(this.AllClauses.TryGetValue(prop.Name.Replace("Clause", string.Empty), out var val))
                    prop.SetValue(this, val);
            }
        }

        public string TrackShortName { get; set; }
        public string AlbumShortName { get; set; }
        public Dictionary<string,string> AllClauses { get; set; }

        public string FromClause { get; set; }
        public string ForClause { get; set; }
        public string FeaturingClause { get; set; }
        public string FeatClause { get; set; }
        public string RemixClause { get; set; }
        public string EditClause { get; set; }
        public string CutClause { get; set; }
        public string DubClause { get; set; }
        public string CoverClause { get; set; }
        public string InstrumentalClause { get; set; }
        public string AcousticClause { get; set; }
    }
}
