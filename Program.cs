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

            var spotify = new SpotifyClient(accessToken);



            var me = await spotify.UserProfile.Current();
            Console.WriteLine($"Hello there, {me.DisplayName}");
            Console.WriteLine("----------------------");
            Console.WriteLine();

            //get inputs
            var genreBreakdowns = GetGenreBreakdowns(spotify);
            var likedTracks = await GetLikedTracks(spotify);
            //do work!
            await UpdatePlaylists(spotify, genreBreakdowns, likedTracks);



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
                newFile["SETTINGS"]["DeleteOrphanedPlaylists"] = "false";
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

        //static async System.Threading.Tasks.Task<Dictionary<string, List<string>>> GetGenreBreakdowns(SpotifyClient spotify)
        static Dictionary<string, List<string>> GetGenreBreakdowns(SpotifyClient spotify)
        {

            var byArtistPaths = System.IO.Path.Join(Settings._PlaylistFolderPath, "By Artist");

            if (!System.IO.Directory.Exists(byArtistPaths))
                System.IO.Directory.CreateDirectory(byArtistPaths);


            var files = System.IO.Directory.GetFiles(byArtistPaths);

            //write an example file for bupkiss
            if (!files.Any())
            {
                var exampleArtists =
                    "Heilung" + Environment.NewLine +
                    "Danheim" + Environment.NewLine +
                    "Nytt Land" + Environment.NewLine +
                    "1B8vQacVN5UXO4C4x9kthJ" + Environment.NewLine +
                    "37i9dQZF1DWXhcuQw7KIeM" + Environment.NewLine
                    ;

                //write the example file
                System.IO.File.WriteAllText(System.IO.Path.Join(byArtistPaths, "Nordic Folk.txt"), exampleArtists);
                //re-read files to pick up the example
                files = System.IO.Directory.GetFiles(byArtistPaths);
            }

            //read in genre breakdowns
            var genreBreakdowns = files.ToDictionary(
                path => System.IO.Path.GetFileNameWithoutExtension(path),
                path => System.IO.File.ReadAllLines(path).ToList()
                );

            //test values
            if (Settings._TestValues)
            {
                //hardcoded test values to get this running before building out the text file support
                genreBreakdowns = new Dictionary<string, List<string>>();
                genreBreakdowns.Add("Nordic Folk", new List<string>());
                genreBreakdowns["Nordic Folk"].Add("Heilung");
                genreBreakdowns["Nordic Folk"].Add("Danheim");
                genreBreakdowns["Nordic Folk"].Add("Nytt Land");
                genreBreakdowns["Nordic Folk"].Add("1B8vQacVN5UXO4C4x9kthJ");
                genreBreakdowns["Nordic Folk"].Add("37i9dQZF1DWXhcuQw7KIeM");
            }

            var regex = new Regex(@"[a-zA-Z0-9]{22}");
            foreach (var genreName in genreBreakdowns.Keys)
            {
                //check for any URIs in the artist name list
                var playlistURIs = genreBreakdowns[genreName]
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
                            " playlist URIs under the \"" + genreName + 
                            "\" genre definition"
                            );
                    }

                    //pull out artist names from tracks in these playlists URIs
                    var playlistArtistNames = playlistURIs
                        .Select(uri => spotify.Playlists.Get(uri).Result)
                        .SelectMany(p => spotify.Paginate(p.Tracks).ToListAsync().Result)
                        .SelectMany(playableItem => ((FullTrack)playableItem.Track).Artists.Select(a => a.Name))
                        .Distinct()
                        .ToList();
                    ;

                    //add in the artist names, then remove duplicates
                    genreBreakdowns[genreName].AddRange(playlistArtistNames);
                    genreBreakdowns[genreName] = genreBreakdowns[genreName].Distinct().ToList();

                }

                var removeArtists = genreBreakdowns[genreName]
                    .Where(artistName => artistName.StartsWith("-"))
                    .Select(artistName => artistName.Substring(1))
                    .ToList();

                if (removeArtists.Any())
                    genreBreakdowns[genreName].RemoveRange(removeArtists);

            }

            Console.WriteLine(
                "Found " +
                genreBreakdowns.Keys.Count().ToString("#,##0") +
                " genres with an average of " +
                genreBreakdowns.Values.Average(x => x.Count).ToString("#,##0.00") +
                " artists each."
                );



            return genreBreakdowns;
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


        static async System.Threading.Tasks.Task UpdatePlaylists(SpotifyClient spotify, Dictionary<string, List<string>> genreBreakdowns, List<FullTrack> likedTracks)
        {
            var preface = "#Liked - ";
            var otherPlaylistName = "Z$ Other";

            var allPlaylists = (await spotify.Paginate(await spotify.Playlists.CurrentUsers()).ToListAsync())
                .Select(p => spotify.Playlists.Get(p.Id).Result) //re-get the playlist to convert from SimplePlaylist to FullPlaylist
                .ToList();

            //handle these settings together as they're similar
            if (Settings._RecreatePlaylists || Settings._DeleteOrphanedPlaylists)
            {
                var removePlaylists = allPlaylists.Where(p => p.Name.StartsWith(preface)).ToList();

                //limit to only orphaned playlists unless we're already dumping all of them
                if (Settings._DeleteOrphanedPlaylists && !Settings._RecreatePlaylists)
                    removePlaylists = removePlaylists.Where(p =>
                    !genreBreakdowns.Any(b => p.Name == preface + b.Key)
                    && p.Name != preface + otherPlaylistName
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

            //find liked artists whose genre hasn't been specified by the user
            var missingArtists = likedTracks
                .SelectMany(t => t.Artists.Select(a => a.Name))
                .Where(x => !genreBreakdowns.Values.SelectMany(y => y).Any(y => y == x))
                .OrderByDescending(x => likedTracks.Where(t => t.Artists.Any(a => a.Name == x)).Count()) //TODO ordering here does nothing
                .ToList();

            //consider writing the artist list to an "other" genre file
            //but that's complicated, as you'd have to not read it in on subsequent runs
            //or just explicitly ignore/remove it

            //add an "other" liked playlist, unless it already exists
            if (!genreBreakdowns.Keys.Any(x => x == otherPlaylistName))
                genreBreakdowns.Add(otherPlaylistName, new List<string>());

            genreBreakdowns[otherPlaylistName].AddRange(missingArtists);

            if (Settings._VerboseDebug)
            {
                var allPlaylistsBreakdown = allPlaylists.Select(p =>
                    p.Name +
                    ", ID: " + p.Id +
                    ", URI: " + p.Uri
                    ).Join(Environment.NewLine);
                Console.WriteLine();
                Console.WriteLine(allPlaylistsBreakdown);
                Console.WriteLine();
            }

            var createPlaylistCounter = 0;
            var removedTracksCounter = 0;
            var addedTracksCounter = 0;

            //iterating through rather than running in bulk with linq to *hopefully* be a little more memory efficient
            //order by descending genre name to get alphabetical playlists in the Spotify interface
            foreach (var genre in genreBreakdowns.OrderByDescending(kvp => kvp.Key).ToList())
            {
                //get the playlist for this genre
                var genrePlaylist = allPlaylists.Where(p => p.Name == preface + genre.Key).SingleOrDefault();
                //create genre playlist if missing
                if (genrePlaylist is null)
                {
                    var playlistRequest = new PlaylistCreateRequest(preface + genre.Key);
                    playlistRequest.Description = "Automatically generated by spotify_playlist_generator based on my liked tracks.";
                    playlistRequest.Public = !Settings._NewPlaylistsPrivate;
                    var newPlaylist = await spotify.Playlists.Create(spotify.UserProfile.Current().Result.Id, playlistRequest);

                    genrePlaylist = newPlaylist;

                    createPlaylistCounter += 1;
                }

                //get all the tracks that SHOULD be in the playlist
                var genreLikedTracks = likedTracks.Where(t => t.Artists.Any(a => genre.Value.Any(genreArtist => genreArtist == a.Name))).ToArray();
                //get all the tracks that ARE in the playlist - doing this here as they have to be casted from PlaylistTrack to FullTrack to be useful
                var genrePlaylistTracks = (await spotify.Paginate(genrePlaylist.Tracks).ToListAsync())
                    .Select(x => (FullTrack)x.Track)
                    .ToList()
                    ;

                //main work part 1 - remove existing playlist tracks that no longer belong
                var removeTrackRequestItems = genrePlaylistTracks
                    .Where(gpt => !genreLikedTracks.Any(glt => glt.Id == gpt.Id))
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
                        await spotify.Playlists.RemoveItems(genrePlaylist.Id, removeRequest);

                    removedTracksCounter += removeTrackRequestItems.Count();
                }



                //main work part 2 - add new tracks to playlists
                var addTrackURIs = genreLikedTracks
                    .Where(glt => !genrePlaylistTracks.Any(gpt => gpt.Id == glt.Id))
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
                        await spotify.Playlists.AddItems(genrePlaylist.Id, addRequest);

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
