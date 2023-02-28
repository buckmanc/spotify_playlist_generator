using System;
using SpotifyAPI.Web;
using IniParser;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading.Tasks;
using spotify_playlist_generator.Models;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Net.Mime;
using SixLabors.ImageSharp.Processing;
using System.CommandLine.Parsing;
using System.Drawing.Text;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;

namespace spotify_playlist_generator
{
    partial class Program
    {

        public static class Settings
        {
            public static string _PathsIniFolderPath;
            public static string _PlaylistFolderPath;
            public static string _CommentString = "#";      //TODO consider pulling this from a config file
            public static string _ParameterString = "@";      //TODO consider pulling this from a config file
            public static string _ExclusionString = "-";    //TODO consider pulling this from a config file
            public static string _SeparatorString = "-";    //TODO consider pulling this from a config file
            public static bool _NewPlaylistsPrivate;
            public static bool _RecreatePlaylists;
            public static bool _DeleteOrphanedPlaylists;
            public static bool _VerboseDebug;
            public static string _StartPlaylistsWith;
            public static string _ImageBackupFolderPath
            {
                get { return System.IO.Path.Join(Settings._PlaylistFolderPath, "Images", "Backup"); }
            }
            public static string _ImageWorkingFolderPath
            {
                get { return System.IO.Path.Join(Settings._PlaylistFolderPath, "Images", "Working"); }
            }

        }

