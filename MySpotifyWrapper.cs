using spotify_playlist_generator.Models;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SpotifyAPI.Web.PlaylistRemoveItemsRequest;

namespace spotify_playlist_generator
{
    //TODO make an interface for this so you can support unit tests
    internal class MySpotifyWrapper : IDisposable
    {
        private SpotifyClient _spotify;
        public SpotifyClient spotify
        {
            get
            {
                if (_spotify == null)
                {
                    this.RefreshSpotifyClient();
                }
                return _spotify;
            }
            set { _spotify = value; }
        }

        private string _trackCachePath = System.IO.Path.Join(Program.AssemblyDirectory, "track_cache.json");
        private Guid _sessionID = Guid.NewGuid();

        public void Dispose()
        {
            WriteCache();
        }

        private void WriteCache()
        {
            if (Program.Settings._VerboseDebug)
            {
                Console.WriteLine();
                Console.WriteLine("writing serialized cache");
            }

            //skip writing if we haven't read
            //checking against the backing property to avoid triggering a read
            if (this._trackCache != null && this._trackCache.Any())
            {
                using var stream = File.Create(this._trackCachePath);
                System.Text.Json.JsonSerializer.Serialize(stream, this.TrackCache);
                stream.Dispose();
            }
        }

        private void ReadCache()
        {
            //TODO deserialize the cache here
            if (System.IO.File.Exists(_trackCachePath))
            {
                if (Program.Settings._VerboseDebug)
                {
                    Console.WriteLine("reading serialized cache");
                    Console.WriteLine();
                }
                using var stream = File.OpenRead(this._trackCachePath);
                this._trackCache = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Concurrent.ConcurrentDictionary<string, FullTrackDetails>>(stream);
                stream.Dispose();
            }

        }

        public void RefreshSpotifyClient()
        {
            //TODO better scope management
            var accessToken = Program.UpdateTokens().Result;

            //exit for token problems
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                Console.WriteLine("Problem with the access tokens! Make sure client ID and client secret are in tokens.ini");
                Environment.Exit(-1);
            }

            var config = SpotifyClientConfig
              .CreateDefault()
              .WithRetryHandler(new CustomRetryHandler()
              {
                  //RetryAfter = TimeSpan.FromSeconds(4),
                  //TooManyRequestsConsumesARetry = true
              })
              .WithToken(accessToken)
              ;

            this.spotify = new SpotifyClient(config);
        }


        private System.Collections.Concurrent.ConcurrentDictionary<string, FullArtist> _artistCache = new();
        /// <summary>
        /// Returns artists from the local cache or the Spotify API.
        /// </summary>
        /// <param name="artistIDs"></param>
        /// <returns>Returns a list of FullArtist</returns>
        public List<FullArtist> GetArtists(IEnumerable<string> artistIDs)
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
                var chunkArtists = (this.spotify.Artists.GetSeveral(new ArtistsRequest(idChunk))).Result.Artists
                    .Where(a => a != null)
                    .ToList();

                output.AddRange(chunkArtists);

