using spotify_playlist_generator.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace spotify_playlist_generator
{
    static class SpotifyExtensions
    {
        //ideally these would be on FullPlaylistDetails, but that's not fully implemented yet
        public static string GetWorkingImagePath(this FullPlaylist playlist)
        {
            var chars = playlist.Name.ToList();
            chars.RemoveRange(System.IO.Path.GetInvalidFileNameChars());
            var safeName = chars.Select(c => c.ToString()).Join();

            return System.IO.Path.Join(Program.Settings._ImageWorkingFolderPath, safeName + ".jpg");
        }
        public static string GetBackupImagePath(this FullPlaylist playlist)
        {
            var chars = playlist.Name.ToList();
            chars.RemoveRange(System.IO.Path.GetInvalidFileNameChars());
            var safeName = chars.Select(c => c.ToString()).Join();

            return System.IO.Path.Join(Program.Settings._ImageBackupFolderPath, safeName + ".jpg");
        }

        public static IList<FullTrack> GetTracks(this FullPlaylist value, MySpotifyWrapper spotifyWrapper)
        {
            return Retry.Do(() =>
            {
                var output = (spotifyWrapper.spotify.Paginate(value.Tracks).ToListAsync()).Result
                        .Select(x => (FullTrack)x.Track)
                        .ToList();

                if (Program.Settings._VerboseDebug && output.Any(t => t == null))
                {
                    Console.WriteLine("Spotify returned null tracks from a playlist!");
                    Console.WriteLine("Playlist: " + (value?.Name ?? "null"));
                    Console.WriteLine("Tracks: " + (output?.Count() ?? 0).ToString("#,##0"));
                    throw new Exception("Spotify returned null tracks from a playlist!");
                }

                return output;
            });
        }

        public static string PrettyString(this FullTrack track, bool verbose = false)
        {
            if (verbose)
            {
                // parsing out track clauses to save width at the terminal
                // the "from" clause is the popular format for soundtrack and video game music covers
                var trackName = track.Name;
                var trackClauseDeets = new Dictionary<string,string>();
                var trackClauseMatches = new List<Match>();
                trackClauseMatches.AddRange(Regex.Matches(track.Name, @" [\[\(](?<key>From|For|Featuring|Feat)\.? ""?(?<value>[^\[\(\]\)]+?)""?[\]\)]", RegexOptions.IgnoreCase));
                trackClauseMatches.AddRange(Regex.Matches(track.Name, @" [\[\(-] ?(?<value>[^\[\(\]\)]+?)(?<key>Remix|Mix|Edit|Version|Cut|Dub|Cover) ?[\]\)]?", RegexOptions.IgnoreCase));

                foreach (Match match in trackClauseMatches)
                {
                    var addKeyName = new string[] {"remix", "mix", "edit"};
                    var key = match.Groups["key"].Value.ToTitleCase();
                    var value = match.Groups["value"].Value.Trim();

                    trackClauseDeets.Add(key, value + (addKeyName.Any(x => key.Like(x)) ? " " + key : string.Empty));
                    trackName = trackName.Replace(match.Value, string.Empty);
                }

                var fromClause = trackClauseDeets.Where(kvp => kvp.Key.Like("from")).Select(kvp => kvp.Value).FirstOrDefault();

                var output = new Dictionary<string,string>();
                output.Add(track.Artists.Count() > 1 ? "Artists" : "Artist", track.Artists.Select(a => a.Name).Join(", "));
                output.Add("Track", trackName);
                output.AddRange(trackClauseDeets);
                // album deets if this isn't a single
                if (!(track.Name.AlphanumericOnly().ToLower() == track.Album.Name.AlphanumericOnly().ToLower() && track.Album.TotalTracks == 1))
                {
                    var albumName = track.Album.Name;
                    if (!string.IsNullOrWhiteSpace(fromClause))
                    {
                        var boundingCharClass = @"[:\s" + Program.dashes.Join(string.Empty) + @"]{0,3}";
                        var regexString = boundingCharClass + Regex.Escape(fromClause) + boundingCharClass;
                        var reggy = new Regex(regexString, RegexOptions.IgnoreCase);
                        // output.Add("test", regexString);
                        albumName = reggy.Replace(albumName, "...");
                    }
                    output.Add("Album", albumName);
                    output.Add("Num", track.TrackNumber.ToString() + "/" + track.Album.TotalTracks.ToString());
                }
                output.Add("Year", track.Album.ReleaseDate.Substring(0, 4));

                return Environment.NewLine + output.PrettyPrint();
            }

            return track.Artists.Select(a => a.Name).Join(", ") + " - " + track.Name + " (" + track.Album.ReleaseDate.Substring(0, 4) + ")";
        }

        public static IList<PlaylistReorderItemsRequest> Mash(this IEnumerable<PlaylistReorderItemsRequest> value)
        {
            var requests = value
                .Distinct()
                .OrderBy(x => x.InsertBefore)
                .ThenBy(x => x.RangeStart)
                .ToList();

            for (int i = 0; i < requests.Count -1; i++)
            {
                var currentItem = requests[i];
                var nextItem = requests[i + 1];

                var proposedNewLength = (currentItem.RangeLength ?? 1) + (nextItem.RangeLength ?? 1);

                if (currentItem.RangeStart + (currentItem.RangeLength ?? 1) == nextItem.RangeStart &&
                    currentItem.InsertBefore + (currentItem.RangeLength ?? 1) == nextItem.InsertBefore &&
                    proposedNewLength <= 100 // only mash if the output of this change would be less than the max of tracks to reorder per request

                    )
                {
                    currentItem.RangeLength = proposedNewLength;
                    requests.Remove(nextItem);
                    i--;
                }
            }

            return requests;
        }
        public static IList<FullTrack> Reorder(this IList<FullTrack> value, PlaylistReorderItemsRequest request)
        {
            var tracks = value.ToList();
            var movingTracks = tracks.GetRange(request.RangeStart, request.RangeLength ?? 1);
            tracks.RemoveRange(request.RangeStart, request.RangeLength ?? 1);

            var insertIndex = request.InsertBefore;
            if (insertIndex > request.RangeStart)
                insertIndex = insertIndex - (request.RangeLength ?? 1);

            tracks.InsertRange(insertIndex, movingTracks);

            return tracks;
        }
        public static IList<string> GetArtistIDs(this IList<FullTrackDetails> value, IEnumerable<string> ExceptArtists = null)
        {

            var artistDeets = value
            //.SelectMany(t => t.ArtistIds)
            .SelectMany(t => t.ArtistIds.Select(id => new
            {
                ArtistID = id,
                ArtistName = t.ArtistNames[t.ArtistIds.IndexOf(id)]
            }))
            .Distinct()
            .ToArray();

            if (ExceptArtists != null && ExceptArtists.Any())
            {
                artistDeets = artistDeets
                    .Where(a =>
                    !ExceptArtists.Any(e => a.ArtistName.Like(e))
                    ).ToArray();
            }

            var output = artistDeets.Select(a => a.ArtistID).Distinct().ToArray();
            return output;
        }
    }
}