        private const int MaxPlaylistSize = 11000; //max playlist size as of 2021-07-15; the api throws an error once you pass this
        public static Regex idRegex = new Regex(@"[a-zA-Z0-9]{22}");
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
        public static string AssemblyName
        {
            get
            {
                string output = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                return output;
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

        //TODO the descriptions are not making it to the command line help details
        /// <param name="playlistFolderPath">An alternate path for the playlists folder path. Overrides the value found in paths.ini.</param>
        /// <param name="listPlaylists">List existing playlists from the playlists folder.</param>
        /// <param name="playlistName">The name of the playlist to run alone, unless combined with --playlist-specs.</param>
        /// <param name="playlistSpecification">A playlist specification string for use when creating a new playlist from the command line. Must be combined with --playlist-name.</param>
        /// <param name="modifyPlaylistFile"></param>
        /// <param name="backupPlaylistImage"></param>
        /// <param name="addPlaylistNameToImage"></param>
        /// <param name="restorePlaylistImage"></param>
        static void Main(string playlistFolderPath, bool listPlaylists, string playlistName, string playlistSpecification, bool modifyPlaylistFile,
            bool backupPlaylistImage, bool restorePlaylistImage, bool addPlaylistNameToImage
            )
        {

            if (Debugger.IsAttached)
            {
                //playlistName = "Top - Female Fronted Black Metal Plus";
                //playlistName = "#Full Discog - Acoustic VGC";
                //addPlaylistNameToImage = true;
                //restorePlaylistImage = true;
                playlistName = "test";
            }

            Console.WriteLine();
            Console.WriteLine("Welcome to " + Program.AssemblyName + "! Please don't expect too much, but also be impressed.");
            Console.WriteLine("Starting at " + DateTime.Now.ToString());
            Console.WriteLine();

            var sw = Stopwatch.StartNew();


            Settings._PathsIniFolderPath = AssemblyDirectory;
            if (!string.IsNullOrWhiteSpace(playlistFolderPath))
            {
                Settings._PlaylistFolderPath = playlistFolderPath;
            }

            GetConfig();

            //if only looking at one playlist, don't delete all the others
            //that'd be a big yikes
            if (!string.IsNullOrWhiteSpace(playlistSpecification) || !string.IsNullOrWhiteSpace(playlistName))
                Program.Settings._DeleteOrphanedPlaylists = false;

            using var spotifyWrapper = new MySpotifyWrapper();

            var me = spotifyWrapper.spotify.UserProfile.Current().Result;
            Console.WriteLine($"Hello there, {me.DisplayName}");
            Console.WriteLine("----------------------");

            //get various playlist definitions
            //that is, a name and a list of tracks
            var playlistBreakdowns = new Dictionary<string, List<FullTrackDetails>>();
            var error = false;
            var errorTries = 0;
            var getPlaylistBreakdownsSuccess = false;
            var likedGenreReportSuccess = false;

            var playlistSpecs = ReadPlaylistSpecs(spotifyWrapper, listPlaylists, playlistName, playlistSpecification);

            //eventually would like to run this all the time, but it's problematic right now
            if (modifyPlaylistFile)
            {
                ModifyPlaylistSpecFiles(spotifyWrapper, playlistSpecs);
                Environment.Exit(0);
            }
            else if (backupPlaylistImage)
            {
                var dummyPlaylist = new FullPlaylist();
                BackupAndPrepPlaylistImage(spotifyWrapper, playlistName, out dummyPlaylist, OverwriteBackup: true);
                Environment.Exit(0);
            }
            else if (restorePlaylistImage)
            {
                RestorePlaylistImage(spotifyWrapper, playlistName);
                Environment.Exit(0);
            }
            else if (addPlaylistNameToImage)
            {
                AddPlaylistNameToImage(spotifyWrapper, playlistName);
                Environment.Exit(0);
            }


            //TODO move error handling inside MySpotifyWrapper
            do
            {
                try
                {
                    error = false;
                    if (!getPlaylistBreakdownsSuccess)
                    {
                        playlistBreakdowns.AddRange(GetPlaylistBreakdowns(spotifyWrapper, playlistSpecs));
                        getPlaylistBreakdownsSuccess = true;
                    }
                    if (!likedGenreReportSuccess)
                    {
                        LikedGenreReport(spotifyWrapper);
                        likedGenreReportSuccess = true;
                    }
                    

                }
                catch (Exception ex)
                {
                    if (ex.ToString().ToLower().Contains("access token expired"))
                    {
                        error = true;
                        errorTries += 1;
                        //set a new spotify client to get a fresh token
                        Console.WriteLine("Access token expired, resetting SpotifyClient to get a new access token.");
                        spotifyWrapper.RefreshSpotifyClient();
                    }
                    else
                        throw;
                }
            } while (error && errorTries < 1); // setting to 1 for now, until issues with api hits starting over from the beginning can be resolved

            //do work!
            UpdatePlaylists(spotifyWrapper, playlistBreakdowns);

            Console.WriteLine();
            Console.WriteLine("All done, get jammin'!");

            Console.WriteLine("Run took " + sw.Elapsed.ToHumanTimeString() + ", completed at " + DateTime.Now.ToString());
        }

        static void GetConfig()
        {
            var pathsIniPath = System.IO.Path.Join(Settings._PathsIniFolderPath, "paths.ini");
            var iniParser = new FileIniDataParser();

            //create paths file if it doesn't exist
            if (!System.IO.File.Exists(pathsIniPath))
            {
                var newFile = new IniParser.Model.IniData();
                newFile["SETTINGS"]["PlaylistFolderPath"] = AssemblyDirectory;
                iniParser.WriteFile(pathsIniPath, newFile);
            }

            //only read in the playlist folder path if it doesn't already exist
            //this allows for overriding by passing it as an argument
            if (string.IsNullOrEmpty(Settings._PlaylistFolderPath))
            {
                //read path config from file
                var pathsIni = iniParser.ReadFile(pathsIniPath);
                Settings._PlaylistFolderPath = pathsIni["SETTINGS"]["PlaylistFolderPath"];
            }


            //lazy developer shortcut for sharing files between two machines
            if (System.Diagnostics.Debugger.IsAttached)
                Settings._PlaylistFolderPath = Settings._PlaylistFolderPath.Replace("/media/content/", "Z:/");

            if (!System.IO.Directory.Exists(Settings._PlaylistFolderPath))
                System.IO.Directory.CreateDirectory(Settings._PlaylistFolderPath);

            var configIniPath = System.IO.Path.Join(Settings._PlaylistFolderPath, "config.ini");

            //create config file if it doesn't exist
            if (!System.IO.File.Exists(configIniPath))
            {
                var newFile = new IniParser.Model.IniData();
                newFile["SETTINGS"]["NewPlaylistsPrivate"] = "false";
                newFile["SETTINGS"]["RecreatePlaylists"] = "false";
                newFile["SETTINGS"]["DeleteOrphanedPlaylists"] = "true";
                newFile["SETTINGS"]["Verbose"] = "false";
                newFile["SETTINGS"]["StartPlaylistsWithString"] = String.Empty;
                iniParser.WriteFile(configIniPath, newFile);
            }

            //read config file
            var configIni = iniParser.ReadFile(configIniPath);

            //store settings
            Settings._NewPlaylistsPrivate = bool.Parse(configIni["SETTINGS"]["NewPlaylistsPrivate"]);
            Settings._RecreatePlaylists = bool.Parse(configIni["SETTINGS"]["RecreatePlaylists"]);
            Settings._DeleteOrphanedPlaylists = bool.Parse(configIni["SETTINGS"]["DeleteOrphanedPlaylists"]);
            Settings._VerboseDebug = bool.Parse(configIni["SETTINGS"]["Verbose"]);
            Settings._StartPlaylistsWith = configIni["SETTINGS"]["StartPlaylistsWithString"];

        }
        static List<PlaylistSpec> ReadPlaylistSpecs(MySpotifyWrapper spotifyWrapper, bool listPlaylists, string playlistName, string playlistSpecification)
        {


            var directoryPath = System.IO.Path.Join(Settings._PlaylistFolderPath, "Playlists");

            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);

                var exampleMetalPlaylist =
                    Settings._ParameterString + "default:LikesByGenre" + Environment.NewLine +
                    Settings._CommentString + " genres to be used in this playlist should be specified here" + Environment.NewLine +
                    Environment.NewLine +
                    "symphonic metal" + Environment.NewLine +
                    "melodic metal" + Environment.NewLine +
                    "power metal" + Environment.NewLine +
                    "progressive metal" + Environment.NewLine +
                    "-artist:Korpiklaani #korpiklaani is more folk metal than symphonic metal!"
                    ;

                var playlist = spotifyWrapper.GetUsersPlaylists()
                    .OrderByDescending(p => p.Followers.Total)
                    .ThenBy(p => p.Tracks.Total)
                    .FirstOrDefault();

                //TODO find one of the users playlists
                var playlistLikes = "LikesFromPlaylist:" + playlist.Id + " #" + playlist.Name;

                var likedTracks = spotifyWrapper.GetLikedTracks();

                var topLikedArtistNames = likedTracks
                    .SelectMany(t => t.ArtistNames)
                    .GroupBy(x => x)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .Take(10)
                    .ToArray();

                var exampleArtistLikesPlaylist =
                    Settings._ParameterString + "default:LikesByArtist" + Environment.NewLine +
                    topLikedArtistNames.Join(Environment.NewLine);

                var exampleGenreContainsPlaylist = "#black metal is great but watch out for not sees" + Environment.NewLine +
                    "LikesByGenreContains:black metal";

                var exampleFullDiscog =
                    Settings._ParameterString + "default:AllByArtist" + Environment.NewLine +
                    "Golden Light" + Environment.NewLine +
                    "Andvari" + Environment.NewLine +
                    "Cave Mouth" + Environment.NewLine +
                    "Crown of Asteria" + Environment.NewLine +
                    "Enon Chapel" + Environment.NewLine +
                    "Great Cold Emptiness" + Environment.NewLine +
                    "6hXonF1DK45IuMCemMiyD2 #Heksebrann" + Environment.NewLine +
                    "Iarnvidjur" + Environment.NewLine +
                    "Tomblord"
                    ;

                //write the example files
                System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Liked - Melodic Metal.txt"), exampleMetalPlaylist);
                System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Liked - Black Metal Genres.txt"), exampleGenreContainsPlaylist);
                System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Liked - " + playlist.Name + ".txt"), playlistLikes);
                System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Liked - Fav Artist.txt"), exampleArtistLikesPlaylist);
                System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Full Discog - Meghan Wood.txt"), exampleFullDiscog);
            }


            if (!listPlaylists && !string.IsNullOrWhiteSpace(playlistSpecification))
            {
                var output = new List<PlaylistSpec>();
                output.Add(new PlaylistSpec(playlistName, playlistSpecification));
                return output;
            }

            //TODO turn file system folders into Spotify playlist folders
            var files = System.IO.Directory.GetFiles(directoryPath, "*.txt", System.IO.SearchOption.AllDirectories);
            //read in playlist breakdowns
            var playlistSpecs = files.Select(path => new PlaylistSpec(path)).ToList();

            //print a nice little report if asked for it
            if (listPlaylists)
            {
                var reportText = "Playlists found in " + Program.Settings._PlaylistFolderPath + ":" + Environment.NewLine + Environment.NewLine;

                var maxNameLength = playlistSpecs.Max(p => p.PlaylistName.Length);

                reportText += playlistSpecs
                    .Select(p => p.PlaylistName + new string(' ', maxNameLength - p.PlaylistName.Length + 4) + p.SpecLines.Count().ToString("#,##0") + " lines")
                    .OrderBy(x => x)
                    .Join(Environment.NewLine);

                Console.WriteLine(reportText);
                Environment.Exit(0);
            }
            else if (!string.IsNullOrWhiteSpace(playlistName))
            {
                playlistSpecs = playlistSpecs
                    .Where(p => p.PlaylistName.Trim().ToLower() == playlistName.Trim().ToLower())
                    .ToList();
            }

            return playlistSpecs;
        }

