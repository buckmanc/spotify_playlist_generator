using spotify_playlist_generator.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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

        public static string PrettyString(this FullTrack track)
        {
            return track.Artists.Select(a => a.Name).Join(", ") + " - " + track.Name;
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
    }
}
