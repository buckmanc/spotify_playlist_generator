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
            return System.IO.Path.Join(Program.Settings._ImageWorkingFolderPath, playlist.Name + ".jpg");
        }
        public static string GetBackupImagePath(this FullPlaylist playlist)
        {
            return System.IO.Path.Join(Program.Settings._ImageBackupFolderPath, playlist.Name + ".jpg");
        }
    }
}