        static void ModifyPlaylistSpecFiles(MySpotifyWrapper spotifyWrapper, IList<PlaylistSpec> playlistSpecs)
        {
            Console.WriteLine();
            Console.WriteLine("---making updates to playlist spec files---");
            Console.WriteLine("started at " + DateTime.Now.ToString());

            var likedTracks = spotifyWrapper.GetLikedTracks();

            //swap artist names for artist IDs
            //this will save multiple API hits involved in searching for artists by name and paging over the results
            //TODO nest your progress printers
            //var pp1 = new ProgressPrinter(playlistSpecs.Length, (perc, time) => ConsoleWriteAndClearLine("\rAdding artist IDs to playlist files: " + perc + ", " + time + " remaining"));
            foreach (var playlistSpec in playlistSpecs)
            {
                //----------- swap artist names for ids -----------

                var artistParameterNames = new string[]
                {
                    "LikesByArtist",
                    "AllByArtist",
                    "TopByArtist",
                    "-Artist"
                };

                var findFailureWarning = "Could not find this item. Remove this comment to try again";

                var artistNameLines = playlistSpec.SpecLines
                    .Where(line =>
                        line.IsValidParameter &&
                        artistParameterNames.Contains(line.ParameterName, StringComparer.InvariantCultureIgnoreCase) &&
                        !line.Comment.Contains(findFailureWarning) &&
                        !idRegex.Match(line.ParameterValue).Success
                    )
                    .ToArray();

                //no need to work further with artist names if none were found
                //TODO add a comment warning if the artist isn't found, then check for that warning string and exclude those later?
                //"delete this comment to try again"
                if (artistNameLines.Any())
                {

                    var likedArtistCounts = likedTracks
                        .SelectMany(t => t.ArtistIds)
                        .GroupBy(ID => ID)
                        .ToDictionary(g => g.Key, g => g.Count());

                    //pull out the artist names, chunk them so that progress is saved if the program crashes on your 1.3k artist playlist
                    var artistNameChunks = artistNameLines
                        .Select(line => line.ParameterValue)
                        .ChunkBy(50)
                        .ToArray()
                        ;

                    var pp1 = new ProgressPrinter(artistNameChunks.Length, (perc, time) => ConsoleWriteAndClearLine("\rAdding artist IDs to " + playlistSpec.PlaylistName + " : " + perc + ", " + time + " remaining"));
                    foreach (var artistNameChunk in artistNameChunks)
                    {

                        var artists = spotifyWrapper.GetArtistsByName(artistNameChunk);
                        //TODO make sure this .Contains call works
                        //TODO make sure it actually updates the SpecLines in playlistSpec
                        foreach (var line in playlistSpec.SpecLines.Where(line => artistNameLines.Contains(line)))
                        {
                            var matchingArtists = artists
                                .Where(a => a.Name.Trim().ToLower() == line.ParameterValue.Trim().ToLower())
                                .OrderByDescending(a => a.Popularity)
                                .ToArray();

                            if (matchingArtists.Any())
                            {
                                //reassemble the line with artist ID as the parameter value and the name in the comment
                                //multiple artists will add additional lines, sorted by popularity
                                line.RawLine = matchingArtists.Select(artist =>
                                    ((playlistSpec.DefaultParameter ?? String.Empty).ToLower() != line.ParameterName.ToLower() ? line.ParameterName + ":" : string.Empty) +
                                    artist.Id + " " + Program.Settings._CommentString + "  " + artist.Name +
                                    (matchingArtists.Count() > 1 ? (", " + artist.Genres.FirstOrDefault() ?? String.Empty) + (likedArtistCounts.ContainsKey(artist.Id) ? ", " + likedArtistCounts[artist.Id].ToString("#,##0") + " liked tracks" : String.Empty) : string.Empty) +
                                    (!string.IsNullOrWhiteSpace(line.Comment) ? new string('\t', 3) + line.Comment : string.Empty)
                                    ).Join(Environment.NewLine);
                            }
                            else
                            {
                                //if no artist was found, leave a warning in the file about it
                                line.RawLine =
                                    ((playlistSpec.DefaultParameter ?? String.Empty).ToLower() != line.ParameterName.ToLower() ? line.ParameterName + Program.Settings._SeparatorString : string.Empty) +
                                    line.ParameterValue + " " + Program.Settings._CommentString + " " + findFailureWarning +
                                    (!string.IsNullOrWhiteSpace(line.Comment) ? new string('\t', 3) + line.Comment : string.Empty)
                                    ;
                            }
                        }
                        //write changes
                        playlistSpec.Write();
                        playlistSpec.UpdateFromDisk();

                        pp1.PrintProgress();
                    }
                }
                //----------- add playlist names -----------


                var playlistLines = playlistSpec.SpecLines
                    .Where(line =>
                        line.IsValidParameter &&
                        string.IsNullOrEmpty(line.Comment) &&
                        line.ParameterName.Contains("playlist", StringComparison.InvariantCultureIgnoreCase) &&
                        idRegex.Match(line.ParameterValue).Success &&
                        !line.Comment.Contains(findFailureWarning) //unlikely
                    )
                    .ToArray();

                var playlists = playlistLines.Select(line => line.ParameterValue)
                    .Distinct()
                    .Select(ID => spotifyWrapper.spotify.Playlists.Get(ID).ResultSafe())
                    .Where(p => p != null)
                    .ToList();

                if (playlists.Any())
                {
                    //TODO make sure this .Contains call works
                    foreach (var line in playlistSpec.SpecLines.Where(line => playlistLines.Contains(line)))
                    {
                        var matchingPlaylist = playlists
                            .Where(p => p.Id.Trim().ToLower() == line.ParameterValue.Trim().ToLower())
                            .SingleOrDefault();

                        line.RawLine =
                                ((playlistSpec.DefaultParameter ?? String.Empty).ToLower() != line.ParameterName.ToLower() ? line.ParameterName + Program.Settings._SeparatorString : string.Empty) +
                                line.ParameterValue + " " +
                                Program.Settings._CommentString + " " + (matchingPlaylist != null ? matchingPlaylist.Name : findFailureWarning)
                                ;
                    }

                    //write changes
                    System.IO.File.WriteAllLines(playlistSpec.Path, playlistSpec.SpecLines.Select(line => line.RawLine));
                }

                //pp1.PrintProgress();
            }
            Console.WriteLine();
        }