                foreach (var artist in chunkArtists)
                    _artistCache.TryAdd(artist.Id, artist);
            }

            return output;
        }
        public List<FullArtist> GetArtists(IEnumerable<FullTrack> tracks)
        {
            var artistIDs = tracks
                .SelectMany(t => t.Artists.Select(a => a.Id))
                .Distinct()
                .ToArray();

            return this.GetArtists(artistIDs);
        }

        //do not save this cache
        private System.Collections.Concurrent.ConcurrentDictionary<string, string> _artistByNameCache = new();
        public List<FullArtist> GetArtistsByName(IEnumerable<string> artistNames)
        {

            //the only example of searching with the API wrapper
            //https://johnnycrazy.github.io/SpotifyAPI-NET/docs/pagination

            //query examples found here
            //https://developer.spotify.com/documentation/web-api/reference/#writing-a-query---guidelines

            //var testRequest = new SearchRequest(SearchRequest.Types.Artist, "artist:" + artistNames.First().Trim());
            //var testRequest = new SearchRequest(SearchRequest.Types.Artist, "artist:" + "Henrick Janson");
            //var testRequest = new SearchRequest(SearchRequest.Types.Artist, "Henrick Janson");
            //var testSearch = spotify.Search.Item(testRequest).Result;
            //var testArtists = spotify.Paginate(testSearch.Artists, s => s.Artists, new WaitPaginator(WaitTime: 500))
            //        .ToListAsync(Take: 100).Result;
            //var testNames = testArtists.Select(a => a.Name).ToArray();

            //var test = new string[] { "Henrick Janson" }
            //    .Select(artistName => new SearchRequest(SearchRequest.Types.Artist, artistName.Trim()))
            //    .Select(request => spotify.Search.Item(request).Result)
            //    .SelectMany(item => spotify.Paginate(item.Artists, s => s.Artists, new WaitPaginator(WaitTime: 500))
            //        .ToListAsync(Take: 40).Result // would like this to be 1, but the sought for artists are missing with less than 40
            //        .Where(artist => artistNames.Contains(artist.Name, StringComparer.InvariantCultureIgnoreCase)) // can't do a test on this specific artist name without a lot more mess
            //        )
            //    //.Where(artist => ppArtists.PrintProgress() && artist != null)
            //    .Where(artist => artist != null)
            //    .ToList();
            //    ;
            //"search" for artists, then correct results to actually the artists named
            //var ppArtists = new ProgressPrinter(playlistByArtist.Value.Count(), (perc, time) => ConsoleWriteAndClearLine(cursorLeft, " -- " + playlistDetailsString + " playlist -- Getting artists: " + perc + ", " + time + " remaining"));
            
            //TODO searching with accent marks has multiple problems
            //1) they're hard to type, 2) some artist names technically have them but their spotify pages don't
            var artists = artistNames
                .Select(artistName => new SearchRequest(SearchRequest.Types.Artist, artistName.Trim()) { Limit = 40 })
                .Select(request => spotify.Search.Item(request).Result)
                .SelectMany(item => spotify.Paginate(item.Artists, s => s.Artists, new WaitPaginator(WaitTime: 500))
                    .ToListAsync(Take: 40).Result // would like this to be 1, but the sought for artists are missing with less than 40
                    .Where(artist => artistNames.Contains(artist.Name, StringComparer.InvariantCultureIgnoreCase)) // can't do a test on this specific artist name without a lot more mess
                    )
                //.Where(artist => ppArtists.PrintProgress() && artist != null)
                .Where(artist => artist != null && artist.Images.Any())
                //.OrderBy(artist => System.Array.IndexOf(artistNames.ToArray(), artist.Name)) //TODO make this case insensitive first
                .OrderBy(artist => artist.Name)
                .OrderByDescending(artist => artist.Popularity)
                .ToList();

            if (Debugger.IsAttached)
            {
                var testNames = artists.Select(a => a.Name).ToArray();
                var testIDs = artists.Select(a => a.Id).Distinct().ToArray();
                var dummy = testNames;

                //one artist that appears in #Full Discog - Acoustic VGC but isn't in the source playlist
                //need to figure out where this is coming from
                if (testNames.Contains("ROZEN"))
                {
                    Debugger.Break();
                }
            }

            return artists;
        }

        public void AddToCache(IList<FullTrackDetails> tracks)
        {
            //easy way to limit cache size if desired
            //if (_trackCache.Count > 10000)
            //    return;

            var existingTrackIDs = new List<string>();

            //if any of the tracks being added have these properties
            //then merge those values into the existing entries
            if (tracks.Any(t => t.Source_AllTracks || t.Source_Liked || t.Source_Top))
            {
                var existingTracks = this.TrackCache.TryGetValues(tracks.Select(t => t.TrackId).ToArray());
                existingTrackIDs = existingTracks.Select(t => t.TrackId).ToList();

                var joinedTracks = existingTracks.Join(tracks,
                    t => t.TrackId,
                    t => t.TrackId,
                    (oldTrack, newTrack) => new { oldTrack, newTrack })
                    .ToArray();

                foreach (var joinedTrack in joinedTracks)
                {
                    //TODO make sure this actually modifies the stored track
                    if (joinedTrack.newTrack.Source_AllTracks)
                    {
                        joinedTrack.oldTrack.Source_AllTracks = true;
                    }
                    if (joinedTrack.newTrack.Source_Liked)
                    {
                        joinedTrack.oldTrack.LikedAt = joinedTrack.newTrack.LikedAt;
                        joinedTrack.oldTrack.Source_Liked = true;
                    }
                    if (joinedTrack.newTrack.Source_Top)
                    {
                        joinedTrack.oldTrack.Source_Top = true;
                    }

                }
            }

            foreach (var track in tracks.Where(t => !existingTrackIDs.Contains(t.TrackId)))
                this.TrackCache.TryAdd(track.TrackId, track);
        }

        ////TODO consider this as a method to handle access token expiration retries
        //private void CallMethodWithTryCatch(Func method, object[] parameters)
        //{

        //}

        //TODO should this be a concurrent bag instead of a dictionary?
        //the track ID as dictionary key is redundant but may offer performance benefits
        private System.Collections.Concurrent.ConcurrentDictionary<string, FullTrackDetails> _trackCache;

        public System.Collections.Concurrent.ConcurrentDictionary<string, FullTrackDetails> TrackCache
        {
            get
            {
                //simple way to lazy load the cache
                if (_trackCache == null)
                {
                    ReadCache();
                    if (_trackCache == null)
                        _trackCache = new();
                }

                return _trackCache;
            }
        }

        /// <summary>
        /// Returns tracks from the local cache or the Spotify API.
        /// </summary>
        /// <param name="trackIDs"></param>
        /// <returns>Returns a list of FullTrack</returns>
        public List<FullTrackDetails> GetTracks(IEnumerable<string> trackIDs)
        {
            trackIDs = trackIDs.Distinct();

            var output = new List<FullTrackDetails>();
            var trackIdApiQueue = new List<string>();

            //check the local cache for any items that we've already retrieved from the api
            //queue up any items that aren't in the local cache
            foreach (var trackID in trackIDs)
            {
                FullTrackDetails foundTrack;
                if (this.TrackCache.TryGetValue(trackID, out foundTrack))
                    output.Add(foundTrack);
                else
                    trackIdApiQueue.Add(trackID);
            }

            //pull any remaining items from the api
            foreach (var idChunk in trackIdApiQueue.ChunkBy(50))
            {

                //TODO wrap this api call in an "access token expired" check that calls RefreshSpotifyClient() and tries again
                var chunkTracks = (this.spotify.Tracks.GetSeveral(new TracksRequest(idChunk))).Result.Tracks
                    .Where(a => a != null)
                    .ToList();

                var artists = this.GetArtists(chunkTracks);

                var trackDetails = chunkTracks.Select(t => new FullTrackDetails(t, artists, this._sessionID)).ToList();

                output.AddRange(trackDetails);

                //cache items
                this.AddToCache(trackDetails);
            }

            return output;
        }


        private List<FullTrackDetails> _likedTracks;
        private bool _getLikedTracksRunning;
        public List<FullTrackDetails> GetLikedTracks()
        {
            //threadsafe this sucker
            //well *mostly* threadsafe
            while (_getLikedTracksRunning)
                System.Threading.Thread.Sleep(1000);

            if (_likedTracks != null)
                return _likedTracks;

            _getLikedTracksRunning = true;

            //could easily call GetTracks to assemble the tracks, but this method
            var savedTracks = spotify.Paginate(spotify.Library.GetTracks().Result)
                .ToListAsync()
                .Result
                .ToList();

            var artists = this.GetArtists(savedTracks.Select(s => s.Track));

            var tracks = savedTracks
                .Select(s => new FullTrackDetails(s, artists, this._sessionID))
                .ToList();

            Console.WriteLine(
                "Found " +
                tracks.Count.ToString("#,##0") +
                " liked tracks from " +
                tracks.SelectMany(t => t.ArtistIds).Distinct().Count().ToString("#,##0") +
                " artists."
                );

            _likedTracks = tracks;

            _getLikedTracksRunning = false;

            this.AddToCache(tracks);

            return tracks;

        }
        public List<FullTrackDetails> GetTracksByArtists(IEnumerable<string> artistNamesOrIDs)
        {
            //TODO consider how to report progress

            //TODO scope problem with this ID regex
            var artistIDs = artistNamesOrIDs
                .Where(x => Program.idRegex.Match(x).Success)
                .ToList();

            var artistNames = artistNamesOrIDs
                .Where(x => !artistIDs.Contains(x))
                .ToList();

            var searchArtists = this.GetArtistsByName(artistNames);

            artistIDs.AddRange(searchArtists.Select(a => a.Id));

            var tracks = new List<FullTrackDetails>();

            //if an artist in the cache has been retrieved by *this* method *this* session
            //then we can be assured that the artist's full discography is already in the cache
            var cachedFullDiscog = this.TrackCache.Values
                .Where(t =>
                    t.Source_AllTracks &&
                    t.SessionID == this._sessionID &&
                    artistIDs.Any(aID => t.ArtistIds.Contains(aID))
                )
                .ToArray();

            //TODO make sure this worked
            if (cachedFullDiscog.Any())
            {
                //add the tracks found in the cache to the output, remove the artists from the artists we're looking for
                tracks.AddRange(cachedFullDiscog);
                artistIDs.RemoveRange(cachedFullDiscog.SelectMany(t => t.ArtistIds).Distinct());
            }

            //get all albums for the artists found
            //var ppAlbums = new ProgressPrinter(Total: artists.Count(),
            //                                   Update: (perc, time) => ConsoleWriteAndClearLine(cursorLeft, " -- " + playlistDetailsString + " playlist -- Getting albums: " + perc + ", " + time + " remaining")
            //                                   );
            var albums = artistIDs.Select(artistID => spotify.Artists.GetAlbums(artistID).Result)
                //.Where(x => ppAlbums.PrintProgress())
                .SelectMany(item => spotify.Paginate(item, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
                .OrderBy(album => album.ReleaseDate)
                .ToList();

            //remove a particular album which is 1) a duplicate and 2) behaves erratically
            //only remove if the set contains both this album and its pair
            if (albums.Any(album => album.Id == "3jRsMOSeikuwpE9Q75Ij7I" && albums.Any(album => album.Id == "3BhDAfxJZ7Ng8oNGy3XS1v")))
            {
                albums.Remove(albums.Where(album => album.Id == "3jRsMOSeikuwpE9Q75Ij7I").SingleOrDefault());
            }

            //if an album in the cache has been retrieved by *this* method 
            //then we can be assured that the full album is already in the cache
            //if an album tracklist is changed after this though the cache will need to be cleared to get the update
            var cachedAlbumTracks = this.TrackCache.Values
                .Where(t =>
                    t.Source_AllTracks &&
                    albums.Any(a => a.Id == t.AlbumId)
                ).ToArray();

            //TODO make sure this worked
            //add the tracks found in the cache to the output, remove the albums from the album we're looking for
            tracks.AddRange(cachedAlbumTracks);
            albums.Remove(a => cachedAlbumTracks.Any(at => at.AlbumId == a.Id));

            //the Track.Album.AlbumGroup is always null (specifically when pulled from the track object), so it can't be used below
            //therefore the data point is pulled here directly from the album object
            var appearsOnAlbums = albums
                .Where(a => a.AlbumGroup == "appears_on")
                .Select(a => a.Id)
                .ToList();

            //var ppTracks = new ProgressPrinter(Total: Math.Max(albums.Count(), MaxPlaylistSize),
            //                                   Update: (perc, time) => ConsoleWriteAndClearLine(cursorLeft, " -- " + playlistDetailsString + " playlist -- Getting tracks: " + perc + ", " + time + " remaining")
            //                                   );

            //identify tracks in those albums
            var trackIDs = albums.Select(album => spotify.Albums.GetTracks(album.Id).Result)
                //.Where(x => ppTracks.PrintProgress())
                .SelectMany(item => spotify.Paginate(item, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
                .Select(track => track.Id) // this "track" is SimpleTrack rather than FullTrack; need a list of IDs to convert them to FullTrack
                .Distinct()
                .ToList();

            //get the tracks
            var newTracks = (this.GetTracks(trackIDs))
                //ignore tracks by artists outside the spec if this is an "appears on" album, like a compilation
                //this includes tracks that are on split albums, collaborations, and the like
                .Where(t =>
                    !appearsOnAlbums.Contains(t.AlbumId)
                    || t.ArtistNames.Any(artistName => artistNames.Contains(artistName, StringComparer.InvariantCultureIgnoreCase))
                    || t.ArtistIds.Any(artistID => artistIDs.Contains(artistID))
                    )
                ////only include tracks strictly by artists specified
                //.Where(t => t.Artists.Select(a => a.Name).Any(artistName => playlistByArtist.Value.Contains(artistName, StringComparer.InvariantCultureIgnoreCase)))
                .ToList();

            tracks.AddRange(newTracks);

            //placement of cache writing is important
            //this means that no partial albums or partial discographies are written by this method
            //since the playlist generator batches requests, this method writes the cache a maximum of once per playlist
            this.WriteCache();

            return tracks;
        }


        public List<FullTrackDetails> GetTopTracksByArtists(IEnumerable<string> artistNamesOrIDs)
        {

            //TODO scope problem with this ID regex
            var artistURIs = artistNamesOrIDs
                .Where(x => Program.idRegex.Match(x).Success)
                .ToList();

            var artistNames = artistNamesOrIDs
                .Where(x => !artistURIs.Contains(x))
                .ToList();

            var searchArtists = this.GetArtistsByName(artistNames);
            var uriArtists = this.GetArtists(artistURIs);

            var artists = searchArtists.Union(uriArtists).Distinct().ToList();

            var tracks = new List<FullTrackDetails>();

            //if an artist in the cache has been retrieved by *this* method *this* session
            //then we can be assured that the artist's top tracks are already in the cache
            //unfortunately this likely won't occur much
            //which means a call to GetTopTracks *usually* hits the api
            var cachedTopTracks = this.TrackCache.Values
                .Where(t =>
                    t.Source_Top &&
                    t.SessionID == this._sessionID &&
                    artists.Any(a => t.ArtistIds.Contains(a.Id))
                )
                .ToArray();

            if (cachedTopTracks.Any())
            {
                //TODO make sure this worked
                //add the tracks found in the cache to the output, remove the artists from the artists we're looking for
                tracks.AddRange(cachedTopTracks);
                artists.Remove(a => cachedTopTracks.Any(t => t.ArtistIds.Contains(a.Id)));
            }

            //only hit the api for artists that we don't already have top tracks for
            var me = this.spotify.UserProfile.Current().Result;
            var newTracks = artists.Select(artist => this.spotify.Artists.GetTopTracks(artist.Id, new ArtistsTopTracksRequest(me.Country ?? "US")).Result)
                .SelectMany(x => x.Tracks.Take(5))
                .ToArray()
                ;

            var newFullTrackDetails = newTracks.Select(t => new FullTrackDetails(t, artists, this._sessionID, topTrack:true)).ToList();
            this.AddToCache(newFullTrackDetails);

            if (newFullTrackDetails.Any())
                this.WriteCache();

            tracks.AddRange(newFullTrackDetails);

            return tracks;
        }

        public List<FullTrackDetails> GetTracksByPlaylist(IEnumerable<string> playlistIDs)
        {
            //TODO add some kind of caching for playlists? especially since they have that snapshot id
            //and especially since, as is, playlists do not benefit from track caching at all

            var playlists = playlistIDs
                .Select(ID => spotify.Playlists.Get(ID).ResultSafe())
                .Where(p => p != null)
                .ToList();

            var fullTracks = playlists
                .SelectMany(p => spotify.Paginate(p.Tracks, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
                .Select(playableItem => ((FullTrack)playableItem.Track))
                .Distinct()
                .ToList();

            var artists = this.GetArtists(fullTracks);

            var output = fullTracks.Select(t => new FullTrackDetails(t, artists, this._sessionID)).ToList();
            this.AddToCache(output);
            return output;
        }

        private List<FullPlaylist> _usersPlaylists;
        public List<FullPlaylist> GetUsersPlaylists(bool refreshCache = false)
        {
            if (_usersPlaylists != null && !refreshCache)
                return _usersPlaylists;


            _usersPlaylists = (this.spotify.Paginate(spotify.Playlists.CurrentUsers().Result).ToListAsync()).Result
                .Where(p => p.Owner.Id == this.spotify.UserProfile.Current().Result.Id)
                .Select(p => spotify.Playlists.Get(p.Id).Result) //re-get the playlist to convert from SimplePlaylist to FullPlaylist
                .ToList()
                ;

            return _usersPlaylists;
        }
        public List<FullPlaylist> GetUsersPlaylists(string playlistName, string playlistStartString = null)
        {
            if (string.IsNullOrWhiteSpace(playlistName)) return null;

            var playlists = this.GetUsersPlaylists().Where(p =>
                p.Name.Like(playlistName) ||
                p.Name.Like(playlistStartString + playlistName)
                )
                .ToList();
            return playlists;
        }

        public void DownloadPlaylistImage(FullPlaylist playlist, string path)
        {
            var folderPath = System.IO.Path.GetDirectoryName(path);

            if (!System.IO.Directory.Exists(folderPath))
                System.IO.Directory.CreateDirectory(folderPath);

            // .images is sorted by size, so the first is the largest
            var image = playlist.Images.First();

            ImageTools.DownloadFile(image.Url, path);
        }

        public bool UploadPlaylistImage(FullPlaylist playlist, string imagePath)
        {
            var bytes = File.ReadAllBytes(imagePath);
            var file = Convert.ToBase64String(bytes);

            var output = Retry.Do(retryInterval:TimeSpan.FromSeconds(10), maxAttemptCount:3,
                action: () =>
            {
                return this.spotify.Playlists.UploadCover(playlist.Id, file).Result;
            });

            return output;
        }

        public bool Play(string playlistName)
        {
            var playlist = this.GetUsersPlaylists(playlistName).FirstOrDefault();
            if (playlist == null)
                return false;

            if (!this.spotify.Player.GetAvailableDevices().Result.Devices.Any())
            {
                Console.WriteLine("No device available found to play on.");
                return false;
            }

            var success = this.spotify.Player.ResumePlayback(new PlayerResumePlaybackRequest()
            {
                ContextUri = playlist.Uri
            }).Result;

            return success;
        }

        public FullPlaylist GetCurrentPlaylist()
        {
            var playbackContextURI = this.spotify.Player.GetCurrentPlayback()
                .Result?.Context?.Uri ?? string.Empty;
            var playingContextURI = this.spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.All))
                .Result?.Context?.Uri ?? string.Empty;

            if (Debugger.IsAttached && playbackContextURI != playingContextURI)
            {
                Console.WriteLine("Current \"playback\" and \"playing\" context URIs do not match!");
                Console.WriteLine("playback context uri: " + playbackContextURI);
                Console.WriteLine("playing  context uri: " + playingContextURI);
            }

            if (string.IsNullOrWhiteSpace(playbackContextURI) && string.IsNullOrWhiteSpace(playingContextURI))
                return null;
            
            return this.GetUsersPlaylists()
                .Where(p => p.Uri == playbackContextURI || p.Uri == playingContextURI)
                .FirstOrDefault();

        }
    }

}
