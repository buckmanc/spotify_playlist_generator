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
            var output = (spotifyWrapper.spotify.Paginate(value.Tracks).ToListAsync()).Result
                    .Select(x => (FullTrack)x.Track)
                    .ToList();

            return output;
        }
    }
}