        static string BackupAndPrepPlaylistImage(MySpotifyWrapper spotifyWrapper, string playlistName, out FullPlaylist playlist, bool OverwriteBackup)
        {
            playlist = null;

            if (string.IsNullOrWhiteSpace(playlistName))
            {
                Console.WriteLine("--playlist-name is required for playlist image operations");
                return null;
            }

            playlist = spotifyWrapper.GetUsersPlaylists(playlistName);

            if (playlist == null)
            {
                Console.WriteLine("No playlist named \"" + playlistName + "\" found.");
                return null;
            }

            if (!playlist.Images.Any())
            {
                Console.WriteLine("Playlist has no cover art.");
                return null;
            }

            var imagePath = spotifyWrapper.DownloadPlaylistImage(playlist, Settings._ImageWorkingFolderPath, playlist.Name);

            if (imagePath == null)
            {
                Console.WriteLine("Could not download playlist cover image.");
                return null;
            }

            var backupImagePath = System.IO.Path.Join(Settings._ImageBackupFolderPath, System.IO.Path.GetFileName(imagePath));

            if (!System.IO.File.Exists(backupImagePath) || OverwriteBackup)
            {
                if (!System.IO.Directory.Exists(Settings._ImageBackupFolderPath))
                    System.IO.Directory.CreateDirectory(Settings._ImageBackupFolderPath);

                System.IO.File.Copy(imagePath, backupImagePath);
            }

            return imagePath;
        }

