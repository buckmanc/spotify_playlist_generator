using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using spotify_playlist_generator.Models;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        private string _trackCachePath = System.IO.Path.Join(Program.AssemblyDirectory, "caches", "track_cache.json");
        private string _artistSearchCachePath = System.IO.Path.Join(Program.AssemblyDirectory, "caches", "artist_search_cache.json");
        private string _albumTracksCachePath = System.IO.Path.Join(Program.AssemblyDirectory, "caches", "album_tracks_cache.json");
        private Guid _sessionID = Guid.NewGuid();

        public void Dispose()
        {
            WriteTrackCache();
            WriteArtistSearchCache();
        }

        // TODO reduce your duplicate code here
        private void WriteTrackCache()
        {

            //skip writing if we haven't read
            //checking against the backing property to avoid triggering a read
            if (this._trackCache != null && this._trackCache.Any() && this.DirtyTrackCache)
            {
                if (Program.Settings._VerboseDebug)
                {
                    Console.WriteLine();
                    Console.WriteLine("writing track cache");
                }

                var dir = System.IO.Path.GetDirectoryName(this._trackCachePath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                using var stream = File.Create(this._trackCachePath);
                System.Text.Json.JsonSerializer.Serialize(stream, this.TrackCache);
                stream.Dispose();
                this.DirtyTrackCache = false;
            }
            else if (Program.Settings._VerboseDebug)
            {
                Console.WriteLine();
                Console.WriteLine("skipping track cache write; no new changes");
            }
        }

        private void WriteArtistSearchCache()
        {

            //skip writing if we haven't read
            //checking against the backing property to avoid triggering a read
            if (this._artistSearchCache != null && this._artistSearchCache.Any())// && this.DirtyArtistSearchCache)
            {
                if (Program.Settings._VerboseDebug)
                {
                    Console.WriteLine();
                    Console.WriteLine("writing artist search cache");
                }

                var dir = System.IO.Path.GetDirectoryName(this._artistSearchCachePath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                using var stream = File.Create(this._artistSearchCachePath);
                System.Text.Json.JsonSerializer.Serialize(stream, this.ArtistSearchCache);
                stream.Dispose();
                //this.DirtyArtistSearchCache = false;
            }
            else if (Program.Settings._VerboseDebug)
            {
                Console.WriteLine();
                Console.WriteLine("skipping artist search cache write; no new changes");
            }
        }

        private void WriteAlbumTracksCache()
        {

            //skip writing if we haven't read
            //checking against the backing property to avoid triggering a read
            if (this._albumTracksCache != null && this._albumTracksCache.Any())
            {
                if (Program.Settings._VerboseDebug)
                {
                    Console.WriteLine();
                    Console.WriteLine("writing album tracks cache");
                }

                var dir = System.IO.Path.GetDirectoryName(this._albumTracksCachePath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                using var stream = File.Create(this._albumTracksCachePath);
                System.Text.Json.JsonSerializer.Serialize(stream, this.AlbumTracksCache);
                stream.Dispose();
                //this.DirtyalbumTracksCache = false;
            }
            else if (Program.Settings._VerboseDebug)
            {
                Console.WriteLine();
                Console.WriteLine("skipping album tracks cache write; no new changes");
            }
        }

        private void ReadTrackCache()
        {
            //TODO deserialize the cache here
            if (System.IO.File.Exists(this._trackCachePath))
            {
                if (Program.Settings._VerboseDebug)
                {
                    Console.WriteLine("reading track cache");
                    Console.WriteLine();
                }
                using var stream = File.OpenRead(this._trackCachePath);
                this._trackCache = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Concurrent.ConcurrentDictionary<string, FullTrackDetails>>(stream);
                stream.Dispose();
                this.DirtyTrackCache = false;
            }
        }

        private void ReadArtistSearchCache()
        {
            //TODO deserialize the cache here
            if (System.IO.File.Exists(this._artistSearchCachePath))
            {
                if (Program.Settings._VerboseDebug)
                {
                    Console.WriteLine("reading artist search cache");
                    Console.WriteLine();
                }
                using var stream = File.OpenRead(this._artistSearchCachePath);
                this._artistSearchCache = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Concurrent.ConcurrentDictionary<string, List<FullArtist>>>(stream);
                stream.Dispose();
                //this.DirtyArtistSearchCache = false;
            }
        }

        private void ReadAlbumTracksCache()
        {
            //TODO deserialize the cache here
            if (System.IO.File.Exists(this._albumTracksCachePath))
            {
                if (Program.Settings._VerboseDebug)
                {
                    Console.WriteLine("reading album tracks cache");
                    Console.WriteLine();
                }
                using var stream = File.OpenRead(this._albumTracksCachePath);
                this._albumTracksCache = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Concurrent.ConcurrentDictionary<string, IEnumerable<string>>>(stream);
                stream.Dispose();
                //this.DirtyAlbumTracksCache = false;
            }
        }

        public void RefreshSpotifyClient()
        {
            if (Program.Settings._VerboseDebug)
                Console.WriteLine("Updating access token...");

            //TODO better scope management
            var accessToken = Program.UpdateTokens().Result;

            if (Program.Settings._VerboseDebug)
            {
                Console.WriteLine("Access Token as of " + DateTime.Now.ToShortDateTimeString() + ": ");
                Console.WriteLine(accessToken?.Substring(0, 20) ?? String.Empty);
            }

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

        private PrivateUser _currentUser;

        public PrivateUser CurrentUser
        {
            get
            {
                _currentUser ??= this.spotify.UserProfile.Current().Result;
                return _currentUser; 
            }
        }



        private System.Collections.Concurrent.ConcurrentDictionary<string, FullArtist> _artistCache = new();
        /// <summary>
        /// Returns artists from the local cache or the Spotify API.
        /// </summary>
        /// <param name="artistIDs"></param>
        /// <returns>Returns a list of FullArtist</returns>
        public List<FullArtist> GetArtists(IEnumerable<string> artistIDs)
        {
            artistIDs = artistIDs.Distinct().ToArray();

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
                .Where(t => t != null)
                .SelectMany(t => t.Artists.Select(a => a.Id))
                .Distinct()
                .ToArray();

            return this.GetArtists(artistIDs);
        }

        //do not save this cache
        private System.Collections.Concurrent.ConcurrentDictionary<string, string> _artistByNameCache = new();
        public List<FullArtist> GetArtistsByName(List<string> artistNames)
        {

            artistNames = artistNames
                .Distinct()
                .Where(x => x.Trim() != "*")
                .ToList();

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



            var artists = new List<FullArtist>();
            var cachedArtists = this.ArtistSearchCache.TryGetValues(artistNames).SelectMany(x => x).ToArray();
            if (cachedArtists.Any())
            {
                artistNames.RemoveRange(this.ArtistSearchCache.Keys);
                artists.AddRange(cachedArtists);
            }

            foreach (var artistNamesChunk in artistNames.ChunkBy(50))
                Retry.Do((Exception ex) =>
                {
                    var artistsChunk = artistNamesChunk.Distinct()
                        .Select(artistName => new SearchRequest(SearchRequest.Types.Artist, artistName.Trim()) { Limit = 40 })
                        .Select(request => spotify.Search.Item(request).Result)
                        .SelectMany(item => spotify.Paginate(item.Artists, s => s.Artists, new WaitPaginator(WaitTime: 500))
                            .ToListAsync(Take: 40).Result // would like this to be 1, but the sought for artists are missing with less than 40
                            .Where(artist => artistNames.Any(n => artist.Name.Like(n)))
                            )
                        //.Where(artist => ppArtists.PrintProgress() && artist != null)
                        .Where(artist => artist != null && artist.Images.Any())
                        //.OrderBy(artist => System.Array.IndexOf(artistNames.ToArray(), artist.Name)) //TODO make this case insensitive first
                        .OrderBy(artist => artist.Name)
                        .OrderByDescending(artist => artist.Popularity)
                        .ToList();

                    artists.AddRange(artistsChunk);
                });

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

            artists = artists.Distinct().ToList();

            var toCache = artistNames.Select(x => new
            {
                ArtistName = x,
                Artists = artists.Where(a => a.Name.Like(x)).ToList()
            })
                .Where(x => x.Artists?.Any() ?? false)
                .ToDictionary(x => x.ArtistName, x => x.Artists);

            this.ArtistSearchCache.AddRange(toCache);
            WriteArtistSearchCache();

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
                // TODO improve performance
                // making two attempts to find the key in the cache per item

                var existingTracks = tracks.Where(t => this.TrackCache.ContainsKey(t.TrackId)).ToArray();
                existingTrackIDs = existingTracks.Select(t => t.TrackId).ToList();

                foreach (var existingTrack in existingTracks)
                {
                    //TODO make sure this actually modifies the stored track
                    if (existingTrack.Source_AllTracks)
                    {
                        this.TrackCache[existingTrack.TrackId].Source_AllTracks = true;
                    }
                    if (existingTrack.Source_Liked)
                    {
                        this.TrackCache[existingTrack.TrackId].LikedAt = existingTrack.LikedAt;
                        this.TrackCache[existingTrack.TrackId].Source_Liked = true;
                    }
                    if (existingTrack.Source_Top)
                    {
                        this.TrackCache[existingTrack.TrackId].Source_Top = true;
                    }

                }
            }

            this.DirtyTrackCache = true;
            foreach (var track in tracks.Where(t => !existingTrackIDs.Contains(t.TrackId)))
                this.TrackCache.TryAdd(track.TrackId, track);
        }

        private bool DirtyTrackCache = false;
        //the track ID as dictionary key is redundant but may offer performance benefits
        private System.Collections.Concurrent.ConcurrentDictionary<string, FullTrackDetails> _trackCache;

        public System.Collections.Concurrent.ConcurrentDictionary<string, FullTrackDetails> TrackCache
        {
            get
            {
                //simple way to lazy load the cache
                if (_trackCache == null)
                {
                    ReadTrackCache();
                    _trackCache ??= new();
                }

                return _trackCache;
            }
        }

        private System.Collections.Concurrent.ConcurrentDictionary<string, List<FullArtist>> _artistSearchCache;

        public System.Collections.Concurrent.ConcurrentDictionary<string, List<FullArtist>> ArtistSearchCache
        {
            get
            {
                //simple way to lazy load the cache
                if (_artistSearchCache == null)
                {
                    ReadArtistSearchCache();
                    _artistSearchCache ??= new();
                }

                return _artistSearchCache;
            }
        }

        //the track ID as dictionary key is redundant but may offer performance benefits
        private System.Collections.Concurrent.ConcurrentDictionary<string, IEnumerable<string>> _albumTracksCache;

        public System.Collections.Concurrent.ConcurrentDictionary<string, IEnumerable<string>> AlbumTracksCache
        {
            get
            {
                //simple way to lazy load the cache
                if (_albumTracksCache == null)
                {
                    ReadAlbumTracksCache();
                    _albumTracksCache ??= new();
                }

                return _albumTracksCache;
            }
        }

        /// <summary>
        /// Returns tracks from the local cache or the Spotify API.
        /// </summary>
        /// <param name="trackIDs"></param>
        /// <returns>Returns a list of FullTrack</returns>
        public List<FullTrackDetails> GetTracks(IEnumerable<string> trackIDs, bool Source_AllTracks = false)
        {
            trackIDs = trackIDs.Distinct().ToArray();

            var apiTracks = new List<FullTrackDetails>();
            var cacheTracks = new List<FullTrackDetails>();
            var trackIdApiQueue = new List<string>();

            //check the local cache for any items that we've already retrieved from the api
            //queue up any items that aren't in the local cache
            foreach (var trackID in trackIDs)
            {
                FullTrackDetails foundTrack;
                if (this.TrackCache.TryGetValue(trackID, out foundTrack))
                    cacheTracks.Add(foundTrack);
                else
                    trackIdApiQueue.Add(trackID);
            }

            //pull any remaining items from the api
            foreach (var idChunk in trackIdApiQueue.ChunkBy(50))
                Retry.Do((Exception ex) =>
                {
                    if (ex?.ToString()?.Like("*access token expired*") ?? false)
                        this.RefreshSpotifyClient();

                    var chunkTracks = (this.spotify.Tracks.GetSeveral(new TracksRequest(idChunk))).Result.Tracks
                        .Where(a => a != null)
                        .ToList();

                    var artists = this.GetArtists(chunkTracks);

                    var trackDetails = chunkTracks.Select(t => new FullTrackDetails(t, artists, this._sessionID, allTracksTrack:Source_AllTracks)).ToList();

                    apiTracks.AddRange(trackDetails);

                    //cache items
                    this.AddToCache(trackDetails);
                }, maxAttemptCount: 4);

            //make sure existing cached tracks get updated appropriately
            //otherwise liked tracks, for example, get left out of big sets
            //let me tell you, that is a difficult bug to find
            if (Source_AllTracks && cacheTracks.Any(t => !t.Source_AllTracks))
            {
                cacheTracks.ForEach(t => t.Source_AllTracks = true);
                this.AddToCache(cacheTracks);
            }

            var output = cacheTracks.Union(apiTracks).ToList();

            return output;
        }


        private List<FullTrackDetails> _likedTracks;
        private bool _getLikedTracksRunning;
        public List<FullTrackDetails> LikedTracks
        {
            get
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

                var errorTracks = savedTracks.Where(s => s.Track is null).ToArray();
                if (errorTracks.Any())
                    Console.WriteLine("Found " +
                        errorTracks.Count().ToString("#,##0") +
                        " phantom tracks liked between " +
                        errorTracks.Min(t => t.AddedAt).ToShortDateString() +
                        " and " + errorTracks.Max(t => t.AddedAt).ToShortDateString() +
                        "."
                        );

                savedTracks.RemoveRange(errorTracks);

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
        }
        public List<FullTrackDetails> GetTracksByArtists(IList<string> artistNamesOrIDs)
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
            artistIDs = artistIDs.Distinct().ToList();

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
            //TODO add caching here for even better failure recovery
            var albums = Retry.Do((Exception ex) =>
            {
                if (ex?.ToString()?.Like("*access token expired*") ?? false)
                    this.RefreshSpotifyClient();

                return artistIDs.Select(artistID => spotify.Artists.GetAlbums(artistID, new ArtistsAlbumsRequest() { Limit = 50, Market = this.CurrentUser.Country ?? "US" }).Result)
                        //.Where(x => ppAlbums.PrintProgress())
                        .SelectMany(item => spotify.Paginate(item, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
                        .OrderBy(album => album.ReleaseDate)
                        .ToList();
            }, maxAttemptCount: 4);

            //remove a particular album which is 1) a duplicate and 2) behaves erratically
            //only remove if the set contains both this album and its pair
            if (albums.Any(album => album.Id == "3jRsMOSeikuwpE9Q75Ij7I" && albums.Any(album => album.Id == "3BhDAfxJZ7Ng8oNGy3XS1v")))
            {
                albums.Remove(albums.Where(album => album.Id == "3jRsMOSeikuwpE9Q75Ij7I").SingleOrDefault());
            }

            //the Track.Album.AlbumGroup is always null (specifically when pulled from the track object), so it can't be used below
            //therefore the data point is pulled here directly from the album object
            var appearsOnAlbums = albums
                .Where(a => a.AlbumGroup == "appears_on")
                .Select(a => a.Id)
                .ToList();

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
            if (cachedAlbumTracks.Any())
            {
                tracks.AddRange(cachedAlbumTracks);
                albums.Remove(a => cachedAlbumTracks.Any(at => at.AlbumId == a.Id));
            }

            //var ppTracks = new ProgressPrinter(Total: Math.Max(albums.Count(), MaxPlaylistSize),
            //                                   Update: (perc, time) => ConsoleWriteAndClearLine(cursorLeft, " -- " + playlistDetailsString + " playlist -- Getting tracks: " + perc + ", " + time + " remaining")
            //                                   );

            var albumIDs = albums.Select(a => a.Id).Distinct().ToList();

            //pull tracks from the api
            var trackIDs = new List<string>();

            var cachedTrackIDs = this.AlbumTracksCache.TryGetValues(albumIDs).SelectMany(x => x).ToArray();
            if (cachedTrackIDs.Any())
            {
                albumIDs.RemoveRange(this.AlbumTracksCache.Keys);
                trackIDs.AddRange(cachedTrackIDs);
            }
            // pull any remaining items from the api
            // GetSeveral will throw an error for an empty list
            foreach (var albumIDChunk in albumIDs.ChunkBy(20))
                Retry.Do((Exception ex) =>
                {
                    if (ex?.ToString()?.Like("*access token expired*") ?? false)
                        this.RefreshSpotifyClient();

                    //create this weird anonymous type as we need to preserve the album/track relationship temporarily 
                    var albumsWithTracks = this.spotify.Albums.GetSeveral(new AlbumsRequest(albumIDChunk)).Result
                        //.Where(x => ppTracks.PrintProgress())
                        .Albums
                        .Select(album => new
                        {
                            AlbumID = album.Id,
                            TrackIDs = spotify.Paginate(album.Tracks, new WaitPaginator(WaitTime: 500))
                                .ToListAsync()
                                .Result
                                .Select(t => t.Id)
                        })
                        .ToArray();

                    var chunkTrackIDs = albumsWithTracks
                        .SelectMany(x => x.TrackIDs) // this "track" is SimpleTrack rather than FullTrack; need a list of IDs to convert them to FullTrack
                        .Distinct()
                        .ToArray();

                    //cache away
                    foreach(var albumWithTracks in albumsWithTracks)
                        this.AlbumTracksCache.TryAdd(albumWithTracks.AlbumID, albumWithTracks.TrackIDs);

                    this.WriteAlbumTracksCache();

                    trackIDs.AddRange(chunkTrackIDs);
                }, maxAttemptCount:4);

            //get the tracks
            var newTracks = this.GetTracks(trackIDs, Source_AllTracks: true).ToList();

            tracks.AddRange(newTracks);

            //ignore tracks by artists outside the spec if this is an "appears on" album, like a compilation
            //this includes tracks that are on split albums, collaborations, and the like
            tracks = tracks
                .Where(t =>
                    !appearsOnAlbums.Contains(t.AlbumId)
                    || t.ArtistNames.Any(artistName => artistNames.Any(n => artistName.Like(n)))
                    || t.ArtistIds.Any(artistID => artistIDs.Contains(artistID))
                    )
                ////only include tracks strictly by artists specified
                //.Where(t => t.Artists.Select(a => a.Name).Any(artistName => playlistByArtist.Value.Contains(artistName, StringComparer.InvariantCultureIgnoreCase)))
                .ToList();

            var inputWithIndex = artistNamesOrIDs.Select(x => new
            {
                value = x,
                index = artistNamesOrIDs.IndexOf(x),
            }).ToArray();

            //restore the original order in which artists and whatnot were provided
            tracks = tracks.OrderBy(t =>
            //look for the index of a matching id parameter, then for matching artist parameters
            //sort by the first/primary artist if they are in the spec, otherwise look at all artists
            //this only matters when both/all artists are deliberately requested, which is very possible
            inputWithIndex.Where(x => t.ArtistIds.Contains(x.value)).Select(x => x?.index).FirstOrDefault() ??
            inputWithIndex.Where(x => t.ArtistNames.FirstOrDefault()?.Like(x.value) ?? false).Select(x => x?.index).FirstOrDefault() ??
            inputWithIndex.Where(x => t.ArtistNames.Any(a => a.Like(x.value))).Select(x => x?.index).FirstOrDefault() ??
            inputWithIndex.Length + 1 //sort sort failures to the end I guess
            )
                .ThenBy(t => t.ReleaseDate)
                .ThenBy(t => t.AlbumName)
                .ThenBy(t => t.TrackNumber)
                .ToList();

            //placement of cache writing is important
            //this means that no partial albums or partial discographies are written by this method
            //since the playlist generator batches requests, this method writes the cache a maximum of once per playlist
            this.WriteTrackCache();

            //now that the FullTrackDetails have been logged to the track cache we can remove them from this cache
            //this cache is only helpful for failed runs
            this.AlbumTracksCache.TryRemove(tracks.Select(t => t.AlbumId).Distinct());

            return tracks;
        }
        public List<FullTrackDetails> GetTracksByAlbum(IList<string> albumNamesOrIDs)
        {
            //shift functionality provided by this method into GetTracksByArtist
            //then you could avoid pulling tracks for albums you're not interested in

            //TODO scope problem with this ID regex
            var albumIDs = albumNamesOrIDs
                .Where(x => Program.idRegex.Match(x).Success)
                .ToList();

            var names = albumNamesOrIDs
                .Where(x => !albumIDs.Contains(x))
                .ToList();

            var tracks = new List<FullTrackDetails>();

            //if an album in the cache has been retrieved by All Tracks 
            //then we can be assured that the full album is already in the cache
            //if an album tracklist is changed after this though the cache will need to be cleared to get the update
            var cacheIDMatches = this.TrackCache.Values.Where(t =>
                    albumIDs.Any(x => t.AlbumStringIsMatch(x))
                    )
                .ToList();

            //need to process IDs and names separately here so we know which names to remove
            //previously these were re-searched even if found in the cache
            var cacheNameMatches = names.Select(n => new
            {
                name = n,
                matchedCachedAlbumTracks = this.TrackCache.Values
                .Where(t =>
                    albumIDs.Any(x => t.AlbumStringIsMatch(x))
                )
                .ToArray()
            })
                .Where(x => x.matchedCachedAlbumTracks.Any())
                .ToList();

            var cachedAlbumTracks = cacheIDMatches.Union(cacheNameMatches.SelectMany(x => x.matchedCachedAlbumTracks)).ToArray();

            //add the tracks found in the cache to the output, remove the albums from the album we're looking for
            if (cachedAlbumTracks.Any())
            {
                tracks.AddRange(cachedAlbumTracks);
                albumIDs.Remove(ID => cacheIDMatches.Any(at => at.AlbumId == ID));
                names.Remove(n => cacheNameMatches.Any(x => x.name == n));
            }

            //pull tracks from the api
            var idAlbumTrackIDs = new List<string>();

            // GetSeveral will throw an error for an empty list
            foreach (var albumIDChunk in albumIDs.ChunkBy(20))
                Retry.Do((Exception ex) =>
                {
                    if (ex?.ToString()?.Like("*access token expired*") ?? false)
                        this.RefreshSpotifyClient();

                    var chunkAlbums = this.spotify.Albums.GetSeveral(new AlbumsRequest(albumIDChunk)).Result
                        .Albums
			.ToList();

		    var errorAlbums = chunkAlbums.Where(a => a == null || a.Tracks == null).ToArray();

		    if (errorAlbums.Any())
			    Console.WriteLine("Found " +
					    errorAlbums.Length.ToString("#,##0") +
					    " albums with phantom tracks: " +
					    errorAlbums.Select(a => (a?.Id ?? "null") + " " + ( a?.Name ?? "null")).Join(", ")
					    );

		    chunkAlbums.RemoveRange(errorAlbums);

		    var chunkIDAlbumTrackIDs = chunkAlbums 
                            .SelectMany(album => spotify.Paginate(album.Tracks, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
                            .Select(t => t.Id) // this "track" is SimpleTrack rather than FullTrack; need a list of IDs to convert them to FullTrack
                            .ToList()
                            ;

                    idAlbumTrackIDs.AddRange(chunkIDAlbumTrackIDs);
                }, maxAttemptCount:4);
            var idAlbumTracks = this.GetTracks(idAlbumTrackIDs);

            var artistNames = names
                .Select(x => x.Split(Program.dashes, 2, StringSplitOptions.TrimEntries).FirstOrDefault())
                .Distinct()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            //counting on the caching in this method for caching benefits
            var nameAlbumTracks = this.GetTracksByArtists(artistNames)
                .Where(t =>
                    names.Any(x => t.AlbumStringIsMatch(x))
                    )
                .ToList();

            tracks.AddRange(idAlbumTracks);
            tracks.AddRange(nameAlbumTracks);

            //restore provided album order
            tracks = tracks
                .OrderBy(t => albumNamesOrIDs.IndexOf(albumNamesOrIDs.First(x => t.AlbumStringIsMatch(x))))
                // the below is duplicate logic unfort
                .ThenBy(t => t.ReleaseDate)
                .ThenBy(t => t.ArtistNames.FirstOrDefault())
                .ThenBy(t => t.AlbumName)
                .ThenBy(t => t.TrackNumber)
                .ToList();

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
            var newTracks = artists.Select(artist => this.spotify.Artists.GetTopTracks(artist.Id, new ArtistsTopTracksRequest(this.CurrentUser.Country ?? "US")).Result)
                .SelectMany(x => x.Tracks.Take(5))
                .ToList()
                ;

            var errorTracks = newTracks.Where(t => t is null).ToArray();
            if (errorTracks.Any())
                Console.WriteLine("Found " +
                    errorTracks.Count().ToString("#,##0") +
                    " phantom tracks");

            newTracks.RemoveRange(errorTracks);

            var newFullTrackDetails = newTracks.Select(t => new FullTrackDetails(t, artists, this._sessionID, topTrack: true)).ToList();
            this.AddToCache(newFullTrackDetails);

            if (newFullTrackDetails.Any())
                this.WriteTrackCache();

            tracks.AddRange(newFullTrackDetails);

            return tracks;
        }

        public List<FullTrackDetails> GetTracksByPlaylist(IEnumerable<string> playlistIDsOrNames)
        {
            //name searching only works for the users's followed playlists
            //too hard to guarantee we found the right playlist otherwise

            //TODO add some kind of caching for playlists? especially since they have that snapshot id
            //and especially since, as is, playlists do not benefit from track caching at all

            //TODO scope problem with this ID regex
            var playlistIDs = playlistIDsOrNames
                .Where(x => Program.idRegex.Match(x).Success)
                .ToList();

            var playlistNames = playlistIDsOrNames
                .Where(x => !playlistIDs.Contains(x))
                .ToList();

            var idPlaylists = playlistIDs
                .Select(ID => spotify.Playlists.Get(ID).ResultSafe())
                .Where(p => p != null)
                .ToList();

            var namePlaylists = new List<FullPlaylist>();

            //don't call user playlists if we don't have to
            if (playlistNames.Any())
                namePlaylists = this.GetFollowedPlaylists().Where(p => playlistIDsOrNames.Any(x => 
                p.Name.Like(x) ||
                p.Name.Like(Program.Settings._StartPlaylistsWith + x)
                )).ToList();

            var playlists = idPlaylists.Union(namePlaylists);

            var fullTracks = playlists
                .SelectMany(p => spotify.Paginate(p.Tracks, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
                .Select(playableItem => ((FullTrack)playableItem.Track))
                .Distinct()
                .ToList();

            var artists = this.GetArtists(fullTracks);


            var errorTracks = fullTracks.Where(t => t is null).ToArray();
            if (errorTracks.Any())
                Console.WriteLine("Found " +
                    errorTracks.Count().ToString("#,##0") +
                    " phantom tracks");

            fullTracks.RemoveRange(errorTracks);

            var output = fullTracks.Select(t => new FullTrackDetails(t, artists, this._sessionID)).ToList();
            this.AddToCache(output);
            return output;
        }

        private System.Collections.Concurrent.ConcurrentBag<FullPlaylist> _followedPlaylists;

        private bool _GetFollowedPlaylistsRunning;
        public System.Collections.Concurrent.ConcurrentBag<FullPlaylist> GetFollowedPlaylists(bool refreshCache = false)
        {

            //threadsafe this sucker
            //well *mostly* threadsafe
            while (_GetFollowedPlaylistsRunning)
                System.Threading.Thread.Sleep(1000);

            if (_followedPlaylists != null && !refreshCache)
                return _followedPlaylists;

            _GetFollowedPlaylistsRunning = true;

            _followedPlaylists = this.spotify.Paginate(spotify.Playlists.CurrentUsers().Result).ToListAsync().Result
                .Select(p => Retry.Do(() =>
                {
                    //errors encountered here:
                    //timeout
                    //not found
                    return spotify.Playlists.Get(p.Id).Result;
                })) //re-get the playlist to convert from SimplePlaylist to FullPlaylist
                .ToConcurrentBag()
                ;

            _GetFollowedPlaylistsRunning = false;

            return _followedPlaylists;
        }

        public List<FullPlaylist> GetFollowedPlaylists(string playlistName, string playlistStartString = null)
        {
            if (string.IsNullOrWhiteSpace(playlistName)) return null;

            var playlists = this.GetFollowedPlaylists().Where(p =>
                p.Name.Like(playlistName) ||
                p.Name.Like(playlistStartString + playlistName)
                )
                .ToList();

            return playlists;
        }

        public List<FullPlaylist> GetUsersPlaylists(bool refreshCache = false)
        {
            return this.GetFollowedPlaylists(refreshCache)
                    .Where(p => p.Owner.Id == this.CurrentUser.Id)
                    .ToList();
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

            var success = false;
            var attempts = 0;
            var maxAttempts = 6;

            do
            {
                attempts += 1;

                var dropPercBy = 0.1;
                var sizeRatio = 1 - dropPercBy;
                var attemptRatio = 1 - (sizeRatio * (attempts - 1));

                // shrink the image per subsequent attempt
                if (attempts > 1)
                {
                    if (Program.Settings._VerboseDebug)
                        Console.WriteLine("Image upload failed. Making " + attempts.AddOrdinal() + " attempt.");

                    using (var img = SixLabors.ImageSharp.Image.Load(imagePath))
                    {

                        // drop the size by x percent every attempt
                        var resizeSize = new Size((int)Math.Round(img.Width * sizeRatio, 0), (int)Math.Round(img.Height * sizeRatio, 0));

                        img.Mutate(i => i.Resize(resizeSize));

                        var jpgEncoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder();
                        jpgEncoder.ColorType = SixLabors.ImageSharp.Formats.Jpeg.JpegColorType.Rgb;

                        // drop the encoding quality every attempt
                        //jpgEncoder.Quality = (int)Math.Round((jpgEncoder.Quality ?? 75) * attemptRatio, 0);

                        if (attempts == maxAttempts)
                        {
                            // could take desparate measures here
                            // but probably better to just increase maxAttempts instead
                        }

                        img.SaveAsJpeg(imagePath, jpgEncoder);
                    }
                }

                // attempt to upload the image
                success = UploadPlaylistImage_Attempt(playlist, imagePath);

            }
            while (!success && attempts <= maxAttempts);

            return success;
        }

        private bool UploadPlaylistImage_Attempt(FullPlaylist playlist, string imagePath)
        {
            var bytes = File.ReadAllBytes(imagePath);
            var kb = bytes.Length / 1024;

            // if the image is too large, don't even try
            if (kb >= 256)
            {
                if (Program.Settings._VerboseDebug)
                    Console.WriteLine("Image filesize too large: " + kb.ToString("#,##0") + " kilobytes.");
                return false;
            }

            var fileString = Convert.ToBase64String(bytes);

            var success = false;
            try
            {
                success = this.spotify.Playlists.UploadCover(playlist.Id, fileString).Result;
            }
            catch (Exception ex)
            {
                var exString = ex.ToString();

                //all these errors *seem* to indicate some kind of problem with the image
                //one know problem is file size > 256 kb. not sure what the others are, but they seem to be dependant on the file
                if (
                    exString.Contains("broken pipe") ||
                    exString.Contains("SpotifyAPI.Web.APIException") ||
                    exString.Contains("Error while copying content to a stream") ||
                    exString.Contains("An established connection was aborted ")

                )
                    return false;
                else
                    throw;
            }
            return success;

        }
        private bool DeviceCheck()
        {
            if (!this.spotify.Player.GetAvailableDevices().Result.Devices.Any())
            {
                Console.WriteLine("No device available found to play on.");
                return false;
            }
            return true;
        }

        public bool Play(string playlistName)
        {
            var playlist = (!string.IsNullOrWhiteSpace(playlistName) ? this.GetFollowedPlaylists(playlistName).FirstOrDefault() : null);

            if (!this.DeviceCheck())
                return false;

            var success = false;

            if (playlist != null)
            {
                success = this.spotify.Player.ResumePlayback(new PlayerResumePlaybackRequest()
                {
                    ContextUri = playlist.Uri
                }).Result;
                Console.WriteLine();
                Console.WriteLine("|> " + this.GetCurrentTrack()?.PrettyString() ?? " unknown track");
            }
            else if (this.spotify.Player.GetCurrentPlayback().Result.IsPlaying)
            {
                success = this.spotify.Player.PausePlayback().Result;
                Console.WriteLine();
                Console.WriteLine("||");
            }
            else
            {
                success = this.spotify.Player.ResumePlayback().Result;
                Console.WriteLine();
                Console.WriteLine("|> " + this.GetCurrentTrack()?.PrettyString() ?? "unknown track");
            }

            return success;
        }

        public bool SkipNext()
        {
            return Skip(true);
        }

        public bool SkipPrevious()
        {
            return Skip(false);
        }

        private bool Skip(bool next)
        {

            if (!this.DeviceCheck())
                return false;

            var oldTrack = this.GetCurrentTrack();
            var output = false;
            if (next)
                output = this.spotify.Player.SkipNext().Result;
            else
                output = this.spotify.Player.SkipPrevious().Result;

            //wait for the CurrentTrack to actually be updated for the track skip
            FullTrack newTrack = null;
            var i = 0;
            do
            {
                if (newTrack != null)
                    System.Threading.Thread.Sleep(i <= 10 ? 500 : 1000);

                newTrack = this.GetCurrentTrack();
                i += 1;
            } while (newTrack.Id == oldTrack.Id && i < 20);

            var spaceCount = Math.Min(oldTrack.PrettyString().Length, newTrack.PrettyString().Length) / 2;

            Console.WriteLine();
            if (next)
            {
                Console.WriteLine(oldTrack.PrettyString());
                Console.WriteLine("↓↓".PadLeft(spaceCount));
                Console.WriteLine(newTrack.PrettyString());
            }
            else
            {
                Console.WriteLine(newTrack.PrettyString());
                Console.WriteLine("↑↑".PadLeft(spaceCount));
                Console.WriteLine(oldTrack.PrettyString());
            }
            return output;
        }

        public bool LikeCurrent(bool like)
        {
            var track = this.GetCurrentTrack();
            if (track == null) return false;

            var liked = this.spotify.Library.CheckTracks(new LibraryCheckTracksRequest(new List<string>() { track.Id })).Result.FirstOrDefault();

            if (like && liked)
                Console.WriteLine("Already liked " + track.PrettyString());
            else if (!like && !liked)
                Console.WriteLine("Not currently liked " + track.PrettyString());
            else if (!like && this.spotify.Library.RemoveTracks(new LibraryRemoveTracksRequest(new List<string>() { track.Id })).Result)
            {
                Console.WriteLine("💔 Unliked " + track.PrettyString());
                return true;
            }
            else if (like && this.spotify.Library.SaveTracks(new LibrarySaveTracksRequest(new List<string>() { track.Id })).Result)
            {
                Console.WriteLine("💖  Liked " + track.PrettyString());
                return true;
            }

            return false;
        }

	public void PrintCurrent(){
                Console.WriteLine("|> " + this.GetCurrentTrack()?.PrettyString() ?? "unknown track");

	}

        public FullPlaylist GetCurrentPlaylist()
        {
            //TODO pick the more reliable of the two
            //or if both are reliable, clean up to reduce calls
            var playbackContextURI = this.GetCurrentlyPlaying()?.Context?.Uri;

            if (string.IsNullOrWhiteSpace(playbackContextURI))
                return null;

	        var ID = playbackContextURI.Split(":").Last();
	        return this.spotify.Playlists.Get(ID).ResultSafe();
        }

        public FullTrack GetCurrentTrack(bool warn = false)
        {
            var playbackItem = this.GetCurrentlyPlaying()?.Item;

            var output = playbackItem as FullTrack;
	    if (warn && output == null)
                Console.WriteLine("Could not get current track.");

	    return output;
        }

        private CurrentlyPlaying GetCurrentlyPlaying()
        {
            //TODO do we need to check GetAvailableDevices here? does it throw an error if nothing is playing?

            //playback contains playing but ALSO player state stuff
            //var playback = this.spotify.Player.GetCurrentPlayback()
            //    .Result;
            var playing = this.spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.All))
                .Result;

            return playing;
        }
    }

}
