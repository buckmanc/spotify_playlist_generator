using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spotify_playlist_generator
{
    static class SpotifyExtensionMethods
    {

        private static System.Collections.Concurrent.ConcurrentDictionary<string, FullArtist> _artistCache = new System.Collections.Concurrent.ConcurrentDictionary<string, FullArtist>();
        /// <summary>
        /// Returns artists from the local cache or the Spotify API.
        /// </summary>
        /// <param name="spotify"></param>
        /// <param name="artistIDs"></param>
        /// <returns>Returns a list of FullArtist</returns>
        public static async Task<List<FullArtist>> GetArtists(this SpotifyClient spotify, IEnumerable<string> artistIDs)
        {
            artistIDs = artistIDs.Distinct();

            var output = new List<FullArtist>();
            var artistIdApiQueue = new List<string>();

            //check the local cache for any items that we've already retrieved from the api
            //queue up any items that aren't in the local cache
            foreach (var artistID in artistIDs)
            {
                FullArtist foundArtist;
                if (_artistCache.TryGetValue(artistID, out foundArtist))
                    output.Add(foundArtist);
                else
                    artistIdApiQueue.Add(artistID);
            }

            //pull any remaining items from the api
            foreach (var idChunk in artistIdApiQueue.ChunkBy(50))
            {
                var chunkArtists = (await spotify.Artists.GetSeveral(new ArtistsRequest(idChunk))).Artists
                    .Where(a => a != null)
                    .ToList();

                output.AddRange(chunkArtists);
                //cache items
                //if (_artistCache.Count <= 10000)
                foreach (var artist in chunkArtists)
                    _artistCache.TryAdd(artist.Id, artist);
            }

            return output;
        }

        private static System.Collections.Concurrent.ConcurrentDictionary<string, FullTrack> _trackCache = new System.Collections.Concurrent.ConcurrentDictionary<string, FullTrack>();
        /// <summary>
        /// Returns tracks from the local cache or the Spotify API.
        /// </summary>
        /// <param name="spotify"></param>
        /// <param name="trackIDs"></param>
        /// <returns>Returns a list of FullTrack</returns>
        public static async Task<List<FullTrack>> GetTracks(this SpotifyClient spotify, IEnumerable<string> trackIDs)
        {
            trackIDs = trackIDs.Distinct();

            var output = new List<FullTrack>();
            var trackIdApiQueue = new List<string>();

            //check the local cache for any items that we've already retrieved from the api
            //queue up any items that aren't in the local cache
            foreach (var trackID in trackIDs)
            {
                FullTrack foundTrack;
                if (_trackCache.TryGetValue(trackID, out foundTrack))
                    output.Add(foundTrack);
                else
                    trackIdApiQueue.Add(trackID);
            }

            //pull any remaining items from the api
            foreach (var idChunk in trackIdApiQueue.ChunkBy(50))
            {
                var chunkTracks = (await spotify.Tracks.GetSeveral(new TracksRequest(idChunk))).Tracks
                    .Where(a => a != null)
                    .ToList();

                output.AddRange(chunkTracks);
                //cache items
                //if (_trackCache.Count <= 10000)
                foreach (var track in chunkTracks)
                    _trackCache.TryAdd(track.Id, track);
            }

            return output;
        }
        public static void AddToCache(this SpotifyClient spotify, IEnumerable<FullTrack> tracks)
        {
            foreach (var track in tracks)
                _trackCache.TryAdd(track.Id, track);

        }
        public static void AddToCache(this SpotifyClient spotify, IEnumerable<FullArtist> artists)
        {
            foreach (var artist in artists)
                _artistCache.TryAdd(artist.Id, artist);

        }

        private static List<FullTrack> _likedTracks;
        private static bool _getLikedTracksRunning;
        public static async System.Threading.Tasks.Task<List<FullTrack>> GetLikedTracks(this SpotifyClient spotify)
        {
            //threadsafe this sucker
            //well *mostly* threadsafe
            while (_getLikedTracksRunning)
                System.Threading.Thread.Sleep(1000);

            if (_likedTracks != null)
                return _likedTracks;

            _getLikedTracksRunning = true;

            var tracks = spotify.Paginate(await spotify.Library.GetTracks())
                .ToListAsync()
                .Result
                .Select(x => x.Track)
                .ToList();

            Console.WriteLine(
                "Found " +
                tracks.Count().ToString("#,##0") +
                " liked tracks from " +
                tracks.SelectMany(t => t.Artists.Select(a => a.Name)).Distinct().Count().ToString("#,##0") +
                " artists."
                );

            _likedTracks = tracks;

            _getLikedTracksRunning = false;

            spotify.AddToCache(tracks);

            return tracks;

        }

    }
}