        static void RestorePlaylistImage(MySpotifyWrapper spotifyWrapper, string playlistName)
        {
            if (!System.IO.Directory.Exists(Settings._ImageBackupFolderPath))
            {
                Console.WriteLine("No backup image found for " + playlistName + ".");
                return;
            }

            var backupImagePath = System.IO.Directory.GetFiles(Settings._ImageBackupFolderPath)
                .Where(filePath => System.IO.Path.GetFileNameWithoutExtension(filePath).ToLower() == playlistName.ToLower())
                .FirstOrDefault();

            if (!System.IO.File.Exists(backupImagePath))
            {
                Console.WriteLine("No backup image found for " + playlistName + ".");
                return;
            }

            spotifyWrapper.UploadPlaylistImage(playlistName, backupImagePath);

            System.IO.File.Delete(backupImagePath);
        }

        static void AddPlaylistNameToImage(MySpotifyWrapper spotifyWrapper, string playlistName)
        {
            FullPlaylist playlist;

            var imagePath = BackupAndPrepPlaylistImage(spotifyWrapper, playlistName, out playlist, OverwriteBackup: false);

            var textForArt = playlist.Name.Replace(" - ", Environment.NewLine);

            using (var img = SixLabors.ImageSharp.Image.Load(imagePath))
            {

                var fontSize = img.Height / 10;

                var edgeDistance = (int)Math.Round(img.Height * 0.033333, 0);
                //var test = SixLabors.Shapes.TextBuilder.GenerateGlyphs("yo", new SixLabors.Fonts.RendererOptions());

                //TODO actually pick a font
                Console.WriteLine("font families:");
                Console.WriteLine(SystemFonts.Families.Select(f => f.Name).Join(Environment.NewLine));

                FontFamily fontFamily = SystemFonts.Families.FirstOrDefault();
                var font = fontFamily.CreateFont((float)fontSize, FontStyle.Regular);

		        var lineCount = textForArt.Split(Environment.NewLine).Count();

                //const double pointToPixelRatio = 1.333333;
                //const double verticalLineSpacePercent = 0.05;
                const double pointToPixelRatio = 1;
                const double verticalLineSpacePercent = 0.25;

                var fontHeightNoSpacing = (fontSize * pointToPixelRatio) * lineCount;
                var lineSpacingAdjustmentPercent = (1 + ((lineCount - 1) * verticalLineSpacePercent));

                //TODO slap the font ratio to a constant?
                //line height should have some relation to font size
                var fontHeight = (float)(
			        (fontSize * pointToPixelRatio) * lineCount     //point to pixel conversion
                    * (1 + ((lineCount - 1) * verticalLineSpacePercent))    //line spacing adjustment for multiple lines
                                                        //* (1 + (lineCount - 1) )	//line spacing adjustment for multiple lines
                    )
			        ;

                if (Settings._VerboseDebug)
                {
                    Console.WriteLine("font size in points: " + fontSize.ToString());
                    Console.WriteLine("font height:         " + fontHeight.ToString());
                    Console.WriteLine("edge distance:       " + edgeDistance.ToString());
                    Console.WriteLine("cover height:        " + img.Height.ToString());
                    Console.WriteLine("y calc:              " + (img.Height - fontHeight - edgeDistance).ToString());
                }

                img.Mutate(x => x.DrawText(
                    textForArt,
                    font,
                    Brushes.Solid(Color.White),
                    Pens.Solid(Color.Black, 2f),
                    //new PointF(edgeDistance, img.Height - edgeDistance)
                    new PointF(edgeDistance, img.Height - fontHeight - edgeDistance)
                    //new PointF(10, 10)
                    ));

                img.Save(imagePath);
            }

            //if (System.Diagnostics.Debugger.IsAttached)
            //{
            //    Process.Start("explorer.exe", "\"" + imagePath + "\"");
            //}
            //else
            //{
                spotifyWrapper.UploadPlaylistImage(playlist, imagePath);
            //}
        }

