using System;
using SpotifyAPI.Web;
using IniParser;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace spotify_playlist_generator
{
    partial class Program
    {

        private static class Settings
        {
            public static string _configFolder;
            public static string _PlaylistFolderPath;
            public static string _CommentString = "#"; //TODO consider pulling this from a config file
            public static bool _NewPlaylistsPrivate;
            public static bool _RecreatePlaylists;
            public static bool _DeleteOrphanedPlaylists;
            public static bool _VerboseDebug = false;
            public static bool _TestValues = false;
        }

        public static string AssemblyDirectory
        {
            get
            {
                //https://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in
                string loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
                //UriBuilder uri = new UriBuilder(loc); //causes error on linux
                //string path = Uri.UnescapeDataString(uri.Path);
                //return System.IO.Path.GetDirectoryName(path);
                return System.IO.Path.GetDirectoryName(loc);
            }
        }

        static void Main(string[] args)
        {
            //hop into async land
            MainAsync(args).Wait();
        }
        static async System.Threading.Tasks.Task MainAsync(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("Welcome to C# Spotify Playlist Generator! Please don't expect too much, but also be impressed.");

            //default to looking for settings in the same dir as the program
            if (args.Any() && System.IO.Directory.Exists(args[0]))
                Settings._configFolder = args[0];
            else
                Settings._configFolder = AssemblyDirectory;


            //TODO remove uses of .Result where practical, try to actually get some benefit from async calls
            //don't go overboard though
            //TODO note where you did weird casts and stuff because of the API library

            GetConfig();
            var accessToken = await UpdateTokens();

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

            var spotify = new SpotifyClient(config);



            var me = await spotify.UserProfile.Current();
            Console.WriteLine($"Hello there, {me.DisplayName}");
            Console.WriteLine("----------------------");
            Console.WriteLine();

            //get various playlist definitions
            //that is, a name and a list of tracks
            var playlistBreakdowns = new Dictionary<string, List<FullTrack>>();
            playlistBreakdowns.AddRange(await GetLikesByArtistPlaylistBreakdowns(spotify));
            playlistBreakdowns.AddRange(await GetFullArtistDiscographyBreakdowns(spotify));

            //do work!
            await UpdatePlaylists(spotify, playlistBreakdowns);



            Console.WriteLine();
            Console.WriteLine("All done, get jammin'!");
        }

        //static async System.Threading.Tasks.Task GetConfig()
        static void GetConfig()
        {
            var configIniPath = System.IO.Path.Join(Settings._configFolder, "config.ini");
            var iniParser = new FileIniDataParser();

            //create config file if it doesn't exist
            if (!System.IO.File.Exists(configIniPath))
            {
                var newFile = new IniParser.Model.IniData();
                newFile["SETTINGS"]["PlaylistFolderPath"] = AssemblyDirectory;
                newFile["SETTINGS"]["NewPlaylistsPrivate"] = "false";
                newFile["SETTINGS"]["RecreatePlaylists"] = "false";
                newFile["SETTINGS"]["DeleteOrphanedPlaylists"] = "true";
                iniParser.WriteFile(configIniPath, newFile);
            }

            //read config file
            var configIni = iniParser.ReadFile(configIniPath);

            //store settings
            Settings._PlaylistFolderPath = configIni["SETTINGS"]["PlaylistFolderPath"];
            Settings._NewPlaylistsPrivate = bool.Parse(configIni["SETTINGS"]["NewPlaylistsPrivate"]);
            Settings._RecreatePlaylists = bool.Parse(configIni["SETTINGS"]["RecreatePlaylists"]);
            Settings._DeleteOrphanedPlaylists = bool.Parse(configIni["SETTINGS"]["DeleteOrphanedPlaylists"]);


            //lazy developer shortcut for sharing files between two machines
            if (System.Diagnostics.Debugger.IsAttached)
                Settings._PlaylistFolderPath = Settings._PlaylistFolderPath.Replace("/media/content/", "Z:/");

            if (!System.IO.Directory.Exists(Settings._PlaylistFolderPath))
                System.IO.Directory.CreateDirectory(Settings._PlaylistFolderPath);

        }

        static Dictionary<string, List<string>> GetArtistPlaylistSetup(SpotifyClient spotify, string directoryPath)
        {

            if (!System.IO.Directory.Exists(directoryPath))
                System.IO.Directory.CreateDirectory(directoryPath);


            var files = System.IO.Directory.GetFiles(directoryPath);

            //write an example file for bupkiss
            //TODO only write this example file when creating a missing directory
            //this will prevent the file and playlist from being created over and over if the user doesn't want it
            if (!files.Any())
            {
                var exampleArtists =
                    Settings._CommentString + " artist names to use in this playlist should be specified here" + Environment.NewLine +
                    "Heilung" + Environment.NewLine +
                    "Danheim" + Environment.NewLine +
                    "Nytt Land" + Environment.NewLine +
                    Environment.NewLine +
                    Settings._CommentString + " playlist IDs can be used as well, to pull artist names from" + Environment.NewLine +
                    Settings._CommentString + " The playlist ID can be found by navigating to the playlist on open.spotify.com and pulling it from the URL" + Environment.NewLine +
                    "1B8vQacVN5UXO4C4x9kthJ " + Settings._CommentString + "# Nordic folk music" + Environment.NewLine +
                    "37i9dQZF1DWXhcuQw7KIeM " + Settings._CommentString + "# Northern Spirits" + Environment.NewLine
                    ;

                //write the example file
                System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Nordic Folk.txt"), exampleArtists);
                //re-read files to pick up the example
                files = System.IO.Directory.GetFiles(directoryPath);
            }

            //read in playlist breakdowns
            var playlistsByArtists = files.ToDictionary(
                path => System.IO.Path.GetFileNameWithoutExtension(path),
                path => System.IO.File.ReadAllLines(path)
                        .Select(line => line.RemoveAfterString(Settings._CommentString).Trim()) //remove comments
                        .Where(line => !string.IsNullOrWhiteSpace(line)) //remove blank lines
                        .Distinct() //keep unique lines
                        .ToList()
                        
                );

            var regex = new Regex(@"[a-zA-Z0-9]{22}");
            foreach (var playlistName in playlistsByArtists.Keys)
            {
                //check for any URIs in the artist name list
                var playlistURIs = playlistsByArtists[playlistName]
                    .Where(artistName => regex.Match(artistName).Success)
                    .ToList();

                //deliberately not removing raw URIs from the artist list as one could *theoretically* also be an artist name

                //only do more work if we found any playlist URIs
                if (playlistURIs.Any())
                {

                    if (Settings._VerboseDebug)
                    {
                        Console.WriteLine(
                            "Found " +
                            playlistURIs.Count().ToString("#,##0") +
                            " playlist URIs under the \"" + playlistName +
                            "\" playlist definition"
                            );
                    }

                    //pull out artist names from tracks in these playlists URIs
                    var playlistArtistNames = playlistURIs
                        .Select(uri => spotify.Playlists.Get(uri).Result)
                        .SelectMany(p => spotify.Paginate(p.Tracks, new WaitPaginator(WaitTime:500)).ToListAsync().Result)
                        .SelectMany(playableItem => ((FullTrack)playableItem.Track).Artists.Select(a => a.Name))
                        .Distinct()
                        .ToList();
                    ;

                    //add in the artist names, then remove duplicates
                    playlistsByArtists[playlistName].AddRange(playlistArtistNames);
                    playlistsByArtists[playlistName] = playlistsByArtists[playlistName].Distinct().ToList();

                }

                var removeArtists = playlistsByArtists[playlistName]
                    .Where(artistName => artistName.StartsWith("-"))
                    .Select(artistName => artistName.Substring(1))
                    .ToList();

                if (removeArtists.Any())
                    playlistsByArtists[playlistName].RemoveRange(removeArtists);

            }

            Console.WriteLine(
                "Found " +
                playlistsByArtists.Keys.Count().ToString("#,##0") +
                " playlists by artist with an average of " +
                playlistsByArtists.Values.Average(x => x.Count).ToString("#,##0.00") +
                " artists each."
                );

            return playlistsByArtists;
        }

        static async System.Threading.Tasks.Task<Dictionary<string, List<FullTrack>>> GetLikesByArtistPlaylistBreakdowns(SpotifyClient spotify)
        //static Dictionary<string, List<FullTrack>> GetLikesByArtistPlaylistBreakdowns(SpotifyClient spotify)
        {
            var preface = "#Liked - ";
            var otherPlaylistName = "Z$ Other";

            var likesByArtistPath = System.IO.Path.Join(Settings._PlaylistFolderPath, "Likes By Artist");

            var playlistsByArtists = GetArtistPlaylistSetup(spotify, likesByArtistPath);
            var likedTracks = await GetLikedTracks(spotify);

            //find liked artists whose playlist name hasn't been specified by the user
            var missingArtists = likedTracks
                .SelectMany(t => t.Artists.Select(a => a.Name))
                .Where(x => !playlistsByArtists.Values.SelectMany(y => y).Any(y => y == x))
                .OrderByDescending(x => likedTracks.Where(t => t.Artists.Any(a => a.Name == x)).Count()) //TODO ordering here does nothing
                .ToList();

            //consider writing the artist list to an "other" liked by artist playlist file
            //but that's complicated, as you'd have to not read it in on subsequent runs
            //or just explicitly ignore/remove it

            //add an "other" liked playlist, unless it already exists
            if (!playlistsByArtists.Keys.Any(x => x == otherPlaylistName))
                playlistsByArtists.Add(otherPlaylistName, new List<string>());

            playlistsByArtists[otherPlaylistName].AddRange(missingArtists);

            var playlistBreakdowns = new Dictionary<string, List<FullTrack>>();

            foreach (var playlistBreakdown in playlistsByArtists)
            {

                //add all liked tracks to the playlist breakdown
                var artistLikedTracks = likedTracks.Where(t => t.Artists.Any(a => playlistBreakdown.Value.Any(playlistArtist => playlistArtist == a.Name))).ToList();
                playlistBreakdowns.Add(preface + playlistBreakdown.Key, artistLikedTracks);

            }


            return playlistBreakdowns;
        }

        //static Dictionary<string, List<FullTrack>> GetFullArtistDiscographyBreakdowns(SpotifyClient spotify)
        static async System.Threading.Tasks.Task<Dictionary<string, List<FullTrack>>> GetFullArtistDiscographyBreakdowns(SpotifyClient spotify)
        {

            var directoryPath = System.IO.Path.Join(Settings._PlaylistFolderPath, "Full Discography By Artist");
            var playlistsByArtists = GetArtistPlaylistSetup(spotify, directoryPath);
            var preface = "#Full Discog - ";

            var playlistBreakdowns = new Dictionary<string, List<FullTrack>>();

            foreach (var playlistByArtist in playlistsByArtists)
            {
                //the only example of searching with the API wrapper
                //https://johnnycrazy.github.io/SpotifyAPI-NET/docs/pagination

                //query examples found here
                //https://developer.spotify.com/documentation/web-api/reference/#writing-a-query---guidelines

                //"search" for artists, then correct results to actually the artists named
                var artists = playlistByArtist.Value
                    .Select(artistName => new SearchRequest(SearchRequest.Types.Artist, "artist:" + artistName))
                    .Select(request => spotify.Search.Item(request).Result)
                    .Select(item => spotify.Paginate(item.Artists, s => s.Artists, new WaitPaginator(WaitTime: 500))
                        .ToListAsync(Take: 40).Result // would like this to be 1, but the sought for artists are missing with less than 40
                        .Where(artist => playlistByArtist.Value.Contains(artist.Name)) // can't do a test on this specific artist name without a lot more mess
                        .FirstOrDefault()
                        )
                    .Where(artist => artist != null)
                    .ToList();

                //get all albums for the artists found
                var albums = artists.Select(artist => spotify.Artists.GetAlbums(artist.Id).Result)
                    .SelectMany(item => spotify.Paginate(item, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
                    .OrderBy(album => album.ReleaseDate)
                    .ToList();

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

                //identify tracks in those albums
                var trackIDs = albums.Select(album => spotify.Albums.GetTracks(album.Id).Result)
                    .SelectMany(item => spotify.Paginate(item, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
                    .Select(track => track.Id) // this "track" is SimpleTrack rather than FullTrack; need a list of IDs to convert them to FullTrack
                    .ToList();

                //get the tracks
                var tracks = new List<FullTrack>();
                foreach (var idChunk in trackIDs.ChunkBy(50))
                {
                    var chunkTracks = (await spotify.Tracks.GetSeveral(new TracksRequest(idChunk))).Tracks
                        //ignore tracks by artists outside the spec if this is an "appears on" album, like a compilation
                        //this includes tracks that are on split albums, collaborations, and the like
                        .Where(t => !appearsOnAlbums.Contains(t.Album.Id) || t.Artists.Select(a => a.Name).Any(artistName => playlistByArtist.Value.Contains(artistName)))
                        ////only include tracks strictly by artists specified
                        //.Where(t => t.Artists.Select(a => a.Name).Any(artistName => playlistByArtist.Value.Contains(artistName)))
                        .ToList();

                    tracks.AddRange(chunkTracks);
                }

                if (Settings._VerboseDebug)
                {
                    var heyoJankyos = playlistByArtist.Value.Where(artistName => !artists.Any(a => a.Name == artistName)).ToList();
                    if (heyoJankyos.Any())
                        Console.WriteLine("Could not find the following artists for \"" + playlistByArtist.Key + "\": " + heyoJankyos.Join(", "));

                    Console.WriteLine("found " + 
                        artists.Count.ToString("#,##0") + " artists, " +
                        albums.Count.ToString("#,##0") + " albums, and " +
                        tracks.Count.ToString("#,##0") + " tracks " +
                        "in full discog search");
                }

                //add the playlist
                playlistBreakdowns.Add(preface + playlistByArtist.Key, tracks);
            }

            return playlistBreakdowns;
        }

        static async System.Threading.Tasks.Task<List<FullTrack>> GetLikedTracks(SpotifyClient spotify)
        {
            var tracks = spotify.Paginate(await spotify.Library.GetTracks())
                .ToListAsync()
                .Result
                .Select(x => x.Track)
                .ToList();

            if (Settings._VerboseDebug)
            {
                var likedTracksBreakdown = tracks.Select(track =>
                    string.Join(", ", track.Artists.Select(x => x.Name).ToArray()) + " - " +
                    track.Name
                    ).Join(Environment.NewLine);
                Console.WriteLine();
                Console.WriteLine(likedTracksBreakdown);
                Console.WriteLine();
            }

            Console.WriteLine("Found " + tracks.Count().ToString("#,##0") + " liked tracks.");

            return tracks;

        }

        static async System.Threading.Tasks.Task<List<FullPlaylist>> GetAllPlaylists(SpotifyClient spotify)
        {
            var allPlaylists = (await spotify.Paginate(await spotify.Playlists.CurrentUsers()).ToListAsync())
                .Select(p => spotify.Playlists.Get(p.Id).Result) //re-get the playlist to convert from SimplePlaylist to FullPlaylist
                .ToList();

            return allPlaylists;
        }

        static async System.Threading.Tasks.Task UpdatePlaylists(SpotifyClient spotify, Dictionary<string, List<FullTrack>> playlistBreakdowns)
        {

            var allPlaylists = await GetAllPlaylists(spotify);

            var prefixes = new string[] { "#Liked", "#Full Discog" };
            var prefixesInProd = playlistBreakdowns.Keys.Select(playlistName => "#" + playlistName.FindTextBetween("#", " - "))
                .Distinct()
                .ToList();

            if (System.Diagnostics.Debugger.IsAttached && prefixesInProd.Any(p => !prefixes.Contains(p)))
                throw new Exception("Missing hardcoded playlist prefix");

            //dump all playlists here if settings say to recreate them
            if (Settings._RecreatePlaylists)
            {
                var removePlaylists = allPlaylists.Where(p => prefixes.Any(prefix => p.Name.StartsWith(prefix))).ToList();

                //dump the playlists
                foreach (var playlist in removePlaylists)
                {
                    await spotify.Follow.UnfollowPlaylist(playlist.Id);
                    allPlaylists.RemoveRange(removePlaylists);
                }

                if (removePlaylists.Any())
                    Console.WriteLine("Removed " + removePlaylists.Count.ToString("#,##0") + " playlists.");

            }
            //remove orphaned playlists
            else if (Settings._DeleteOrphanedPlaylists)
            {
                var playlistsToSkip = new string[] { "#Liked - Z$ Other" }; //is there a better way to do this than hardcoding the name?

                var removePlaylists = allPlaylists.Where(p => 
                    prefixes.Any(prefix =>  p.Name.StartsWith(prefix)) &&
                    !playlistBreakdowns.Any(b => p.Name == b.Key) &&
                    !playlistsToSkip.Contains(p.Name)
                    ).ToList();

                //dump the playlists
                foreach (var playlist in removePlaylists)
                {
                    await spotify.Follow.UnfollowPlaylist(playlist.Id);
                    allPlaylists.RemoveRange(removePlaylists);
                }

                if (removePlaylists.Any())
                    Console.WriteLine("Removed " + removePlaylists.Count.ToString("#,##0") + " playlists.");

            }

            foreach (var playlistBreakdown in playlistBreakdowns)
            {
                //complicated logic for determining duplicates
                var dupes = playlistBreakdown.Value
                    .GroupBy(track =>track.Name + " $$$ " + track.Artists.Select(a => a.Name).Join(", ")) //same track name, same artist
                    .Where(group =>
                        group.Count() > 1 && // only dupes
                        group.Select(track => track.Album.Name).Distinct().Count() > 1 // not from the same album
                        //do a time comparison as well to test for fundamental differences, but think about how this effects live albums
                        )
                    .Select(group => group
                        .OrderByDescending(track => track.Album.AlbumType == "album") //albums first
                        .ThenBy(track => track.Album.ReleaseDate) // older albums first; this should help de-prioritize deluxe releases and live albums
                        //TODO put some serious thought into how to best handle live albums
                        .ToList()
                        )
                    .ToList();

                var removeTracks = dupes
                    .SelectMany(group => group.Where(track => track != group.First()))
                    .ToList();
                playlistBreakdown.Value.RemoveRange(removeTracks);

                if (Settings._VerboseDebug && dupes.Any())
                    Console.WriteLine("Excluding " + removeTracks.Count().ToString("#,##0") + " dupes from " + playlistBreakdown.Key);
            }


            var createPlaylistCounter = 0;
            var removedTracksCounter = 0;
            var addedTracksCounter = 0;

            //iterating through rather than running in bulk with linq to *hopefully* be a little more memory efficient
            //order by descending playlist name to get alphabetical playlists in the Spotify interface
            foreach (var playlistBreakdown in playlistBreakdowns.OrderByDescending(kvp => kvp.Key).ToList())
            {
                //get the playlist for this playlist name
                var playlist = allPlaylists.Where(p => p.Name == playlistBreakdown.Key).SingleOrDefault();
                //create playlist if missing
                if (playlist is null)
                {
                    var playlistRequest = new PlaylistCreateRequest(playlistBreakdown.Key);
                    playlistRequest.Description = "Automatically generated by spotify_playlist_generator.";
                    playlistRequest.Public = !Settings._NewPlaylistsPrivate;
                    var newPlaylist = await spotify.Playlists.Create(spotify.UserProfile.Current().Result.Id, playlistRequest);

                    playlist = newPlaylist;

                    createPlaylistCounter += 1;
                }

                //get all the tracks that ARE in the playlist - doing this here as they have to be casted from PlaylistTrack to FullTrack to be useful
                var playlistTracksCurrent = (await spotify.Paginate(playlist.Tracks).ToListAsync())
                    .Select(x => (FullTrack)x.Track)
                    .ToList()
                    ;

                //main work part 1 - remove existing playlist tracks that no longer belong
                var removeTrackRequestItems = playlistTracksCurrent
                    .Where(gpt => !playlistBreakdown.Value.Any(glt => glt.Id == gpt.Id))
                    .Select(gpt => new PlaylistRemoveItemsRequest.Item() { Uri = gpt.Uri })
                    .ToList();

                if (removeTrackRequestItems.Any())
                {
                    //the API only accepts 100 tracks at a time, so divide up and run each set of 100 separately
                    var removeRequests = removeTrackRequestItems
                        .ChunkBy(100)
                        .Select(items => new PlaylistRemoveItemsRequest { Tracks = items })
                        .ToList();

                    foreach (var removeRequest in removeRequests)
                        await spotify.Playlists.RemoveItems(playlist.Id, removeRequest);

                    removedTracksCounter += removeTrackRequestItems.Count();
                }



                //main work part 2 - add new tracks to playlists
                var addTrackURIs = playlistBreakdown.Value
                    .Where(glt => !playlistTracksCurrent.Any(gpt => gpt.Id == glt.Id))
                    //.Select(glt => new PlaylistAddItemsRequest.Item() { Uri = glt.Uri }) //add track requires URIs, whereas remove track requires a custom object based on URIs
                    .Select(glt => glt.Uri)
                    .ToList();

                if (addTrackURIs.Any())
                {
                    //the API only accepts 100 tracks at a time, so divide up and run each set of 100 separately
                    var addRequests = addTrackURIs
                        .ChunkBy(100)
                        .Select(uris => new PlaylistAddItemsRequest(uris))

                        .ToList();
                    foreach (var addRequest in addRequests)
                        await spotify.Playlists.AddItems(playlist.Id, addRequest);

                    addedTracksCounter += addTrackURIs.Count();
                }

                //TODO percent progress counter
            }

            Console.WriteLine("Removed " + removedTracksCounter.ToString("#,##0") + " existing tracks.");
            Console.WriteLine("Added " + addedTracksCounter.ToString("#,##0") + " new tracks.");
            Console.WriteLine();


        }


    }
}
