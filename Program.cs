using System;
using SpotifyAPI.Web;
using IniParser;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace spotify_playlist_generator
{
    partial class Program
    {

        private static class Settings
        {
            public static string _configFolder;
            public static string _PlaylistFolderPath;
            public static string _CommentString = "#";      //TODO consider pulling this from a config file
            public static string _ExclusionString = "-";    //TODO consider pulling this from a config file
            public static string _SeparatorString = "-";    //TODO consider pulling this from a config file
            public static bool _NewPlaylistsPrivate;
            public static bool _RecreatePlaylists;
            public static bool _DeleteOrphanedPlaylists;
            public static bool _VerboseDebug;
        }

        private static int MaxPlaylistSize = 11000; //max playlist size as of 2021-07-15; the api throws an error once you pass this
        private static Regex idRegex = new Regex(@"[a-zA-Z0-9]{22}");
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

        //having this as an extension method would make for much tighter organization, but CS0721 prevents this; no static types as parameters
        public static void ClearLineAfterCursor(int UpdateCursorLeftPosition)
        {
            ConsoleUpdateCursorLeft(UpdateCursorLeftPosition);
            Console.Write(new string(' ', Console.BufferWidth - UpdateCursorLeftPosition));
            ConsoleUpdateCursorLeft(UpdateCursorLeftPosition);
        }
        public static void ClearLineAfterCursor()
        {
            var cursorLeft = Console.GetCursorPosition().Left;
            ClearLineAfterCursor(cursorLeft);
        }
        //a method of convenience to avoid multi-line lambdas later on
        public static void ConsoleWriteAndClearLine(string value)
        {
            Console.Write(value);
            ClearLineAfterCursor();
        }
        public static void ConsoleWriteAndClearLine(int LeftPosition, string value)
        {
            Console.SetCursorPosition(LeftPosition, Console.GetCursorPosition().Top);
            ConsoleWriteAndClearLine(value);
        }
        public static void ConsoleUpdateCursorLeft(int CursorLeft)
        {
            Console.SetCursorPosition(CursorLeft, Console.GetCursorPosition().Top);
        }
        public static string AddOrdinal(int num)
        {
            //https://stackoverflow.com/a/20175

            if (num <= 0) return num.ToString();

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return num + "th";
            }

            switch (num % 10)
            {
                case 1:
                    return num + "st";
                case 2:
                    return num + "nd";
                case 3:
                    return num + "rd";
                default:
                    return num + "th";
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
            Console.WriteLine("Starting at " + DateTime.Now.ToString());
            Console.WriteLine("Welcome to C# Spotify Playlist Generator! Please don't expect too much, but also be impressed.");

            var sw = Stopwatch.StartNew();

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

            //start reports on a different thread
            //hopefully this doesn't cause api problems lol
            var threadReporting = new System.Threading.Thread(() =>
            {
                LikedGenreReport(spotify).Wait();
            });
            threadReporting.Start();

            //get various playlist definitions
            //that is, a name and a list of tracks
            var playlistBreakdowns = new Dictionary<string, List<FullTrack>>();
            playlistBreakdowns.AddRange(await GetLikedTracksByGenre(spotify));
            playlistBreakdowns.AddRange(await GetLikesByArtistPlaylistBreakdowns(spotify));
            playlistBreakdowns.AddRange(await GetFullArtistDiscographyBreakdowns(spotify));


            //do work!
            await UpdatePlaylists(spotify, playlistBreakdowns);

            //wait for the reporting thread to complete before continuing
            threadReporting.Join();



            Console.WriteLine();
            Console.WriteLine("All done, get jammin'!");

            Console.WriteLine("Run took " + sw.Elapsed.ToHumanTimeString());
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
                newFile["SETTINGS"]["Verbose"] = "false";
                iniParser.WriteFile(configIniPath, newFile);
            }

            //read config file
            var configIni = iniParser.ReadFile(configIniPath);

            //store settings
            Settings._PlaylistFolderPath = configIni["SETTINGS"]["PlaylistFolderPath"];
            Settings._NewPlaylistsPrivate = bool.Parse(configIni["SETTINGS"]["NewPlaylistsPrivate"]);
            Settings._RecreatePlaylists = bool.Parse(configIni["SETTINGS"]["RecreatePlaylists"]);
            Settings._DeleteOrphanedPlaylists = bool.Parse(configIni["SETTINGS"]["DeleteOrphanedPlaylists"]);
            Settings._VerboseDebug = bool.Parse(configIni["SETTINGS"]["Verbose"]);


            //lazy developer shortcut for sharing files between two machines
            if (System.Diagnostics.Debugger.IsAttached)
                Settings._PlaylistFolderPath = Settings._PlaylistFolderPath.Replace("/media/content/", "Z:/");

            if (!System.IO.Directory.Exists(Settings._PlaylistFolderPath))
                System.IO.Directory.CreateDirectory(Settings._PlaylistFolderPath);

        }

        static Dictionary<string, List<string>> GetGenrePlaylistSetup(SpotifyClient spotify, string directoryPath)
        {

            if (!System.IO.Directory.Exists(directoryPath))
                System.IO.Directory.CreateDirectory(directoryPath);

            var files = System.IO.Directory.GetFiles(directoryPath);

            //write an example file for bupkiss
            //TODO only write this example file when creating a missing directory
            //this will prevent the file and playlist from being created over and over if the user doesn't want it
            if (!files.Any())
            {
                var exampleGenres =
                    Settings._CommentString + " genres to be used in this playlist should be specified here" + Environment.NewLine +
                    "dark clubbing" + Environment.NewLine +
                    "darksynth" + Environment.NewLine +
                    "dark electro" + Environment.NewLine +
                    "dark step" + Environment.NewLine
                    //Environment.NewLine +
                    //Settings._CommentString + " playlist IDs can be used as well, to pull artist names from" + Environment.NewLine +
                    //Settings._CommentString + " The playlist ID can be found by navigating to the playlist on open.spotify.com and pulling it from the URL" + Environment.NewLine +
                    //"1B8vQacVN5UXO4C4x9kthJ " + Settings._CommentString + "# Nordic folk music" + Environment.NewLine +
                    //"37i9dQZF1DWXhcuQw7KIeM " + Settings._CommentString + "# Northern Spirits" + Environment.NewLine
                    ;

                //write the example file
                System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Dark Synth.txt"), exampleGenres);
                //re-read files to pick up the example
                files = System.IO.Directory.GetFiles(directoryPath);
            }

            //read in playlist breakdowns
            var playlistsByGenre = files.ToDictionary(
                path => System.IO.Path.GetFileNameWithoutExtension(path),
                path => System.IO.File.ReadAllLines(path)
                        .Select(line => line.RemoveAfterString(Settings._CommentString).Trim()) //remove comments
                        .Where(line => !string.IsNullOrWhiteSpace(line)) //remove blank lines
                        .Distinct() //keep unique lines
                        .ToList()
                        
                );

            //loading genres from playlists seems minimally useful
            //without that exclusions don't make sense at this location either
            //keeping this here just in case we want to add this feature int he future
            //var pp = new ProgressPrinter(playlistsByGenre.Count(), (perc, time) => ConsoleWriteAndClearLine("\rReading playlist definitions: " + perc + ", " + time + " remaining"));
            //foreach (var playlistName in playlistsByGenre.Keys)
            //{
            //    var genresOnly = playlistsByGenre[playlistName]
            //        .Where(genreName => !genreName.StartsWith(Settings._ExclusionString))
            //        .ToList();

            //    //adding these instead of editing to better handle possible artists that include the exclusion character
            //    playlistsByGenre[playlistName].AddRange(genresOnly);

            //    //not a lot of point in pulling genre names from playlists, but it could be done
            //    //check for any URIs in the artist name list
            //    var playlistURIs = playlistsByGenre[playlistName]
            //        .Where(artistName => idRegex.Match(artistName).Success)
            //        .ToList();

            //    //deliberately not removing raw URIs from the artist list as one could *theoretically* also be an artist name



            //    //TODO load playlist artists from a file here
            //    //something like Nordic Folk - Artists From Playlist.txt
            //    //to preserve artist names from living playlists
            //    //maybe draw a bit setting for this from the playlist file itself
            //    //load into a playlist artist archive variable so it can be compared when saving the playlist artists below

            //    //only do more work if we found any playlist URIs
            //    //TODO are collisions with artist URIs a concern?
            //    if (playlistURIs.Any())
            //    {

            //        //pull out artist names from tracks in these playlists URIs
            //        var playlistArtistNames = playlistURIs
            //            .Select(uri => spotify.Playlists.Get(uri).ResultSafe())
            //            .Where(p => p != null)
            //            .SelectMany(p => spotify.Paginate(p.Tracks, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
            //            .SelectMany(playableItem => ((FullTrack)playableItem.Track).Artists.Select(a => a.Name))
            //            .Distinct()
            //            .ToList();
            //        ;

            //        //TODO save playlist artists to a file here

            //        //add in the artist names, then remove duplicates
            //        playlistsByGenre[playlistName].AddRange(playlistArtistNames);
            //        playlistsByGenre[playlistName] = playlistsByGenre[playlistName].Distinct().ToList();

            //    }

            //    //exclusions don't make sense unless we're loading genres from playlists
            //    // handle artists to exclude
            //    // not ignoring album/track exclusions here as the risk of artists containing the separator character is bigger
            //    // TODO consider outsourcing this to a HandleExclusions overload then calling it here for organization purposes
            //    var excludeGenre = playlistsByGenre[playlistName]
            //        .Where(genreName => genreName.StartsWith(Settings._ExclusionString))
            //        .Select(genreName => genreName.Substring(Settings._ExclusionString.Length))
            //        .ToList();

            //    if (excludeGenre.Any())
            //        playlistsByGenre[playlistName].RemoveRange(excludeGenre);

            //    pp.PrintProgress();

            //}

            Console.WriteLine();
            Console.WriteLine(
                "Found " +
                playlistsByGenre.Keys.Count().ToString("#,##0") +
                " playlists by genre with an average of " +
                playlistsByGenre.Values.Average(x => x.Count).ToString("#,##0.00") +
                " genres each."
                );

            return playlistsByGenre;
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

            var pp = new ProgressPrinter(playlistsByArtists.Count(), (perc, time) => ConsoleWriteAndClearLine("\rReading playlist definitions: " + perc + ", " + time + " remaining"));
            foreach (var playlistName in playlistsByArtists.Keys)
            {
                //pull out the artist name for positive album/track specifications here
                //format being "artist name - album name - track name"
                //the excess tracks are removed downline by HandleExclusions()
                var artistsOnly = playlistsByArtists[playlistName]
                    .Where(artistName => !artistName.StartsWith(Settings._ExclusionString) && artistName.Contains(Settings._SeparatorString))
                    .Select(artistName => artistName.Split(Settings._SeparatorString).First().Trim())
                    .ToList();

                //adding these instead of editing to better handle possible artists that include the exclusion character
                playlistsByArtists[playlistName].AddRange(artistsOnly);


                //check for any URIs in the artist name list
                var playlistURIs = playlistsByArtists[playlistName]
                    .Where(artistName => idRegex.Match(artistName).Success)
                    .ToList();

                //deliberately not removing raw URIs from the artist list as one could *theoretically* also be an artist name



                //TODO load playlist artists from a file here
                //something like Nordic Folk - Artists From Playlist.txt
                //to preserve artist names from living playlists
                //maybe draw a bit setting for this from the playlist file itself
                //load into a playlist artist archive variable so it can be compared when saving the playlist artists below

                //only do more work if we found any playlist URIs
                //TODO are collisions with artist URIs a concern?
                if (playlistURIs.Any())
                {

                    //pull out artist names from tracks in these playlists URIs
                    var playlistArtistNames = playlistURIs
                        .Select(uri => spotify.Playlists.Get(uri).ResultSafe())
                        .Where(p => p != null)
                        .SelectMany(p => spotify.Paginate(p.Tracks, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
                        .SelectMany(playableItem => ((FullTrack)playableItem.Track).Artists.Select(a => a.Name))
                        .Distinct()
                        .ToList();
                    ;

                    //couldn't resolve an API error with this method, but likely not faster anyway
                    ////pull out artist names from tracks in these playlists URIs
                    //var playlistArtistNames = playlistURIs
                    //    .Select(uri => spotify.Paginate(spotify.Playlists.GetItems(uri).Result, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
                    //    .SelectMany(x => x)
                    //    .SelectMany(playableItem => ((FullTrack)playableItem.Track).Artists.Select(a => a.Name))
                    //    .Distinct()
                    //    .ToList()
                    ;

                    //TODO save playlist artists to a file here

                    //add in the artist names, then remove duplicates
                    playlistsByArtists[playlistName].AddRange(playlistArtistNames);
                    playlistsByArtists[playlistName] = playlistsByArtists[playlistName].Distinct().ToList();

                }

                // handle artists to exclude
                // not ignoring album/track exclusions here as the risk of artists containing the separator character is bigger
                // TODO consider outsourcing this to a HandleExclusions overload then calling it here for organization purposes
                var excludeArtist = playlistsByArtists[playlistName]
                    .Where(artistName => artistName.StartsWith(Settings._ExclusionString))
                    .Select(artistName => artistName.Substring(Settings._ExclusionString.Length))
                    .ToList();

                if (excludeArtist.Any())
                    playlistsByArtists[playlistName].RemoveRange(excludeArtist);

                pp.PrintProgress();

            }

            Console.WriteLine();
            Console.WriteLine(
                "Found " +
                playlistsByArtists.Keys.Count().ToString("#,##0") +
                " playlists by artist with an average of " +
                playlistsByArtists.Values.Average(x => x.Count).ToString("#,##0.00") +
                " artists each."
                );

            return playlistsByArtists;
        }
        static void HandleExclusions(Dictionary<string, List<FullTrack>> playlistBreakdowns, Dictionary<string, List<string>> playlistArtists)
        {

            //temp measure to keep this runable
            return;

            //right now this logic is excluding nearly everything
            //because you're excluding every artist that isn't explicitly included
            //need to work out a better way to do this
            //the slightly more intuitive way would be to go line by line applying the logic in that order... but that may be too hard

            //foreach (var playlistBreakdown in playlistBreakdowns)
            //{
            //    //TODO add a missing check here, though it should never occur without coder error
            //    //the split here is terrible, but should work fine as we're only dealing with one type of playlist at a time
            //    //TODO consider refactoring this if practical
            //    var artistNames = playlistArtists[playlistBreakdown.Key.Split("-",2)[1].Trim()]
            //            .Where(artistName => artistName.Contains(Settings._SeparatorString))
            //            .ToList();

            //    if (!artistNames.Any())
            //        continue;

            //    //model the include/exclude breakdown in an object
            //    var includeExcludeBreakdown = artistNames.Select(artistName => new
            //    {
            //        Exclude = artistName.StartsWith(Settings._ExclusionString),
            //        ArtistName = artistName.TrimStart(Settings._ExclusionString).Trim()
            //    })
            //        .Select(x => new
            //        {
            //            x.Exclude,
            //            ArtistName = x.ArtistName.Split(Settings._SeparatorString)[0].Trim(),
            //            AlbumName = (x.ArtistName.CountOccurrences(Settings._SeparatorString) >= 1 ? x.ArtistName.Split(Settings._SeparatorString)[1].Trim() : null),
            //            TrackName = (x.ArtistName.CountOccurrences(Settings._SeparatorString) >= 2 ? x.ArtistName.Split(Settings._SeparatorString, 3)[2].Trim() : null) //it's very likely that tracks will include the separator character, so only split to three elements
            //        })
            //        .ToList();

            //    var tracks =
            //        playlistBreakdown.Value
            //        //inclusions
            //        .Where(track =>
            //        includeExcludeBreakdown.Where(ie => !ie.Exclude).Any(ie =>
            //            (track.Artists.Any(artist => artist.Name == ie.ArtistName) && ie.AlbumName == null)
            //            || (track.Artists.Any(artist => artist.Name == ie.ArtistName) && track.Album.Name == ie.AlbumName && ie.TrackName == null)
            //            || (track.Artists.Any(artist => artist.Name == ie.ArtistName) && track.Album.Name == ie.AlbumName && track.Name == ie.TrackName )
            //            )
            //    )
            //        //then exclusions
            //        .Where(track =>
            //        !includeExcludeBreakdown.Where(ie => ie.Exclude).Any(ie =>
            //            (track.Artists.Any(artist => artist.Name == ie.ArtistName) && ie.AlbumName == null)
            //            || (track.Artists.Any(artist => artist.Name == ie.ArtistName) && track.Album.Name == ie.AlbumName && ie.TrackName == null)
            //            || (track.Artists.Any(artist => artist.Name == ie.ArtistName) && track.Album.Name == ie.AlbumName && track.Name == ie.TrackName)
            //            )
            //    )
            //        .ToList();

            //    //for testing only
            //    var excludedTracks = playlistBreakdown.Value.Where(track => !tracks.Any(x => x.Id == track.Id))
            //        .Select(track => new
            //        {
            //            ArtistNames = track.Artists.Select(a => a.Name).Join(", "),
            //            AlbumName = track.Album.Name,
            //            TrackName = track.Name
            //        })
            //        .ToList();


            //    foreach (var artistName in artistNames)
            //    {
            //        //positive album
            //        if (!artistName.StartsWith(Settings._ExclusionString) && artistName.TrimStart(Settings._ExclusionString).CountOccurrences(Settings._SeparatorString) == 1)
            //        {

            //        }
            //        //positive track
            //        //doing a > 1 count here to include tracks with the separator character in them
            //        //account for this logic when parsing out the artist/album/track names
            //        else if (!artistName.StartsWith(Settings._ExclusionString) && artistName.TrimStart(Settings._ExclusionString).CountOccurrences(Settings._SeparatorString) > 1)
            //        {

            //        }
            //        //negative album
            //        else if (artistName.StartsWith(Settings._ExclusionString) && artistName.TrimStart(Settings._ExclusionString).CountOccurrences(Settings._SeparatorString) == 1)
            //        {

            //        }
            //        //negative track
            //        else if (artistName.StartsWith(Settings._ExclusionString) && artistName.TrimStart(Settings._ExclusionString).CountOccurrences(Settings._SeparatorString) > 1)
            //        {

            //        }
            //    }

            //}
        }

        static async System.Threading.Tasks.Task<Dictionary<string, List<FullTrack>>> GetLikesByArtistPlaylistBreakdowns(SpotifyClient spotify)
        //static Dictionary<string, List<FullTrack>> GetLikesByArtistPlaylistBreakdowns(SpotifyClient spotify)
        {
            Console.WriteLine();
            Console.WriteLine("---likes by artist playlists---");

            var preface = "#Liked - ";
            var otherPlaylistName = "Z$ Other";
            var likesByArtistPath = System.IO.Path.Join(Settings._PlaylistFolderPath, "Likes By Artist");

            var playlistsByArtists = GetArtistPlaylistSetup(spotify, likesByArtistPath);
            var likedTracks = await spotify.GetLikedTracks();

            //find liked artists whose playlist name hasn't been specified by the user
            var missingArtists = likedTracks
                .SelectMany(t => t.Artists.Select(a => a.Name))
                .Where(x => !playlistsByArtists.Values.SelectMany(y => y).Any(y => y == x))
                .OrderByDescending(x => likedTracks.Where(t => t.Artists.Any(a => a.Name == x)).Count()) //TODO ordering here does nothing
                .ToList();

            //TODO consider writing the artist list to an "other" liked by artist playlist file
            //but that's complicated, as you'd have to not read it in on subsequent runs
            //or just explicitly ignore/remove it

            //add an "other" liked playlist, unless it already exists
            if (!playlistsByArtists.Keys.Any(x => x == otherPlaylistName))
                playlistsByArtists.Add(otherPlaylistName, new List<string>());

            playlistsByArtists[otherPlaylistName].AddRange(missingArtists);

            var playlistBreakdowns = new Dictionary<string, List<FullTrack>>();

            var pp = new ProgressPrinter(playlistsByArtists.Count(), (perc, time) => ConsoleWriteAndClearLine("\rAssembling playlists: " + perc + ", " + time + " remaining"));
            foreach (var playlistBreakdown in playlistsByArtists)
            {

                //add all liked tracks to the playlist breakdown
                var artistLikedTracks = likedTracks.Where(t => t.Artists.Any(a => playlistBreakdown.Value.Any(playlistArtist => playlistArtist.ToLower() == a.Name.ToLower()))).ToList();
                playlistBreakdowns.Add(preface + playlistBreakdown.Key, artistLikedTracks);
                pp.PrintProgress();
            }

            HandleExclusions(playlistBreakdowns, playlistsByArtists);

            Console.WriteLine();
            Console.WriteLine("Assembled " +
                playlistBreakdowns.Count().ToString("#,##0") +
                " playlists with " +
                playlistBreakdowns.Values.Sum(x => x.Count()).ToString("#,##0") +
                " tracks."
                );

            return playlistBreakdowns;
        }

        //static Dictionary<string, List<FullTrack>> GetFullArtistDiscographyBreakdowns(SpotifyClient spotify)
        static async System.Threading.Tasks.Task<Dictionary<string, List<FullTrack>>> GetFullArtistDiscographyBreakdowns(SpotifyClient spotify)
        {
            Console.WriteLine();
            Console.WriteLine("---full discography by artist playlists---");

            var directoryPath = System.IO.Path.Join(Settings._PlaylistFolderPath, "Full Discography By Artist");
            var playlistsByArtists = GetArtistPlaylistSetup(spotify, directoryPath);
            var preface = "#Full Discog - ";

            var missingArtists = new List<string>();
            var playlistBreakdowns = new Dictionary<string, List<FullTrack>>();
            var playlistCount = 1;
            var tooLargePlaylistCount = 0;
            var verboseDebugOutput = new List<string>();

            var pp = new ProgressPrinter(playlistsByArtists.Count(), (perc, time) => ConsoleWriteAndClearLine("\rAssembling playlists: " + perc));
            foreach (var playlistByArtist in playlistsByArtists)
            {
                var cursorLeft = Console.GetCursorPosition().Left;

                //the only example of searching with the API wrapper
                //https://johnnycrazy.github.io/SpotifyAPI-NET/docs/pagination

                //query examples found here
                //https://developer.spotify.com/documentation/web-api/reference/#writing-a-query---guidelines

                //"search" for artists, then correct results to actually the artists named
                var ppArtists = new ProgressPrinter(playlistByArtist.Value.Count(), (perc, time) => ConsoleWriteAndClearLine(cursorLeft, " -- " + AddOrdinal(playlistCount) + " playlist -- Getting artists: " + perc + ", " + time + " remaining"));
                var artists = playlistByArtist.Value
                    .Select(artistName => new SearchRequest(SearchRequest.Types.Artist, "artist:" + artistName))
                    .Select(request => spotify.Search.Item(request).Result)
                    .Select(item => spotify.Paginate(item.Artists, s => s.Artists, new WaitPaginator(WaitTime: 500))
                        .ToListAsync(Take: 40).Result // would like this to be 1, but the sought for artists are missing with less than 40
                        .Where(artist => playlistByArtist.Value.Contains(artist.Name, StringComparer.InvariantCultureIgnoreCase)) // can't do a test on this specific artist name without a lot more mess
                        .FirstOrDefault()
                        )
                    .Where(artist => ppArtists.PrintProgress() && artist != null)
                    .ToList();
                ConsoleUpdateCursorLeft(cursorLeft);


                //check for any URIs in the artist name list
                //this will pick up playlist URIs every time, but they'll be filtered out below
                var artistURIs = playlistByArtist.Value
                    .Where(artistName => idRegex.Match(artistName).Success)
                    .ToList();

                //only do more work if we found any artist URIs
                //TODO are collisions with playlist URIs a concern?
                if (artistURIs.Any())
                {
                    //pull out artist names from these artists URIs
                    var artistsByURI = await spotify.GetArtists(artistURIs);
                    artists.AddRange(artistsByURI);
                };

                //get all albums for the artists found
                var ppAlbums = new ProgressPrinter(Total: artists.Count(),
                                                   Update: (perc, time) => ConsoleWriteAndClearLine(cursorLeft, " -- " + AddOrdinal(playlistCount) + " playlist -- Getting albums: " + perc + ", " + time + " remaining")
                                                   );
                var albums = artists.Select(artist => spotify.Artists.GetAlbums(artist.Id).Result)
                    .Where(x => ppAlbums.PrintProgress())
                    .SelectMany(item => spotify.Paginate(item, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
                    .OrderBy(album => album.ReleaseDate)
                    .Take(MaxPlaylistSize) //no point in taking more albums than the amount of tracks that are allowed TODO update for album/track exclusions
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

                var playlistDetailsString = AddOrdinal(playlistCount);
                if (Settings._VerboseDebug)
                    playlistDetailsString = playlistByArtist.Key;

                //TODO update for album/track exclusions
                var ppTracks = new ProgressPrinter(Total: Math.Max(albums.Count(), MaxPlaylistSize),
                                                   Update: (perc, time) => ConsoleWriteAndClearLine(cursorLeft, " -- " + playlistDetailsString + " playlist -- Getting tracks: " + perc + ", " + time + " remaining")
                                                   );

                //TODO consider adding a caching GetAlbums method (like SpotifyClient.GetTracks) and using that below instead, as FullAlbum.Tracks exists
                //identify tracks in those albums
                var trackIDs = albums.Select(album => spotify.Albums.GetTracks(album.Id).Result)
                    .Where(x => ppTracks.PrintProgress())
                    .SelectMany(item => spotify.Paginate(item, new WaitPaginator(WaitTime: 500)).ToListAsync().Result)
                    .Select(track => track.Id) // this "track" is SimpleTrack rather than FullTrack; need a list of IDs to convert them to FullTrack
                    .Distinct()
                    .Take(MaxPlaylistSize) //no point in taking more tracks than the max TODO update for album/track exclusions
                    .ToList();

                //technically this will flag at one track BEFORE passing the max, but I think the loss of precision is worth the simplicity
                if (trackIDs.Count >= MaxPlaylistSize)
                {
                    tooLargePlaylistCount += 1;
                    verboseDebugOutput.Add(playlistByArtist.Key + " playlist has tracks beyond the max");
                }

                //get the tracks
                var tracks = (await spotify.GetTracks(trackIDs))
                    //ignore tracks by artists outside the spec if this is an "appears on" album, like a compilation
                    //this includes tracks that are on split albums, collaborations, and the like
                    .Where(t => !appearsOnAlbums.Contains(t.Album.Id) || t.Artists.Select(a => a.Name).Any(artistName => playlistByArtist.Value.Contains(artistName, StringComparer.InvariantCultureIgnoreCase)))
                    ////only include tracks strictly by artists specified
                    //.Where(t => t.Artists.Select(a => a.Name).Any(artistName => playlistByArtist.Value.Contains(artistName, StringComparer.InvariantCultureIgnoreCase)))
                    .ToList();

                verboseDebugOutput.Add(playlistByArtist.Key + " full discog: " +
                    artists.Count().ToString("#,##0") + " artists, " +
                    albums.Count().ToString("#,##0") + " albums, " +
                    tracks.Count().ToString("#,##0") + " tracks"
                    );

                //track artists that couldn't be found for reporting later
                //TODO update this for album/track exclusion logic
                var missingArtistsInThisPlaylist = playlistByArtist.Value.Where(artistName => !artists.Any(a => a.Name.ToLower() == artistName.ToLower())).ToList();
                missingArtists.AddRange(missingArtistsInThisPlaylist);

                //add the playlist
                playlistBreakdowns.Add(preface + playlistByArtist.Key, tracks);
                playlistCount += 1;
                pp.PrintProgress();
            }

            HandleExclusions(playlistBreakdowns, playlistsByArtists);

            missingArtists = missingArtists
                .Where(s => !s.StartsWith("-") && !idRegex.Match(s).Success)
                .ToList();

            Console.WriteLine();
            if (missingArtists.Any())
                Console.WriteLine("Could not find the following artists: " + missingArtists.Join(", "));

            if (tooLargePlaylistCount > 0)
                Console.WriteLine(tooLargePlaylistCount.ToString("#,##0") +
                    " playlist" + (tooLargePlaylistCount == 1 ? "" : "s") +
                    " reached Spotify's max playlist size of " +
                    MaxPlaylistSize.ToString("#,##0") + " tracks"
                    );

            if (Settings._VerboseDebug)
                Console.WriteLine(verboseDebugOutput.Join(Environment.NewLine));

            Console.WriteLine("Assembled " +
                playlistBreakdowns.Count().ToString("#,##0") +
                " playlists with " +
                playlistBreakdowns.Values.Sum(x => x.Count()).ToString("#,##0") +
                " tracks."
                );

            return playlistBreakdowns;
        }

        static async System.Threading.Tasks.Task<Dictionary<string, List<FullTrack>>> GetLikedTracksByGenre(SpotifyClient spotify)
        //static Dictionary<string, List<FullTrack>> GetLikesByArtistPlaylistBreakdowns(SpotifyClient spotify)
        {
            Console.WriteLine();
            Console.WriteLine("---likes by genre playlists---");

            var preface = "#Liked - ";
            var likesByGenrePath = System.IO.Path.Join(Settings._PlaylistFolderPath, "Likes By Genre");

            var playlistsByGenre = GetGenrePlaylistSetup(spotify, likesByGenrePath);
            var likedTracks = await spotify.GetLikedTracks();

            var playlistBreakdowns = new Dictionary<string, List<FullTrack>>();

            //TODO this is a duplicate api call for what generates the reports; consolidate and/or cache this
            var artistIDs = likedTracks.SelectMany(t => t.Artists.Select(a => a.Id))
                .Distinct()
                .ToList();

            //get the artists
            var artists = await spotify.GetArtists(artistIDs);

            var trackGenres = likedTracks.SelectMany(t => t.Artists.Select(a => new
            {
                TrackID = t.Id,
                Genres = artists.Where(ax => ax.Id == a.Id).Single().Genres
            }))
                .SelectMany(x => x.Genres.Select(g => new GenreReportRecord
                {
                    ItemID = x.TrackID,
                    GenreName = g
                }))
                .ToArray();

            var pp = new ProgressPrinter(playlistsByGenre.Count(), (perc, time) => ConsoleWriteAndClearLine("\rAssembling playlists: " + perc + ", " + time + " remaining"));
            foreach (var playlistBreakdown in playlistsByGenre)
            {
                var stringsToRemove = new string[] { " ", "-" };
                //TODO add a keyword/symbol for "contains" (that could work for any playlists)
                var trackIDs = trackGenres
                    .Where(x => playlistBreakdown.Value.Any(genreName => x.GenreName.ToLower().Remove(stringsToRemove) == genreName.ToLower().Remove(stringsToRemove)))
                    .Select(x => x.ItemID)
                    .ToArray();

                //add all liked tracks to the playlist breakdown
                var genreLikedTracks = likedTracks.Where(t => trackIDs.Contains(t.Id)).ToList();
                playlistBreakdowns.Add(preface + playlistBreakdown.Key, genreLikedTracks);
                pp.PrintProgress();
            }

            HandleExclusions(playlistBreakdowns, playlistsByGenre);

            Console.WriteLine("Assembled " +
                playlistBreakdowns.Count().ToString("#,##0") +
                " playlists with " +
                playlistBreakdowns.Values.Sum(x => x.Count()).ToString("#,##0") +
                " tracks."
                );

            return playlistBreakdowns;
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
            Console.WriteLine();
            Console.WriteLine("---playlist management---");

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
                    Console.WriteLine("Removed " + removePlaylists.Count.ToString("#,##0") + " playlists for re-creation.");

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
                    Console.WriteLine("Removed " + removePlaylists.Count.ToString("#,##0") + " orphaned playlists.");

            }

            foreach (var playlistBreakdown in playlistBreakdowns)
            {
                //complicated logic for determining duplicates
                var dupes = playlistBreakdown.Value
                    .GroupBy(track => track.Name.ToLower() + " $$$ " + track.Artists.Select(a => a.Name).Join(", ")) //same track name, same artist
                    .Where(group =>
                        group.Count() > 1 && // only dupes
                        group.Select(track => track.Album.Id).Distinct().Count() > 1 // not from the same album
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
                    .SelectMany(group => group.Where(track => track != group.First())) // remove all but the first track per group
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
            var pp = new ProgressPrinter(playlistBreakdowns.Count(), (perc, time) => ConsoleWriteAndClearLine("\rCreating playlists: " + perc + ", " + time + " remaining"));
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

                pp.PrintProgress();
            }

            Console.WriteLine();
            Console.WriteLine("Removed " + removedTracksCounter.ToString("#,##0") + " existing tracks.");
            Console.WriteLine("Added " + addedTracksCounter.ToString("#,##0") + " new tracks.");
            Console.WriteLine();


        }

        static async System.Threading.Tasks.Task LikedGenreReport(SpotifyClient spotify)
        {
            var likedTracks = await spotify.GetLikedTracks();

            // these properties return SimpleAlbum and SimpleArtist
            // therefore we're gathering IDs and converting to the full versions with genre properties below
            var albumIDs = likedTracks.Select(t => t.Album.Id)
                .Distinct()
                .ToList();
            var artistIDs = likedTracks.SelectMany(t => t.Artists.Select(a => a.Id))
                .Distinct()
                .ToList();

            //album genre is always null, so don't bother there
            ////get the albums
            //var albums = new List<FullAlbum>();
            //foreach (var idChunk in albumIDs.ChunkBy(12))
            //{
            //    var chunkAlbums = (await spotify.Albums.GetSeveral(new AlbumsRequest(idChunk))).Albums
            //        .ToList();

            //    albums.AddRange(chunkAlbums);
            //}

            //get the artists
            var artists = await spotify.GetArtists(artistIDs);

            //report records, assemble!

            var artistAllGenres = artists.SelectMany(a => a.Genres.Select(g => new GenreReportRecord
            {
                ItemID = a.Id,
                GenreName = g
            }))
                .ToArray();

            var artistFirstGenres = artists.Select(a => new GenreReportRecord
            {
                ItemID = a.Id,
                GenreName = a.Genres.FirstOrDefault()
            })
                .ToArray();

            //var albumAllGenres = albums.SelectMany(a => a.Genres.Select(g => new GenreReportRecord
            //{
            //    ItemID = a.Id,
            //    GenreName = g
            //}))
            //    .ToArray();

            //var albumFirstGenres = albums.Select(a => new GenreReportRecord
            //{
            //    ItemID = a.Id,
            //    GenreName = a.Genres.FirstOrDefault()
            //})
            //    .ToArray();

            var trackAllGenres = likedTracks.SelectMany(t => t.Artists.Select(a => new
            {
                TrackID = t.Id,
                Genres = artists.Where(ax => ax.Id == a.Id).Single().Genres
            }))
                .SelectMany(x => x.Genres.Select(g => new GenreReportRecord
                {
                    ItemID = x.TrackID,
                    GenreName = g
                }))
                .ToArray();

            var trackFirstGenres = likedTracks.SelectMany(t => t.Artists.Select(a => new GenreReportRecord
            {
                ItemID = t.Id,
                GenreName = artists.Where(ax => ax.Id == a.Id).Single().Genres.FirstOrDefault()
            }))
                .ToArray();

            //write reports!
            WriteGenreReport(artistAllGenres, System.IO.Path.Join(Settings._PlaylistFolderPath, "Reports", "All Artist Genres"));
            WriteGenreReport(artistFirstGenres, System.IO.Path.Join(Settings._PlaylistFolderPath, "Reports", "First Artist Genres"));
            //WriteGenreReport(albumAllGenres, System.IO.Path.Join(Settings._PlaylistFolderPath, "Reports", "All Album Genres"));
            //WriteGenreReport(albumFirstGenres, System.IO.Path.Join(Settings._PlaylistFolderPath, "Reports", "First Album Genres"));
            WriteGenreReport(trackAllGenres, System.IO.Path.Join(Settings._PlaylistFolderPath, "Reports", "All Track Genres"));
            WriteGenreReport(trackFirstGenres, System.IO.Path.Join(Settings._PlaylistFolderPath, "Reports", "First Track Genres"));

        }
    
        private static void WriteGenreReport(IEnumerable<GenreReportRecord> records, string path)
        {
            //gronk checks
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            var ext = System.IO.Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(ext))
                path = System.IO.Path.ChangeExtension(path, ".txt");

            var nullReplacementString = "[null]";

            var maxNameLength = records.Max(r => (r.GenreName ?? nullReplacementString).Length);

            //format report
            var reportLines = records
                .Where(x => x.GenreName != null)
                .GroupBy(x => x.GenreName)
                .Select(g => new
                {
                    GenreName = g.Key,
                    RecordCount = g.Count(),
                })
                .OrderByDescending(x => x.RecordCount)
                .ThenBy(x => x.GenreName)
                .Select(g =>
                    (g.GenreName ?? nullReplacementString) + new String(' ', maxNameLength - g.GenreName.Length) + " -- " + g.RecordCount.ToString("#,##0")
                )
                .ToArray()
                ;

            //write report
            System.IO.File.WriteAllLines(path, reportLines);
        }
        private class GenreReportRecord
        {
            public string GenreName { get; set; }
            public string ItemID { get; set; }
        }
    }
}