        static Dictionary<string, List<FullTrackDetails>> GetPlaylistBreakdowns(MySpotifyWrapper spotifyWrapper, IList<PlaylistSpec> playlistSpecs)
        {
            Console.WriteLine();
            Console.WriteLine("---assembling playlist tracks---");
            Console.WriteLine("started at " + DateTime.Now.ToString());

            var likedTracks = spotifyWrapper.GetLikedTracks();

            var playlistBreakdowns = new Dictionary<string, List<FullTrackDetails>>();

            var pp = new ProgressPrinter(playlistSpecs.Count, (perc, time) => ConsoleWriteAndClearLine("\rAssembling playlists: " + perc + ", " + time + " remaining"));
            foreach (var playlistSpec in playlistSpecs)
            {

                // ------------ get tracks ------------
                var playlistTracks = new List<FullTrackDetails>();

                var likesByArtist = playlistSpec.GetParameterValues("LikesByArtist");
                if (likesByArtist.Any())
                {
                    var tracks = likedTracks.Where(t =>
                        t.ArtistNames.Any(artistName => likesByArtist.Contains(artistName, StringComparer.InvariantCultureIgnoreCase)) ||
                        t.ArtistIds.Any(artistID => likesByArtist.Contains(artistID, StringComparer.InvariantCultureIgnoreCase))
                        )
                        .ToArray();

                    playlistTracks.AddRange(tracks);
                }

                var likesByArtistFromPlaylist = playlistSpec.GetParameterValues("LikesByArtistFromPlaylist");
                if (likesByArtistFromPlaylist.Any())
                {
                    var artistIDs = spotifyWrapper.GetTracksByPlaylist(likesByArtistFromPlaylist)
                        .SelectMany(t => t.ArtistIds)
                        .Distinct()
                        .ToArray();

                    var tracks = likedTracks.Where(t =>
                        t.ArtistIds.Any(artistID => artistIDs.Contains(artistID, StringComparer.InvariantCultureIgnoreCase))
                        )
                        .ToArray();

                    playlistTracks.AddRange(tracks);
                }

                var likesFromPlaylist = playlistSpec.GetParameterValues("LikesFromPlaylist");
                if (likesFromPlaylist.Any())
                {
                    var tracks = spotifyWrapper.GetTracksByPlaylist(likesFromPlaylist)
                        .Where(t => likedTracks.Contains(t))
                        .ToArray();

                    playlistTracks.AddRange(tracks);
                }

                var allByArtist = playlistSpec.GetParameterValues("AllByArtist");
                if (allByArtist.Any())
                {
                    var tracks = spotifyWrapper.GetTracksByArtists(allByArtist);
                    playlistTracks.AddRange(tracks);
                }

                var allByArtistFromPlaylist = playlistSpec.GetParameterValues("AllByArtistFromPlaylist");
                if (allByArtistFromPlaylist.Any())
                {
                    var artistIDs = spotifyWrapper.GetTracksByPlaylist(allByArtistFromPlaylist)
                        .SelectMany(t => t.ArtistIds)
                        .Distinct()
                        .ToArray();

                    var tracks = spotifyWrapper.GetTracksByArtists(artistIDs);
                    playlistTracks.AddRange(tracks);
                }

                var topByArtist = playlistSpec.GetParameterValues("TopByArtist");
                if (topByArtist.Any())
                {
                    //TODO how many tracks does this method actually return? do we need to limit to 5?
                    var tracks = spotifyWrapper.GetTopTracksByArtists(topByArtist);
                    playlistTracks.AddRange(tracks);
                }

                var topByArtistFromPlaylist = playlistSpec.GetParameterValues("TopByArtistFromPlaylist");
                if (topByArtistFromPlaylist.Any())
                {
                    var artistIDs = spotifyWrapper.GetTracksByPlaylist(likesByArtistFromPlaylist)
                        .SelectMany(t => t.ArtistIds)
                        .Distinct()
                        .ToArray();

                    //TODO how many tracks does this method actually return? do we need to limit to 5?
                    var tracks = spotifyWrapper.GetTopTracksByArtists(artistIDs);
                    playlistTracks.AddRange(tracks);
                }

                var likesByGenre = playlistSpec.GetParameterValues("LikesByGenre");
                if (likesByGenre.Any())
                {
                    var stringsToRemove = new string[] { " ", "-" };
                    var likesByGenreStandardized = likesByGenre.Select(x => x.Remove(stringsToRemove)).ToArray();

                    var tracks = likedTracks.Where(t =>
                    t.ArtistGenres.Any(genreName => likesByGenreStandardized.Contains(genreName.Remove(stringsToRemove), StringComparer.InvariantCultureIgnoreCase))
                    )
                    .ToArray();

                    playlistTracks.AddRange(tracks);
                }

                var likesByGenreContains = playlistSpec.GetParameterValues("LikesByGenreContains");
                if (likesByGenreContains.Any())
                {
                    var stringsToRemove = new string[] { " ", "-" };
                    var likesByGenreStandardized = likesByGenreContains.Select(x => x.Remove(stringsToRemove)).ToArray();

                    var tracks = likedTracks.Where(t =>
                    t.ArtistGenres.Any(genreName => likesByGenreStandardized.Any(specGenre => genreName.Remove(stringsToRemove).Contains(specGenre, StringComparison.InvariantCultureIgnoreCase)))
                    )
                    .ToArray();

                    playlistTracks.AddRange(tracks);
                }

                //TODO mod the spec file here?

                // ------------ exclude tracks ------------
                var excludeArtists = playlistSpec.GetParameterValues("-Artist");
                if (excludeArtists.Any())
                {
                    playlistTracks.Remove(t =>
                        t.ArtistNames.Any(artistName => excludeArtists.Contains(artistName, StringComparer.InvariantCultureIgnoreCase)) ||
                        t.ArtistIds.Any(artistID => excludeArtists.Contains(artistID, StringComparer.InvariantCultureIgnoreCase))
                        );
                }

                var excludeAlbums = playlistSpec.GetParameterValues("-Album");
                if (excludeAlbums.Any())
                {
                    var separators = new string[] { Settings._SeparatorString, ":" };

                    var excludeAlbumDetails = excludeAlbums
                        .Select(x => x.Split(separators, 2, StringSplitOptions.TrimEntries))
                        .Where(x => x.Length == 2)
                        .Select(x => new
                        {
                            ArtistNameOrID = x[0],
                            AlbumName = x[1]
                        })
                        .ToArray();

                    var albumIDs = excludeAlbums
                        .Where(x => idRegex.Match(x).Success && !separators.Any(sep => x.Contains(sep)))
                        .ToArray();

                    playlistTracks.Remove(t =>
                        //artist check
                        (
                        t.ArtistNames.Any(artistName => excludeAlbumDetails.Any(exAlbum => exAlbum.ArtistNameOrID.ToLower() == artistName.ToLower())) ||
                        t.ArtistIds.Any(artistID => excludeAlbumDetails.Any(exAlbum => exAlbum.ArtistNameOrID.ToLower() == artistID.ToLower()))
                        )
                        && excludeAlbumDetails.Any(exAlbum => exAlbum.ArtistNameOrID.ToLower() == t.AlbumName.ToLower())
                        );

                    playlistTracks.Remove(t => albumIDs.Any(exAlbumID => t.AlbumId == exAlbumID));
                }

                var excludePlaylistTracks = playlistSpec.GetParameterValues("-PlaylistTracks");
                if (excludePlaylistTracks.Any())
                {
                    var tracks = spotifyWrapper.GetTracksByPlaylist(excludePlaylistTracks)
                        .ToArray();
                    playlistTracks.RemoveRange(tracks);
                }

                var excludePlaylistArtists = playlistSpec.GetParameterValues("-PlaylistArtists");
                if (excludePlaylistArtists.Any())
                {
                    var artistIDs = spotifyWrapper.GetTracksByPlaylist(excludePlaylistArtists)
                        .SelectMany(t => t.ArtistIds)
                        .Distinct()
                        .ToArray();

                    playlistTracks.Remove(t =>
                        t.ArtistIds.Any(artistID => artistIDs.Contains(artistID, StringComparer.InvariantCultureIgnoreCase))
                        );
                }


                // ------------ remove dupes ------------
                //complicated logic for determining duplicates
                var dupes = playlistTracks
                    .GroupBy(track => track.Name.ToLower() + " $$$ " + track.ArtistNames.Join(", ")) //same track name, same artist
                    .Where(group =>
                        group.Count() > 1 && // only dupes
                        group.Select(track => track.AlbumId).Distinct().Count() > 1 // not from the same album
                                                                                     //do a time comparison as well to test for fundamental differences, but think about how this effects live albums
                        )
                    .Select(group => group
                        .OrderByDescending(track => track.AlbumType == "album") //albums first
                        .ThenBy(track => track.ReleaseDate) // older albums first; this should help de-prioritize deluxe releases and live albums
                        //TODO put some serious thought into how to best handle live albums
                        .ToList()
                        )
                    .ToList();

                var removeTracks = dupes
                    .SelectMany(group => group.Where(track => track != group.First())) // remove all but the first track per group
                    .ToList();
                playlistTracks.RemoveRange(removeTracks);


                // ------------ sort ------------
                var likesSpecLines = playlistSpec.SpecLines
                    .Where(line => line.IsValidParameter && line.ParameterName.Contains("likes", StringComparison.InvariantCultureIgnoreCase))
                    .Count();
                var allTracksSpecLines = playlistSpec.SpecLines
                    .Where(line => line.IsValidParameter && line.ParameterName.Contains("all", StringComparison.InvariantCultureIgnoreCase))
                    .Count();

                //these sorts won't preserve over time
                //for example, what if an old album is added to spotify for a full discog playlist?
                //unless UpdatePlaylists is modified it'll still be at the very bottom
                if (likesSpecLines > allTracksSpecLines)
                {
                    playlistTracks = playlistTracks
                        .OrderByDescending(t => t.LikedAt)
                        .ToList();
                }
                else
                {
                    playlistTracks = playlistTracks
                        .OrderBy(t => t.ReleaseDate)
                        .ThenBy(t => t.ArtistNames.First())
                        .ThenBy(t => t.AlbumName)
                        .ThenBy(t => t.TrackNumber)
                        .ToList();  

                }

                //keep playlists from overflowing
                if (playlistTracks.Count > Program.MaxPlaylistSize)
                {
                    playlistTracks = playlistTracks.Take(Program.MaxPlaylistSize).ToList();
                    //TODO warn
                    //TODO take from the beginning instead of the end
                }

                playlistBreakdowns.Add((Settings._StartPlaylistsWith ?? string.Empty) + playlistSpec.PlaylistName, playlistTracks);

                pp.PrintProgress();

            }

            Console.WriteLine();

            Console.WriteLine();
            Console.WriteLine("Assembled track list for " + playlistBreakdowns.Count.ToString("#,##0") + " playlists " +
                "with an average of " + playlistBreakdowns.Average(kvp => kvp.Value.Count).ToString("#,##0.00") + " tracks per playlist."
                );
            Console.WriteLine();

            return playlistBreakdowns;
        }

        static void UpdatePlaylists(MySpotifyWrapper spotifyWrapper, Dictionary<string, List<FullTrackDetails>> playlistBreakdowns)
        {
            Console.WriteLine();
            Console.WriteLine("---updating spotify---");

            var allPlaylists = spotifyWrapper.GetUsersPlaylists();

            //dump all playlists here if settings say to recreate them
            if (Settings._RecreatePlaylists)
            {
                var removePlaylists = allPlaylists.Where(p => playlistBreakdowns.Any(b => b.Key == p.Name)).ToList();

                //dump the playlists
                foreach (var playlist in removePlaylists)
                {
                    spotifyWrapper.spotify.Follow.UnfollowPlaylist(playlist.Id);
                    allPlaylists.RemoveRange(removePlaylists);
                }

                if (removePlaylists.Any())
                    Console.WriteLine("Removed " + removePlaylists.Count.ToString("#,##0") + " playlists for re-creation.");

            }

            //remove orphaned playlists
            else if (Settings._DeleteOrphanedPlaylists)
            {
                var removePlaylists = allPlaylists.Where(p =>
                    p.Description.Contains(Program.AssemblyName)
		    && !playlistBreakdowns.Keys.Contains(p.Name)
                    ).ToList();

                //dump the playlists
                foreach (var playlist in removePlaylists)
                {
                    spotifyWrapper.spotify.Follow.UnfollowPlaylist(playlist.Id);
                    allPlaylists.RemoveRange(removePlaylists);
                }

                if (removePlaylists.Any())
                    Console.WriteLine("Removed " + removePlaylists.Count.ToString("#,##0") + " orphaned playlists.");

            }

            var createPlaylistCounter = 0;
            var removedTracksCounter = 0;
            var addedTracksCounter = 0;

            //iterating through rather than running in bulk with linq to *hopefully* be a little more memory efficient
            //order by descending playlist name to get alphabetical playlists in the Spotify interface
            var pp = new ProgressPrinter(playlistBreakdowns.Count, (perc, time) => ConsoleWriteAndClearLine("\rCreating playlists: " + perc + ", " + time + " remaining"));
            foreach (var playlistBreakdown in playlistBreakdowns.OrderByDescending(kvp => kvp.Key).ToList())
            {
                //find this playlist by name
                //99% of the time there will only be one, but it's *possible* for two playlists to share a name
                //that was confusing as hell to discover
                var playlist = allPlaylists
                    .Where(p => p.Name == playlistBreakdown.Key)
                    .OrderByDescending(p => p.Description.Contains(Program.AssemblyName))
                    .FirstOrDefault();

                //create playlist if missing
                if (playlist is null)
                {
                    var playlistRequest = new PlaylistCreateRequest(playlistBreakdown.Key);
                    playlistRequest.Description = "Automatically generated by " + Program.AssemblyName + ".";
                    playlistRequest.Public = !Settings._NewPlaylistsPrivate;
                    var newPlaylist = spotifyWrapper.spotify.Playlists.Create(spotifyWrapper.spotify.UserProfile.Current().Result.Id, playlistRequest).Result;

                    playlist = newPlaylist;

                    createPlaylistCounter += 1;
                }

                //get all the tracks that ARE in the playlist - doing this here as they have to be casted from PlaylistTrack to FullTrack to be useful
                var playlistTracksCurrent = (spotifyWrapper.spotify.Paginate(playlist.Tracks).ToListAsync()).Result
                    .Select(x => (FullTrack)x.Track)
                    .ToList()
                    ;

                //main work part 1 - remove existing playlist tracks that no longer belong
                var removeTrackRequestItems = playlistTracksCurrent
                    .Where(gpt => !playlistBreakdown.Value.Any(glt => glt.TrackId == gpt.Id))
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
                        spotifyWrapper.spotify.Playlists.RemoveItems(playlist.Id, removeRequest);

                    removedTracksCounter += removeTrackRequestItems.Count;
                }



                //main work part 2 - add new tracks to playlists
                var addTrackURIs = playlistBreakdown.Value
                    .Where(glt => !playlistTracksCurrent.Any(gpt => gpt.Id == glt.TrackId))
                    //.Select(glt => new PlaylistAddItemsRequest.Item() { Uri = glt.Uri }) //add track requires URIs, whereas remove track requires a custom object based on URIs
                    .Select(glt => glt.TrackUri)
                    .ToList();

                if (addTrackURIs.Any())
                {
                    //the API only accepts 100 tracks at a time, so divide up and run each set of 100 separately
                    var addRequests = addTrackURIs
                        .ChunkBy(100)
                        .Select(uris => new PlaylistAddItemsRequest(uris))

                        .ToList();
                    foreach (var addRequest in addRequests)
                        spotifyWrapper.spotify.Playlists.AddItems(playlist.Id, addRequest);

                    addedTracksCounter += addTrackURIs.Count;
                }

                pp.PrintProgress();
            }

            Console.WriteLine();
            Console.WriteLine("Removed " + removedTracksCounter.ToString("#,##0") + " existing tracks.");
            Console.WriteLine("Added " + addedTracksCounter.ToString("#,##0") + " new tracks.");
            Console.WriteLine();


        }

        static void LikedGenreReport(MySpotifyWrapper spotifyWrapper)
        {
            Console.WriteLine();
            Console.WriteLine("---liked genre report---");
            Console.WriteLine("started at " + DateTime.Now.ToString());

            var likedTracks = spotifyWrapper.GetLikedTracks();

            //TODO simplify this; the data you need is already in likedTracks
            var artistIDs = likedTracks.SelectMany(t => t.ArtistIds)
                .Distinct()
                .ToList();

            //get the artists
            var artists = spotifyWrapper.GetArtists(artistIDs);

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

            var trackAllGenres = likedTracks
                .SelectMany(t => t.ArtistGenres.Select(g => new GenreReportRecord
                {
                    ItemID = t.TrackId,
                    GenreName = g
                }))
                .ToArray();

            var trackFirstGenres = likedTracks
                .Select(t => new GenreReportRecord
                {
                    ItemID = t.TrackId,
                    GenreName = t.ArtistGenres.FirstOrDefault()
                })
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

            if (records == null || !records.Any())
            {
                var text = "Nothing to write. Did something go wrong?";
                System.IO.File.WriteAllText(path, text);
                return;
            }

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
