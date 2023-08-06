using Figgle;
using IniParser;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using spotify_playlist_generator.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using VaderSharp2;

namespace spotify_playlist_generator
{
    partial class Program
    {

        public static class Settings
        {
            public static string _PathsIniFolderPath;
            public static string _PlaylistFolderPath;
            public static string _CommentString = "#";      //TODO consider pulling this from a config file
            public static string _OptionString = "@";      //TODO consider pulling this from a config file
            public static string _ExclusionString = "-";    //TODO consider pulling this from a config file
            public static string _SeparatorString = "-";    //TODO consider pulling this from a config file
            public static bool _NewPlaylistsPrivate;
            public static bool _RecreatePlaylists;
            public static bool _DeleteOrphanedPlaylists;
            public static bool _VerboseDebug;
            public static string _StartPlaylistsWith;
            public static bool _UpdateSort;
            public static string _DefaultFont = "*pressstart*";

            public static string _LyricCommand1;
            public static string _LyricCommand2;
            public static string _LyricCommand3;
            public static string _LyricFailText1;
            public static string _LyricFailText2;
            public static string _LyricFailText3;

            public static string _ImageBackupFolderPath
            {
                get { return System.IO.Path.Join(Settings._PlaylistFolderPath, "Images", "Backup"); }
            }
            public static string _ImageWorkingFolderPath
            {
                get { return System.IO.Path.Join(Settings._PlaylistFolderPath, "Images", "Working"); }
            }
            public static string _ImageTestFolderPath
            {
                get { return System.IO.Path.Join(Settings._PlaylistFolderPath, "Images", "Test"); }
            }
            public static string _ReportsFolderPath
            {
                get { return System.IO.Path.Join(Settings._PlaylistFolderPath, "Reports"); }
            }
            public static string _CacheFolderPath
            {
                get { return System.IO.Path.Join(Program.AssemblyDirectory, "caches"); }
            }
            public static string _TokensIniPath
            {
                get 
                {
                    var assemblyTokensPath = System.IO.Path.Join(AssemblyDirectory, "tokens.ini");
                    
                    // prefer the local tokens path if it exists
                    if (System.IO.File.Exists(assemblyTokensPath))
                    {
                        if (Settings._VerboseDebug)
                            Console.WriteLine("using local tokens file");
                        return assemblyTokensPath;
                    }

                    return System.IO.Path.Join(Settings._PlaylistFolderPath, "tokens.ini"); 
                }
            }

        }

        public static class Tokens
        {
            public static string NasaKey;
            public static string UnsplashAccessKey;
            public static string UnsplashSecretKey;
        }

        enum TextAlignment
        {
            BottomLeft,
            BottomRight,
            TopLeft,
            TopRight,
            Center,
        }

        public static DateTime RunStart;
        private const int MaxPlaylistSize = 11000; //max playlist size as of 2021-07-15; the api throws an error once you pass this
        public static Regex idRegex = new Regex(@"[a-zA-Z0-9]{22}");
        public static Regex urlRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
        //hyphen minus, en dash, em dash, and minus
        public static string[] dashes = new string[] { "-", "–", "—", "−" }.Distinct().ToArray();

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
        public static string ProjectPath
        {
            get
            {
                return Program.AssemblyDirectory?.Split(new char[] { '/', '\\' })?.SkipLast(3)?.Join("/");
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
        public static string TitleText_Large()
        {
            var output = Program.AssemblyName
                .Replace("_", " ")
                .Split(" ")
                .Select(word => FiggleFonts.Standard.Render(word))
                .Join(Environment.NewLine);

            return output;
        }
        public static string TitleText_Medium()
        {
            var vowels = "aeiou".ToArray();
            var output = Program.AssemblyName
                .Replace("_", " ")
                .Split(" ")
                .Select(word => FiggleFonts.Standard.Render(word.Substring(0, 4).TrimEnd(vowels)))
                .Join(Environment.NewLine);

            return output;
        }
        public static string TitleText_Small()
        {
            var output = Program.AssemblyName
                .Replace("_", " ")
                .Split(" ")
                .Select(word => "[" + word + "]")
                .Join(Environment.NewLine);

            return output;
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

        //TODO the descriptions are not making it to the command line help details
        /// <summary>A file based way to add smart playlists to Spotify.</summary> //--help breaks without a summary
        /// <param name="playlistFolderPath">An alternate path for the playlists folder path. Overrides the value found in paths.ini.</param>
        /// <param name="listPlaylists">List existing playlists from the playlists folder.</param>
        /// <param name="playlistName">The name of the playlist to run alone, unless combined with --playlist-specs. Supports wildcards.</param>
        /// <param name="playlistSpec">A playlist specification string for use when creating a new playlist from the command line.</param>
        /// <param name="modifyPlaylistFile">Exchange artist names for artist IDs. Saves time when running but looks worse.</param>
        /// <param name="excludeCurrentArtist">Adds an exclusion line for the currenly playing artist into the playlist. If no --playlist-name is specified the current playlist is used. Intended for refining playlists.</param>
        /// <param name="excludeCurrentAlbum">Adds an exclusion line for the currenly playing album into the playlist. If no --playlist-name is specified the current playlist is used. Intended for refining playlists.</param>
        /// <param name="excludeCurrentTrack">Adds an exclusion line for the currenly playing track into the playlist. If no --playlist-name is specified the current playlist is used. Intended for refining playlists.</param>
        /// <param name="imageAddPhoto">Assign a new image to the playlist.</param>
        /// <param name="imageAddText">Add text to the playlist image. The playlist name is used if no --image-text is provided.</param>
        /// <param name="imageText">Custom text to use for --image-add-text.</param>
        /// <param name="imageFont">Font to use for --image-add-text. Can use wildcards.</param>
        /// <param name="imageBackup">Happens automatically whenever modifying an image. Calling --image-backup directly overwrites previous backups.</param>
        /// <param name="imageRestore">Restore a previously backed up image.</param>
        /// <param name="imageTextAlignment">Pick one of the corners or center. Defaults to bottom left.</param>
        /// <param name="imageRotateDegrees">Rotate the image. You should really stick to 90 degree increments.</param>
        /// <param name="imageClone">Clone the cover art from another of your playlists.</param>
        /// <param name="imageNerdFontGlyph">Wildcard search for a Nerd Font glyph. Returns the symbol when used alone or used as the text with --image-add-text. You still have to specify an installed Nerd Font with --image-font. </param>
        /// <param name="testImages">Generate x test images instead of updating live playlists. Stored in the playlist folder.</param>
        /// <param name="play">Play --playlist-name. If no playlist is provided, toggle playback. Can be used with --playlist-spec to build a new playlist and play it afterward.</param>
        /// <param name="skipNext">Skip forward.</param>
        /// <param name="skipPrevious">Skip backward.</param>
        /// <param name="like">Like the current track.</param>
        /// <param name="unlike">Unlike the current track.</param>
        /// <param name="what">Print details about the current track.</param>
        /// <param name="whatElse">Print more details about the current track.</param>
        /// <param name="reports">Run only the reports.</param>
        /// <param name="lyrics">Pass currently playing info to an external lyrics app specified in the config file.</param>
        /// <param name="tabCompletionArgumentNames">A space delimited list of these arguments to pass to the bash "complete" function.</param>
        /// <param name="updateReadme">Update readme.md. Only used in development.</param>
        /// <param name="verbose">Print more logging messages. Pretty messy. Overrides the settings file.</param>
        /// <param name="commitAnActOfUnspeakableViolence">I wouldn't really do it... would I?</param>
        /// <returns></returns>
        static void Main(string playlistFolderPath, bool listPlaylists, string playlistName, string playlistSpec,
            bool modifyPlaylistFile,
            bool excludeCurrentArtist, bool excludeCurrentAlbum, bool excludeCurrentTrack,
            bool imageAddPhoto, bool imageAddText,
            string imageText, string imageFont,
            bool imageBackup, bool imageRestore, TextAlignment imageTextAlignment, int imageRotateDegrees, string imageClone, string imageNerdFontGlyph,
            int testImages,
            bool play, bool skipNext, bool skipPrevious, bool like, bool unlike, bool what, bool whatElse,
            bool reports,
            bool lyrics,
            bool tabCompletionArgumentNames, bool updateReadme,
            bool? verbose,
            bool commitAnActOfUnspeakableViolence
            )
        {
            Program.RunStart = DateTime.Now;

            if (Debugger.IsAttached)
            {
                //imageAddPhoto = true;
                //imageAddText = true;
                //imageNerdFontGlyph = "random";
                //imageFont = "*NFM*";
                //imageTextAlignment = TextAlignment.Center;
                //testImages = 10;
                //verbose = false;
                //playlistName = "*master*lofi*";
                playlistName = "*no gamechops";
            }

            if (tabCompletionArgumentNames)
            {
                Console.Write(Help.TabCompletionArgumentNames);
                return;
            }
            else if (updateReadme)
            {
                UpdateReadme();
                return;
            }

            // testImages is excluded from imageCommand so that defaults for it can be set below
            var excludeCurrent = new bool[] { excludeCurrentArtist, excludeCurrentAlbum, excludeCurrentTrack }.Any(x => x);
            var imageCommand = new bool[] { imageBackup, imageRestore, imageAddText, imageAddPhoto, imageRotateDegrees != 0, !string.IsNullOrWhiteSpace(imageClone) }.Any(x => x);

            var playerCommand = new bool[] {
                (play && string.IsNullOrEmpty(playlistSpec)),
                skipNext, skipPrevious, like, unlike, what, whatElse, lyrics, excludeCurrent
                }.Any(x => x);
            var shortRun = new bool[] {
                modifyPlaylistFile, excludeCurrent, imageCommand, testImages > 0, !string.IsNullOrWhiteSpace(imageNerdFontGlyph), lyrics, reports, commitAnActOfUnspeakableViolence
                }.Any(x => x);

            if (!playerCommand)
            {
                Console.WriteLine();
                Console.WriteLine("Welcome to ");
                Console.WriteLine(Program.TitleText_Large()
                    .Split(Environment.NewLine)
                    .Select(line => line.SoftSubstring(0, Console.WindowWidth))
                    //.RemoveSubsequentDupeLines(blankLinesOnly: true)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Join(Environment.NewLine)
                    );
                Console.WriteLine();
                Console.WriteLine("Please don't expect too much, but also be impressed.");
                Console.WriteLine("Starting at " + Program.RunStart.ToShortDateTimeString());
                Console.WriteLine();
            }
            var sw = Stopwatch.StartNew();

            //important to set these paths BEFORE reading the config file
            Settings._PathsIniFolderPath = AssemblyDirectory;
            if (!string.IsNullOrWhiteSpace(playlistFolderPath))
            {
                Settings._PlaylistFolderPath = playlistFolderPath;
            }


            GetConfig();

            if (verbose.HasValue)
                Program.Settings._VerboseDebug = verbose.Value;

            //make output a little more concise for player commands
            //i mean it's pretty brief already, right?
            if (playerCommand && Program.Settings._VerboseDebug)
                Program.Settings._VerboseDebug = false;

            //important to deal with this dir AFTER reading the config file
            //clear out any working image files from last time, let them persist per session
            if (System.IO.Directory.Exists(Program.Settings._ImageWorkingFolderPath))
                System.IO.Directory.Delete(Program.Settings._ImageWorkingFolderPath, true);

            //if only looking at one playlist, don't delete all the others
            //that'd be a big yikes
            if (!string.IsNullOrWhiteSpace(playlistSpec) || !string.IsNullOrWhiteSpace(playlistName))
                Program.Settings._DeleteOrphanedPlaylists = false;

            using var spotifyWrapper = new MySpotifyWrapper();

            if (!playerCommand)
            {
                Console.WriteLine("Hello there, " + spotifyWrapper.CurrentUser.DisplayName);
                Console.WriteLine();
                Console.WriteLine("----------------------");
                Console.WriteLine();
            }

            if (playlistName?.Trim()?.ToLower() == "current"
                || (excludeCurrent && string.IsNullOrWhiteSpace(playlistName))
                )
            {
                var currentPlaylist = spotifyWrapper.GetCurrentPlaylist();
                if (currentPlaylist == null)
                {
                    Console.WriteLine("Current playlist is invalid.");
                    Environment.Exit(-1);
                }

                playlistName = currentPlaylist.Name;
            }

            var playlistSpecs = ReadPlaylistSpecs(spotifyWrapper, listPlaylists, playlistName, playlistSpec, dontWarn: shortRun || playerCommand);
            var leaveImageAlonePlaylistNames = playlistSpecs
                .Where(spec => spec.LeaveImageAlone)
                .Select(p => p.FinalPlaylistName)
                .ToArray();

            // ------------ player commands ------------

            if (play && (shortRun || string.IsNullOrWhiteSpace(playlistName)))
                spotifyWrapper.Play(playlistName);

            if (excludeCurrentArtist)
                ExcludeCurrent(spotifyWrapper, playlistSpecs, "artist");

            if (excludeCurrentAlbum)
                ExcludeCurrent(spotifyWrapper, playlistSpecs, "album");

            if (excludeCurrentTrack)
                ExcludeCurrent(spotifyWrapper, playlistSpecs, "track");

            if (lyrics)
                Lyrics(spotifyWrapper);

            if (like)
                spotifyWrapper.LikeCurrent(like: true);

            if (unlike)
                spotifyWrapper.LikeCurrent(like: false);

            if (what || whatElse)
                spotifyWrapper.PrintCurrent(whatElse);

            if (skipNext)
                spotifyWrapper.SkipNext();

            if (skipPrevious)
                spotifyWrapper.SkipPrevious();

            // ------------ other commands ------------

            if (modifyPlaylistFile)
                ModifyPlaylistSpecFiles(spotifyWrapper, playlistSpecs, modifyPlaylistFile);
            // ------------ other commands ------------

            if (reports)
                RunReports(spotifyWrapper);

            if (commitAnActOfUnspeakableViolence)
                CommitAnActOfUnspeakableViolence(spotifyWrapper);

            List<FullPlaylist> newPlaylists = null;
            // if any of those fancy commands besides the main functionality have been processed
            // then skip the main functionality
            if (!shortRun && !playerCommand)
            {

                //do work!
                GetPlaylistBreakdowns(spotifyWrapper, playlistSpecs);
                UpdatePlaylists(spotifyWrapper, playlistSpecs, out newPlaylists);

                if (string.IsNullOrWhiteSpace(playlistName))
                    RunReports(spotifyWrapper);

                //refresh the users playlist cache before doing more playlist operations
                //as new playlists won't be in it
                if (newPlaylists.Any())
                    spotifyWrapper.GetUsersPlaylists(true);
            }

            // ------------ image commands ------------

            // create default artwork for new playlists
            if (!imageCommand &&
                (1 == 2
                || (newPlaylists?.Any() ?? false)
                || testImages > 0
                ))
            {
                imageCommand = true;
                imageAddPhoto = true;
                imageAddText = true;
            }

            if (imageCommand)
            {
                // loop is a hacky way of handling scenarios
                var imagePlaylistNames = new List<string>();
                // #1 playlist name was provided
                if (!string.IsNullOrWhiteSpace(playlistName))
                    imagePlaylistNames.Add(playlistName);
                // #2 default image creation when playlist name was not provided
                else if (newPlaylists?.Any() ?? false)
                    imagePlaylistNames.AddRange(newPlaylists.Select(p => p.Name).Distinct());
                // #3 test image gen or unknowns
                else
                    imagePlaylistNames.Add(null);

                foreach (var imagePlaylistName in imagePlaylistNames)
                    ImageTools.HandleImageStuff(
                         spotifyWrapper: spotifyWrapper, playlistName: imagePlaylistName, leaveImageAlonePlaylistNames: leaveImageAlonePlaylistNames,
                        imageAddPhoto: imageAddPhoto,
                        imageAddText: imageAddText, imageText: imageText, imageFont: imageFont, imageTextAlignment: (ImageTools.TextAlignment)Enum.Parse(typeof(ImageTools.TextAlignment), imageTextAlignment.ToString()),
                        imageRotateDegrees: imageRotateDegrees, imageClone: imageClone,
                        imageNerdFontGlyph: imageNerdFontGlyph,
                        testImages: testImages
                        );
            }

            if (imageRestore)
                ImageTools.RestorePlaylistImage(spotifyWrapper, playlistName, leaveImageAlonePlaylistNames);

            if (imageBackup)
                ImageTools.BackupAndPrepPlaylistImage(spotifyWrapper, playlistName, leaveImageAlonePlaylistNames, OverwriteBackup: true);


            if (!shortRun && !playerCommand)
            {
                if (play)
                {
                    if (newPlaylists.Any())
                        spotifyWrapper.Play(newPlaylists.First().Name);
                    else if (playlistSpecs.Any())
                        spotifyWrapper.Play(playlistSpecs.First().PlaylistName);
                }

                Console.WriteLine();
                Console.WriteLine("All done, get jammin'!");

                Console.WriteLine("Run took " + sw.Elapsed.ToHumanTimeString() + ", completed at " + DateTime.Now.ToShortDateTimeString());
            }
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
                newFile["SETTINGS"]["StartPlaylistsWithString"] = "#";
                newFile["SETTINGS"]["UpdateSort"] = "false";

                newFile["LYRICCOMMANDS"]["LyricCommand1"] = String.Empty;
                newFile["LYRICCOMMANDS"]["LyricFailText1"] = String.Empty;
                newFile["LYRICCOMMANDS"]["LyricCommand2"] = String.Empty;
                newFile["LYRICCOMMANDS"]["LyricFailText2"] = String.Empty;
                newFile["LYRICCOMMANDS"]["LyricCommand3"] = String.Empty;
                newFile["LYRICCOMMANDS"]["LyricFailText4"] = String.Empty;
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
            Settings._UpdateSort = bool.Parse(configIni["SETTINGS"]["UpdateSort"]);

            Settings._LyricCommand1 = configIni["LYRICCOMMANDS"]["LyricCommand1"];
            Settings._LyricCommand2 = configIni["LYRICCOMMANDS"]["LyricCommand2"];
            Settings._LyricCommand3 = configIni["LYRICCOMMANDS"]["LyricCommand3"];
            Settings._LyricFailText1 = configIni["LYRICCOMMANDS"]["LyricFailText1"];
            Settings._LyricFailText2 = configIni["LYRICCOMMANDS"]["LyricFailText2"];
            Settings._LyricFailText3 = configIni["LYRICCOMMANDS"]["LyricFailText3"];

        }
        static List<PlaylistSpec> ReadPlaylistSpecs(MySpotifyWrapper spotifyWrapper, bool listPlaylists, string playlistName, string playlistSpec, bool dontWarn)
        {


            var directoryPath = System.IO.Path.Join(Settings._PlaylistFolderPath, "Playlists");

            if (!System.IO.Directory.Exists(directoryPath))
            {
                Console.WriteLine("Creating some first time example playlists...");

                //var exampleMetalPlaylist =
                //    Settings._ParameterString + "default:LikesByGenre" + Environment.NewLine +
                //    Settings._CommentString + " genres to be used in this playlist should be specified here" + Environment.NewLine +
                //    Environment.NewLine +
                //    "symphonic metal" + Environment.NewLine +
                //    "melodic metal" + Environment.NewLine +
                //    "power metal" + Environment.NewLine +
                //    "progressive metal" + Environment.NewLine +
                //    "-artist:Korpiklaani #korpiklaani is more folk metal than symphonic metal!"
                //    ;

                var playlist = spotifyWrapper.GetFollowedPlaylists()
                    //.OrderByDescending(p => p.Followers.Total)
                    //.ThenBy(p => p.Tracks.Total)
                    .Where(p =>
                        p.Owner.Id != spotifyWrapper.CurrentUser.Id &&
                        p.GetTracks(spotifyWrapper).Where(t => spotifyWrapper.LikedTracks.Any(lt => lt.TrackId == t.Id)).Count() > 2
                        )
                    .FirstOrDefault();

                //TODO find one of the users playlists
                var playlistLikes = "LikesFromPlaylist:" + playlist.Id + " # " + playlist.Name;

                //var topLikedArtistNames = spotifyWrapper.LikedTracks
                //    .SelectMany(t => t.ArtistNames)
                //    .GroupBy(x => x)
                //    .OrderByDescending(g => g.Count())
                //    .Select(g => g.Key)
                //    .Take(10)
                //    .ToArray();

                //var exampleArtistLikesPlaylist =
                //    Settings._ParameterString + "default:LikesByArtist" + Environment.NewLine +
                //    Settings._ParameterString + "LimitPerArtist:5" + Environment.NewLine +
                //    Environment.NewLine +
                //    topLikedArtistNames.Join(Environment.NewLine);

                var topGenre = spotifyWrapper.LikedTracks
                    .SelectMany(t => t.ArtistGenres)
                    .GroupBy(x => x)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .FirstOrDefault();

                var genrePlaylist = "LikesByGenre:" + topGenre;
                var bossaNova = "LikesByGenre:bossa nova";
                var bossaNovaPlus = "@default:LikesByGenre" + Environment.NewLine + Environment.NewLine +
                        "bossa nova" + Environment.NewLine +
                        "samba";


                System.IO.Directory.CreateDirectory(directoryPath);

                //write the example files
                //System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Liked - Melodic Metal Stuff.txt"), exampleMetalPlaylist);
                System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Liked - " + playlist.Name + ".txt"), playlistLikes);
                //System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Liked - Top 10 Fav Artists.txt"), exampleArtistLikesPlaylist);
                System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Liked - " + topGenre + ".txt"), genrePlaylist);
                System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Liked - Bossa Nova.txt"), bossaNova);
                System.IO.File.WriteAllText(System.IO.Path.Join(directoryPath, "Liked - Bossa Nova Plus.txt"), bossaNovaPlus);
            }


            if (!listPlaylists && !string.IsNullOrWhiteSpace(playlistSpec))
            {
                var output = new List<PlaylistSpec>();
                output.Add(new PlaylistSpec(playlistName, playlistSpec));
                return output;
            }

            //TODO turn file system folders into Spotify playlist folders
            var files = System.IO.Directory.GetFiles(directoryPath, "*.txt", System.IO.SearchOption.AllDirectories);
            //read in playlist breakdowns
            var playlistSpecs = files.Select(path => new PlaylistSpec(path)).ToList();

            if (!string.IsNullOrWhiteSpace(playlistName))
            {
                //match with basic globbing syntax
                playlistSpecs = playlistSpecs
                    .Where(p =>
                        p.PlaylistName.Like(playlistName) ||
                        p.FinalPlaylistName.Like(playlistName) ||
                        p.PlaylistName + ".txt" == playlistName // handle accidental shell globbing getting passed in
                        )
                    .ToList();

                if (!playlistSpecs.Any() && !dontWarn)
                {
                    Console.WriteLine("No playlist spec found matching --playlist-name " + playlistName);
                    Environment.Exit(-1);
                }
            }

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

            var optionsErrors = playlistSpecs.SelectMany(p => p.OptionsErrors).Distinct().OrderBy(x => x).ToArray();

            //report on errors
            if (optionsErrors.Any() && !dontWarn)
                Console.WriteLine(optionsErrors.Join(Environment.NewLine));

            return playlistSpecs;
        }
        // TODO make excludeType an enum
        static void ExcludeCurrent(MySpotifyWrapper spotifyWrapper, IList<PlaylistSpec> playlistSpecs, string excludeType)
        {
            var currentTrack = spotifyWrapper.GetCurrentTrack(true);
            if (currentTrack == null) return;

            string consoleDeets = null;
            string specLine = null;

            if (excludeType.Like("artist"))
            {
                // 90% of the time there will be one artist
                consoleDeets = currentTrack.Artists.Select(a => a.Name).Join(", ");

                specLine = currentTrack.Artists.Select(a => "-Artist:" + a.Id + "\t" + Program.Settings._CommentString + "\t" + a.Name).Join(Environment.NewLine);
            }
            else if (excludeType.Like("album"))
            {
                consoleDeets = currentTrack.Artists.Select(a => a.Name).Join(", ") + " - " + currentTrack.Album.Name + " (" + currentTrack.Album.ReleaseDate.Substring(0, 4) + ")";
                specLine = "-Album:" + currentTrack.Album.Id + "\t" + Program.Settings._CommentString + "\t" + consoleDeets;
            }
            else if (excludeType.Like("track"))
            {
                consoleDeets = currentTrack.PrettyString();
                specLine = "-Track:" + currentTrack.Id + "\t" + Program.Settings._CommentString + "\t" + consoleDeets;
            }

            Console.WriteLine(
            "Excluding " +
            consoleDeets +
            " from " +
            playlistSpecs.Select(spec => spec.PlaylistName).Join(", ")
            );

            // 90% of the time there will be one playlist
            foreach(var playlistSpec in playlistSpecs)
            {
                System.IO.File.AppendAllText(playlistSpec.Path, Environment.NewLine + specLine);
            }

        }

        static void Lyrics(MySpotifyWrapper spotifyWrapper)
        {
            var commands = new string[] { Program.Settings._LyricCommand1, Program.Settings._LyricCommand2, Program.Settings._LyricCommand3 };
            var failText = new string[] { Program.Settings._LyricFailText1, Program.Settings._LyricFailText2, Program.Settings._LyricFailText3 };

            commands = commands.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            failText = failText.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            if (!commands.Any())
            {
                Console.WriteLine("No lyric commands specified in the config file.");
                return;
            }

            // if there's only one command and no fail text, just pass the output of the command on
            var passAllOutput = commands.Count() == 1 && !failText.Any();

            var currentTrack = spotifyWrapper.GetCurrentTrack();

            if (currentTrack == null)
            {
                Console.WriteLine("Unable to get currently playing track.");
                return;
            }

            var artistName = currentTrack.Artists.Select(a => a.Name).FirstOrDefault() ?? String.Empty;
            var trackName = currentTrack.Name ?? String.Empty;
            var foundLyrics = false;

            // escape double quotes in values
            artistName = artistName.Replace("\"", "\\\"");
            trackName = trackName.Replace("\"", "\\\"");

            foreach (var command in commands)
            {
                var args = string.Empty;
                var commandMod = command;

                if (commandMod.Contains("{artistName}") && commandMod.Contains("{trackName}"))
                {
                    commandMod = commandMod.Replace("{artistName}", "\"" + artistName + "\"");
                    commandMod = commandMod.Replace("{trackName}", "\"" + trackName + "\"");
                }
                else
                    args = "\"" + artistName + "\" \"" + trackName + "\"";

                var commandArray = commandMod.Split(" ", 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (commandArray.Count() > 1)
                {
                    commandMod = commandArray.First();
                    args = commandArray.Last() + " " + args;
                }

                // if (Settings._VerboseDebug)
                // {
                //    Console.WriteLine("command: " + commandMod);
                //    Console.WriteLine("args: " + args);
                // }

                var lyricCommand = new ProcessStartInfo() {
                    FileName = commandMod,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = !passAllOutput,
                    RedirectStandardError = !passAllOutput,
                    CreateNoWindow = true,
                };
                using (var process = Process.Start(lyricCommand))
                {
                    if (passAllOutput)
                    {
                        process?.WaitForExit();
                        continue;
                    }

                    var commandOutput = new StringBuilder(string.Empty);

                    while (!process.StandardOutput.EndOfStream)
                        commandOutput.AppendLine(process.StandardOutput.ReadLine());
                    while (!process.StandardError.EndOfStream)
                        commandOutput.AppendLine(process.StandardError.ReadLine());

                    process?.WaitForExit();

                    if (commandOutput.Length > 0 && !failText.Any(x => commandOutput.ToString().Like("*" + x + "*")))
                    {
                        Console.WriteLine();
                        Console.WriteLine(commandOutput.ToString().Trim());
                        foundLyrics = true;
                        break;
                    }
                }
            }

            if (!foundLyrics && !passAllOutput)
                Console.WriteLine("Could not find lyrics.");
            }

        static void ModifyPlaylistSpecFiles(MySpotifyWrapper spotifyWrapper, IList<PlaylistSpec> playlistSpecs, bool modifyArg)
        {
            // TODO mod this so it doesn't ruin the sorting on your vgc exclusions
            // OR add that "shared file" option
            Console.WriteLine();
            Console.WriteLine("---making updates to playlist spec files---");
            Console.WriteLine("started at " + DateTime.Now.ToString());

            //swap artist names for artist IDs
            //this will save multiple API hits involved in searching for artists by name and paging over the results
            //TODO nest your progress printers
            //var pp1 = new ProgressPrinter(playlistSpecs.Length, (perc, time) => ConsoleWriteAndClearLine("\rAdding artist IDs to playlist files: " + perc + ", " + time + " remaining"));
            var specs =  playlistSpecs.Where(p => 
                    (modifyArg || p.AddArtistIDs)
                    && !p.DontModify).ToArray();
            foreach (var playlistSpec in specs)
            {
                var findFailureWarning = "Could not find item. Remove this comment to try again";
                var linesUpdated = 0;
                var idCharLength = 7 + 22;

                //go one line at a time for simplicity and intra-run progress writing
                foreach (var line in playlistSpec.SpecLines)
                {
                    if (!line.IsValidParameter || line.SubjectType == ObjectType.Genre || line.ParameterValue.Contains("*"))
                        continue;

                    var newLines = new List<string>();

                    //----------- swap names for ids -----------
                    if (!idRegex.IsMatch(line.ParameterValue))
                    {
                        //artist
                        if (line.SubjectType == ObjectType.Artist)
                        {
                            var matchingArtists = spotifyWrapper.GetArtistsByName(new List<string> { line.ParameterValue })
                                //do some shenannigans since I'm not sure .Distinct will work
                                .GroupBy(a => a.Id)
                                .Select(g => g.First())
                                .Distinct()
                                .ToList();

                            foreach (var artist in matchingArtists)
                            {
                                var newLine = string.Empty;

                                //append artist ID and name as a comment
                                newLine += artist.Id + "\t" + Program.Settings._CommentString + "\t" + artist.Name;

                                //additional deets for help differentiating multiple matching artists
                                if (matchingArtists.Count() > 1)
                                {
                                    //genre
                                    if (artist.Genres.Any())
                                        newLine += ", " + artist.Genres.FirstOrDefault() ?? String.Empty;

                                    //likes
                                    var likedCount = spotifyWrapper.LikedTracks.Count(t => t.ArtistIds.Any(id => id == artist.Id));
                                    if (likedCount > 0)
                                        newLine += ", " + likedCount.ToString("#,##0") + " liked tracks";
                                }

                                newLines.Add(newLine);
                            }

                            if (!matchingArtists.Any())
                            {
                                var newLine = string.Empty;

                                //put the value back with a warning in the comment
                                newLine += line.ParameterValue+ " " + Program.Settings._CommentString + " " + findFailureWarning;

                                newLines.Add(newLine);
                            }
                        }
                        //playlist
                        else if (line.SubjectType == ObjectType.Playlist)
                        {
                            //there are valid reasons to use playlist name instead of ID, don't swap those
                        }
                        //album
                        else if (line.SubjectType == ObjectType.Album)
                        {
                            //duplicate artist/album combos should be exceedingly rare
                            //therefore leaving off differentiating data points

                            var newAlbumLines = spotifyWrapper.GetTracksByAlbum(new List<string> { line.ParameterValue })
                                .GroupBy(t => t.AlbumId)
                                .Select(g =>
                                    g.Key + "\t" + Program.Settings._CommentString + "\t" +
                                    (g.SelectMany(t => t.ArtistNames
                                        //only artist names that appear on every track on the album
                                        .Where(a => g.All(tx => tx.ArtistNames.Contains(a)))
                                        ).Distinct().Join(", ")
                                        .NullIfEmpty() ?? "various") + " - " +
                                    g.First().AlbumName
                                )
                                .ToList();

                            if (!newAlbumLines.Any())
                                newAlbumLines.Add(line.ParameterValue + " " + Program.Settings._CommentString + " " + findFailureWarning);

                            newLines.AddRange(newAlbumLines);
                        }
                        //track
                        else if (line.SubjectType == ObjectType.Track)
                        {
                            // TODO support swapping track names for IDs in ModifyPlaylistSpecFiles
                        }
                    }
                    //----------- add name comments where missing -----------
                    else if (string.IsNullOrWhiteSpace(line.Comment)) // and implicitly line is an ID
                    {
                        string name = null;
                        if (line.SubjectType == ObjectType.Artist)
                        {
                            name = spotifyWrapper.GetArtists(new List<string> { line.ParameterValue }).FirstOrDefault()?.Name;
                        }
                        else if (line.SubjectType == ObjectType.Playlist)
                        {
                            name = spotifyWrapper.spotify.Playlists.Get(line.ParameterValue).Result.Name;
                        }
                        else if (line.SubjectType == ObjectType.Album)
                        {
                            name = spotifyWrapper.spotify.Albums.Get(line.ParameterValue).Result.Name;
                        }
                        else if (line.SubjectType == ObjectType.Track)
                        {
                            name = spotifyWrapper.spotify.Tracks.Get(line.ParameterValue).Result.Name;
                        }

                        if (line.SubjectType != ObjectType.None)
                        {
                            newLines.Add(line.ParameterValue + "\t" + Program.Settings._CommentString + "\t" +
                                name ?? findFailureWarning
                                );
                        }
                    }

                    // no need to mark exclusions; that's part of the parameter name

                    //prepend parameter name if not default
                    if (!playlistSpec.Default.Like(line.ParameterName))
                        newLines = newLines.Select(newLine => line.ParameterName + ":" + newLine).ToList();

                    //slap any existing comment on the end, adding the comment character if necessary
                    if (!string.IsNullOrWhiteSpace(line.Comment))
                        newLines = newLines.Select(newLine =>
                            newLine.Trim() + 
                            (newLine.Contains(Program.Settings._CommentString) ? String.Empty : "  ") +
                            Program.Settings._CommentString + " " + line.Comment.Trim()
                        ).ToList();

                    newLines = newLines.Distinct().ToList();

                    // split and rejoin comments here to get the spacing right
                    newLines = newLines.Select(newLine => new
                    {
                        SplitLine = newLine.Split(Program.Settings._CommentString, 2),
                        PadLen = playlistSpec.Default.Like(line.ParameterName) ? 7 : idCharLength
                    })
                            .Select(x => new
                            {
                                Main = x.SplitLine.First().Trim().PadRight(x.PadLen),
                                Comment = (x.SplitLine.Length > 1 ? "\t" + Program.Settings._CommentString + "\t" + x.SplitLine.Last().Trim() : string.Empty)
                            })
                            .Select(x => (x.Main + x.Comment).Trim())
                            .ToList();

                    if (newLines.Any())
                    {
                        //update the line
                        line.RawLine = newLines.Join(Environment.NewLine);
                        linesUpdated += 1;
                    }


                    //write every 20 lines
                    if (linesUpdated > 0 && linesUpdated % 20 == 0)
                        playlistSpec.Write();

                }

                if (linesUpdated > 0)
                    playlistSpec.Write();

                //pp1.PrintProgress();
            }
            Console.WriteLine();
        }

        static void CommitAnActOfUnspeakableViolence(MySpotifyWrapper spotifyWrapper)
        {
            var rnd = new Random();

            switch (rnd.Next(1, 7))
            {
                case 1:
                    Console.WriteLine("Gee, I'd really rather not.");
                    break;
                case 2:
                    Console.WriteLine("How dare you ask such a thing of me.");
                    break;
                case 3:
                    Console.WriteLine("I'm just an innocent little pwaywist genowatow, uwu.");
                    break;
                case 4:
                    Console.WriteLine("Launching missiles in...");
                    var i = 5;
                    while (i >= 0)
                    {
                        Console.WriteLine(i.ToString("#"));
                        System.Threading.Thread.Sleep(1000);
                        i -= 1;
                    }

                    Console.WriteLine();
                    System.Threading.Thread.Sleep(2000);

                    if (new Random().Next(1, 2) == 1)
                        Console.WriteLine("Huh, must be broken.");
                    else
                        Console.WriteLine("All the nation's capitols now lie in ruin.");

                    break;
                case 5:

                    Console.WriteLine("Preparing to delete playlists...");
                    var userPlaylists = spotifyWrapper.GetUsersPlaylists();

                    var maxNameLen = userPlaylists.Max(p => p.Name.Length);

                    foreach (var playlist in userPlaylists)
                    {
                        Console.Write(playlist.Name);

                        var x = 3;
                        while (x > 0)
                        {
                            Console.Write(".");
                            System.Threading.Thread.Sleep(500);
                            x -= 1;
                        }

                        Console.Write(new String(' ', maxNameLen - playlist.Name.Length) + " deleted");
                        Console.WriteLine();
                    }

                    Console.WriteLine();
                    System.Threading.Thread.Sleep(2000);
                    Console.WriteLine("Just kidding.");

                    break;
                case 6:
                    Console.WriteLine("I'm busy right now.");
                    break;
                default:
                    Console.WriteLine("I had a late night last night. Maybe next time.");
                    break;
            }
        }

        static void GetPlaylistBreakdowns(MySpotifyWrapper spotifyWrapper, IList<PlaylistSpec> playlistSpecs)
        {
            Console.WriteLine();
            Console.WriteLine("---assembling playlist tracks---");
            Console.WriteLine("started at " + DateTime.Now.ToString());

            playlistSpecs = playlistSpecs.Where(playlistSpec => !playlistSpec.DontRun).ToList();

            if (playlistSpecs.Count > 1)
                Console.WriteLine("Found " + playlistSpecs.Count.ToString("#,##0") + " playlists");
            else if (playlistSpecs.Count == 1)
                Console.WriteLine("Found " + playlistSpecs.First().PlaylistName);

            var pp = new ProgressPrinter(playlistSpecs.Count, (perc, time) => ConsoleWriteAndClearLine("\rAssembling playlists: " + perc + ", " + time + " remaining"));
            foreach (var playlistSpec in playlistSpecs)
            {
                //further mangle the console output for the sake of debug info
                if (Program.Settings._VerboseDebug)
                {
                    //Console.WriteLine();
                    //Console.WriteLine("Playlist: " + playlistSpec.PlaylistName);
                    //Console.WriteLine("Playlist lines: " + playlistSpec.SpecLines.Count().ToString("#,##0"));
                    //Console.WriteLine("Playlist param lines: " + playlistSpec.SpecLines
                    //    .Where(line => line.IsValidParameter)
                    //    .Count().ToString("#,##0"));

                    // foreach(var line in playlistSpec.SpecLines)
                    // {
                    //     // Console.WriteLine(line.ToString());
                    //     Console.WriteLine("parameter name: " +line.ParameterName);
                    //     Console.WriteLine("parameter value: " +line.ParameterValue);
                    // }
                }

                // ------------ get tracks ------------

                //find the definition for each parameter name in this playlist and run it
                var playlistTracks = playlistSpec.GetGroupedParameters()
                    .SelectMany(kvp =>
                        PlaylistParameterDefinition.AllDefinitions
                        .Where(d =>
                            d.ParameterName.Like(kvp.Key) &&
                            !kvp.Key.Trim().StartsWith(Program.dashes) &&
                            !d.IsExclusion
                            )
                        .Select(d => new
                        {
                            Definition = d,
                            ParameterValues = kvp.Value
                        })
                    )
                    .SelectMany(x => x.Definition.GetTracks(
                        spotifyWrapper,
                        parameterValues: x.ParameterValues,
                        likedTracks: spotifyWrapper.LikedTracks,
                        // TODO having this as a string isn't great
                        exceptArtists: (x.Definition.ParameterName.Like("*ArtistFromPlaylist") ? playlistSpec.ExceptArtistFromPlaylist_Parsed : null)
                        ))
                    .Distinct()
                    .ToList();

                // ------------ exclude tracks ------------

                var excludeTracks = playlistSpec.GetGroupedParameters()
                    .SelectMany(kvp =>
                        PlaylistParameterDefinition.AllDefinitions
                        .Where(d =>
                            d.ParameterName.Like(kvp.Key) &&
                            kvp.Key.Trim().StartsWith(Program.dashes) &&
                            d.IsExclusion
                            )
                        .Select(d => new
                        {
                            Definition = d,
                            ParameterValues = kvp.Value
                        })
                    )
                    .SelectMany(x => x.Definition.GetTracks(spotifyWrapper, parameterValues: x.ParameterValues, existingTracks: playlistTracks))
                    .ToList();

                playlistTracks.RemoveRange(excludeTracks);




                // ------------ remove dupes ------------

                //complicated logic for determining duplicates
                var dupes = playlistTracks
                    .GroupBy(track => track.ComparisonString) //same track name, same artist, very standardized
                    .Where(group =>
                        group.Count() > 1 && // only dupes
                        group.Select(track => track.AlbumId).Distinct().Count() > 1 // not from the same album
                                                                                     //do a time comparison as well to test for fundamental differences, but think about how this effects live albums
                        )
                    .Select(group => group
                        .OrderByDescending(track => track.AlbumType == "album") //albums first
                        .ThenBy(track => track.ReleaseDate) // older albums first; this should help de-prioritize deluxe releases and live albums
                        //TODO put some serious thought into how to best handle live albums
                        .ThenBy(track => track.AlbumId) // all other things the same, de-dupe to the same album
                        .ThenBy(track => track.TrackId) // one last sort to make this deterministic
                        .ToList()
                        )
                    .ToList();

                // the "track != group.First()" method will not handle multiples of the exact same ID'd track
                // hence the .Distinct() below
                var removeTracks = dupes
                    .SelectMany(group => group.Where(track => track != group.First())) // remove all but the first track per group
                    .ToList();
                playlistTracks.RemoveRange(removeTracks);
                playlistTracks = playlistTracks.Distinct().ToList();

                // ------------ no likes - placement is important ------------

                if (playlistSpec.NoLikes)
                {
                    playlistTracks.Remove(t =>
                        spotifyWrapper.LikedTracks.Any(lt => 1 == 2
                            || lt.ComparisonString == t.ComparisonString
                            || lt.TrackId == t.TrackId
                        ));
                }

                // ------------ no explicit------------
                
                if (playlistSpec.NoExplicit)
                    playlistTracks.Remove(t => t.Explicit);

                // ------------ limit per * ------------

                if (playlistSpec.LimitPerArtist > 0)
                {
                    //allows multiples of an artist if the guests are still unique
                    //playlistTracks = playlistTracks.SelectMany(t => t.ArtistIds.Select(id => new
                    //{
                    //    track = t,
                    //    artistID = id
                    //}))
                    //    .GroupBy(x => x.artistID, x => x.track)
                    //    .SelectMany(g => g
                    //        .Distinct()
                    //        .OrderByDescending(t => t.Popularity)
                    //        .ThenBy(t => t.TrackId)
                    //        .Take(playlistSpec.LimitPerArtist)
                    //    )
                    //    .Distinct()
                    //    .ToList();

                    //allow no more than one artist even if it means some guests get entirely cut
                    var tracksToRemove = playlistTracks
                        .SelectMany(t => t.ArtistIds)
                        .Distinct()
                        .SelectMany(id =>
                            playlistTracks
                            .Where(t => t.ArtistIds.Contains(id))
                            .OrderByDescending(t => t.Popularity)
                            .ThenBy(t => t.TrackId) // one last sort to make this deterministic
                            .Skip(playlistSpec.LimitPerArtist)
                            )
                        .Distinct()
                        .ToArray();

                    playlistTracks.RemoveRange(tracksToRemove);
                }

                if (playlistSpec.LimitPerAlbum > 0)
                {
                    playlistTracks = playlistTracks
                        .GroupBy(x => x.AlbumId, t => t)
                        .SelectMany(g => g
                            .Distinct()
                            .OrderByDescending(t => t.Popularity)
                            .ThenBy(t => t.TrackId) // one last sort to make this deterministic
                            .Take(playlistSpec.LimitPerAlbum)
                        )
                        // handle duplicate albums that are really the same
                        .GroupBy(x => (x.ArtistNames.FirstOrDefault() ?? string.Empty)+ "$$$" + x.AlbumName, t => t)
                        .SelectMany(g => g
                            .Distinct()
                            .OrderByDescending(t => t.Popularity)
                            .ThenBy(t => t.TrackId) // one last sort to make this deterministic
                            .Take(playlistSpec.LimitPerAlbum)
                        )
                        .Distinct()
                        .ToList();
                }

                // ------------ date limits ------------

                if (playlistSpec.ReleasedAfterCalc.HasValue)
                {
                    playlistTracks = playlistTracks
                        .Where(t => t.ReleaseDate > playlistSpec.ReleasedAfterCalc.Value)
                        .ToList();
                }


                if (playlistSpec.ReleasedBefore.HasValue)
                {

                    playlistTracks = playlistTracks
                        .Where(t => t.ReleaseDate < playlistSpec.ReleasedBefore.Value)
                        .ToList();
                }

                if (playlistSpec.LikedAfter.HasValue)
                {
                    playlistTracks = playlistTracks
                        .Where(t => t.LikedAt > playlistSpec.LikedAfter.Value.ToUniversalTime())
                        .ToList();

                    Console.WriteLine("playlistSpec.LikedAfter: " + playlistSpec.LikedAfter.Value.ToUniversalTime().ToString());
                    Console.WriteLine("playlistTracks.Min(t => t.LikedAt): " + playlistTracks.Min(t => t.LikedAt).ToString());
                }

                if (playlistSpec.LikedBefore.HasValue)
                {
                    Console.WriteLine("max like utc: " + playlistTracks.Max(t => t.LikedAt).ToString());
                    playlistTracks = playlistTracks
                        .Where(t => t.LikedAt < playlistSpec.LikedBefore.Value.ToUniversalTime())
                        .ToList();

                    Console.WriteLine("playlistSpec.LikedBefore: " + playlistSpec.LikedBefore.Value.ToUniversalTime().ToString());
                    Console.WriteLine("playlistTracks.Max(t => t.LikedAt): " + playlistTracks.Max(t => t.LikedAt).ToString());
                }

                // ------------ sort ------------

                //these sorts won't preserve over time without @UpdateSort
                //for example, what if an old album is added to spotify for a full discog playlist?
                if (playlistSpec.Sort == Sort.Liked)
                {
                    playlistTracks = playlistTracks
                        .OrderBy(t => t.LikedAt)
                        .ToList();
                }
                else if (playlistSpec.Sort == Sort.Release)
                {
                    playlistTracks = playlistTracks
                        .OrderBy(t => t.ReleaseDate)
                        .ThenBy(t => t.ArtistNames.FirstOrDefault())
                        .ThenBy(t => t.AlbumName)
                        .ThenBy(t => t.TrackNumber)
                        .ToList();
                }
                else if (playlistSpec.Sort == Sort.Artist)
                {
                    playlistTracks = playlistTracks
                        .OrderBy(t => t.ArtistNames.FirstOrDefault())
                        .ThenByDescending(t => t.Popularity)
                        .ToList();
                }
                //else if (playlistSpec.Sort == Sort.File)
                //{
                //    //damn... is it even possible to do a file sort without knowing what tracks go with what lines?
                //}
                else if (playlistSpec.Sort == Sort.Dont)
                {
                    // just leave it alone
                    // if you only have one type of parameter I guess it *should* be a file sort
                    // more or less
                }

                //keep playlists from overflowing
                if (playlistTracks.Count > Program.MaxPlaylistSize)
                {
                    playlistTracks = playlistTracks.Take(Program.MaxPlaylistSize).ToList();
                }

                playlistSpec.Tracks = playlistTracks;

                pp.PrintProgress();

            }

            Console.WriteLine();

            var deets = "Assembled track list for " + playlistSpecs.Count.ToString("#,##0") + " playlists";
            deets += " with an average of ";

            if (playlistSpecs.Any())
                deets += playlistSpecs.Average(spec => spec.Tracks.Count).ToString("#,##0.00");
            else
                deets += "0";

            deets += " tracks per playlist.";

            Console.WriteLine();
            Console.WriteLine(deets);
            Console.WriteLine();
        }

        static void UpdatePlaylists(MySpotifyWrapper spotifyWrapper, List<PlaylistSpec> playlistSpecs, out List<FullPlaylist> newPlaylists)
        {

            if (!playlistSpecs.Where(p => !p.DontRun).Any())
            {
                newPlaylists = new List<FullPlaylist>();
                Console.Write("No playlists to update.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("---updating spotify---");

            var allPlaylists = spotifyWrapper.GetUsersPlaylists();

            //bundle all playlists to be deleted for any reason
            var removePlaylists = allPlaylists.Select(p => new
            {
                Recreate = (Settings._RecreatePlaylists && playlistSpecs.Any(spec => spec.FinalPlaylistName == p.Name)),
                Orphaned = (Settings._DeleteOrphanedPlaylists && p.Description.Contains(Program.AssemblyName) && !playlistSpecs.Any(spec => spec.FinalPlaylistName == p.Name)),
                DeleteIfEmpty = (playlistSpecs.Any(spec => spec.FinalPlaylistName == p.Name && spec.DeleteIfEmpty && !spec.Tracks.Any())),
                Playlist = p
            })
                .Select(x => new
                {
                    Reason = (
                        x.Recreate ? "playlists for re-creation" :
                        x.Orphaned ? "orphaned playlists" :
                        x.DeleteIfEmpty ? "empty playlists" :
                        String.Empty) ,
                    x.Playlist
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Reason))
                .ToArray();

            //dump any playlists as appropriate
            foreach (var removePlaylist in removePlaylists)
            {
                spotifyWrapper.spotify.Follow.UnfollowPlaylist(removePlaylist.Playlist.Id);
                allPlaylists.Remove(removePlaylist.Playlist);
            }

            //create a neat little report on why we removed any playlists that we did
            var removeReport = removePlaylists
                .GroupBy(x => x.Reason)
                .Select(g => "Removed " + g.Count().ToString("#,##0") + " " + g.Key + ".")
                .Join(Environment.NewLine);

            var createPlaylistCounter = 0;
            var removedTracksCounter = 0;
            var removedDupesCounter = 0;
            var addedTracksCounter = 0;
            var sortedPlaylistIDs = new List<string>();
            newPlaylists = new();

            playlistSpecs = playlistSpecs
                .Where(playlistSpec => 
                            !playlistSpec.DontRun &&
                            !(playlistSpec.DeleteIfEmpty && !playlistSpec.Tracks.Any()
                             ))
                .OrderByDescending(spec => spec.FinalPlaylistName).ToList()
                ;

            var sbReport = new StringBuilder();
            var maxPlaylistNameLen = !playlistSpecs.Any() ? 0 : playlistSpecs.Max(spec => spec.FinalPlaylistName.Length);

            //iterating through rather than running in bulk with linq to *hopefully* be a little more memory efficient
            //order by descending playlist name to get alphabetical playlists in the Spotify interface
            var pp = new ProgressPrinter(playlistSpecs.Count, (perc, time) => ConsoleWriteAndClearLine("\rUpdating playlists: " + perc + ", " + time + " remaining"));
            foreach (var playlistSpec in playlistSpecs)
            {
                //find this playlist by name
                //99% of the time there will only be one, but it's *possible* for two playlists to share a name
                //that was confusing as hell to discover
                var playlist = allPlaylists
                    .Where(p => p.Name == playlistSpec.FinalPlaylistName)
                    .OrderByDescending(p => p.Description.Contains(Program.AssemblyName))
                    //TODO order by track id matches?
                    .FirstOrDefault();

                //if the spotify api supported folders, folder creation/finding would go here
                //unfort, it does not and I'm sad T.T

                //create playlist if missing
                if (playlist is null)
                {
                    var playlistRequest = new PlaylistCreateRequest(playlistSpec.FinalPlaylistName);
                    playlistRequest.Description = "Automatically generated by " + Program.AssemblyName + ".";
                    playlistRequest.Public = !Settings._NewPlaylistsPrivate;
                    var newPlaylist = spotifyWrapper.spotify.Playlists.Create(spotifyWrapper.CurrentUser.Id, playlistRequest).Result;

                    playlist = newPlaylist;

                    createPlaylistCounter += 1;
                    newPlaylists.Add(playlist);
                }

                // write the ID if it isn't already logged
                if ((playlistSpec.ID ?? string.Empty) != (playlist.Id ?? string.Empty))
                {
                    var fileLines = System.IO.File.ReadAllLines(playlistSpec.Path).ToList();
                    fileLines.Insert(0, "@ID:" + playlist.Id);
                    System.IO.File.WriteAllLines(playlistSpec.Path, fileLines);

                }

                if (playlistSpec.OnlyRunIfModified)
                {
                    var fileLines = System.IO.File.ReadAllLines(playlistSpec.Path).ToList();
                    var modTime = System.IO.File.GetLastWriteTime(playlistSpec.Path);
                    var lastRunString = "@LastRun:" + Program.RunStart.ToString();
                    var insertPos = fileLines.First().StartsWith("@ID:") ? 1 : 0;

                    if (playlistSpec.LastRun.HasValue)
                        fileLines = fileLines.Select(line =>
                                line.StartsWith("@LastRun:")
                                ? lastRunString
                                : line)
                            .ToList();
                    else
                        fileLines.Insert(insertPos, lastRunString);

                    System.IO.File.WriteAllLines(playlistSpec.Path, fileLines);
                    System.IO.File.SetLastWriteTime(playlistSpec.Path, modTime);
                }

                //get the items in the playlist currently
                // pull out and cast just the track from the weird playlist track object
                var playlistTracksCurrent = playlist.GetTracks(spotifyWrapper);

                //main work part 1 - remove existing playlist tracks that no longer belong
                //"don't" check goes here, else the reporting is wrong
                var removeTrackRequestItems = playlistTracksCurrent
                    .Where(gpt => !playlistSpec.DontRemoveTracks && gpt != null && !playlistSpec.Tracks.Any(glt => glt.TrackId == gpt.Id))
                    .Select(gpt => new PlaylistRemoveItemsRequest.Item() { Uri = gpt.Uri })
                    .Distinct()
                    .ToList();

                //get the technical dupes (not logical dupes) in the playlist in spotify right now
                var dupesCurrent = playlistTracksCurrent
                    .Where(t => t != null)
                    .GroupBy(t => t.Id)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.First())
                    .ToArray();

                var dupePositionsToRemove = dupesCurrent
                    .SelectMany(t => Enumerable.Range(0, playlistTracksCurrent.Count() - 1)
                                 .Where(i => (playlistTracksCurrent[i]?.Id ?? string.Empty) == t.Id)
                                 .Skip(1)
                                 )
                    .ToList();

                if (dupePositionsToRemove.Any())
                {
                    //the API only accepts 100 tracks at a time
                    //however, removing more than one chunk will alter the position of subsequent items
                    //therefore only do 100 at a time
                    //snapshot ID considerations assure that positions remain relevant even after removing some tracks
                    // TODO review dupe removal to make sure it's working correctly with more than one chunk
                    var removeRequests = dupePositionsToRemove
                        .ChunkBy(100)
                        .Select(items => new PlaylistRemoveItemsRequest { Positions = items, SnapshotId = playlist.SnapshotId })
                        .ToList();

                    foreach (var removeRequest in removeRequests)
                        spotifyWrapper.spotify.Playlists.RemoveItems(playlist.Id, removeRequest);

                    removedDupesCounter += dupePositionsToRemove.Count;
                }

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
                var addTrackURIs = playlistSpec.Tracks
                    .Where(glt => !playlistTracksCurrent.Any(gpt => gpt != null && gpt.Id == glt.TrackId))
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
                    {
                        spotifyWrapper.spotify.Playlists.AddItems(playlist.Id, addRequest).Wait();
                    }

                    addedTracksCounter += addTrackURIs.Count;
                }


                if ((playlistSpec.UpdateSort || Program.Settings._UpdateSort) && !newPlaylists.Contains(playlist))
                {
                    List<PlaylistReorderItemsRequest> reorderRequests = null;

                    if (addTrackURIs.Any())
                    {
                        playlist = spotifyWrapper.spotify.Playlists.Get(playlist.Id).Result;
                        playlistTracksCurrent = playlist.GetTracks(spotifyWrapper).Where(t => t != null).ToList();
                    }

                    var snapshotID = playlist.SnapshotId;
                    var reorderCount = 0;
                    do
                    {
                        reorderCount += 1;

                        if (playlistTracksCurrent.Count() != playlistSpec.Tracks.Count())
                        {
                            // on 2023-04-02 this occurred repeatedly when Spotify simply failed to add or remove tracks
                            Console.WriteLine("Skipping sort. Cloud and local playlist tracks no longer match!");
                            Console.WriteLine("playlistTracksCurrent: " + playlistTracksCurrent.Count().ToString("#,##0") + " playlistSpec: " + playlistSpec.Tracks.Count().ToString("#,##0"));
                            break;
                        }

                        //ridiculously complicated as the API only takes "move this index to this index" commands
                        //still, it's better than wiping the playlist every time it needs to be sorted
                        //TODO update playlistTracksCurrent in parallel to the cloud so we don't have to pull them fresh every time
                        reorderRequests = playlistTracksCurrent
                            .Select(t => new
                            {
                                CloudIndex = playlistTracksCurrent.IndexOf(t),
                                CloudID = t.Id,
                                LocalIndex = playlistSpec.Tracks.Select(tn => tn.TrackId).ToList().IndexOf(t.Id),
                            })
                            .Where(x => x.CloudIndex != x.LocalIndex)
                            .Select(x => new
                            {
                                x.CloudIndex,
                                x.CloudID,
                                x.LocalIndex,
                                LocalNextIndex = x.LocalIndex + 1
                            })
                            .Select(x => new PlaylistReorderItemsRequest(
                                rangeStart: x.CloudIndex,
                                insertBefore: x.LocalNextIndex - 1
                                )
                            {
                                SnapshotId = snapshotID
                            })
                            .Mash()
                            .OrderBy(x => x.InsertBefore)
                            .ToList();

                        if (reorderRequests.Any())
                        {
                            var req = reorderRequests.First();
                            snapshotID = spotifyWrapper.spotify.Playlists.ReorderItems(playlist.Id, req).Result.SnapshotId;
                            playlistTracksCurrent = playlistTracksCurrent.Reorder(req);

                            if (!sortedPlaylistIDs.Contains(playlist.Id))
                                sortedPlaylistIDs.Add(playlist.Id);
                        }

                        //no idea what amount is reasonable here
                        if (reorderCount > playlistTracksCurrent.Count())
                        {
                            Console.WriteLine("Too many reorder requests. (" + reorderCount.ToString("#,##0") + ")");
                            break;
                        }

                    }
                    while (reorderRequests.Any());

                    if (Program.Settings._VerboseDebug)
                        Console.WriteLine("Sent " + reorderCount.ToString("#,##0") + " playlist reorder requests");

                }

                //write a nice little output report
                //TODO find a nice way of not cluttering up the folder
                sbReport.AppendLine(playlist.Name + ":" + new string(' ', maxPlaylistNameLen - playlist.Name.Length) +
                    playlistTracksCurrent.Count().ToString("#,##0").PadLeft(6).PadRight(1) +
                    "-->" +
                    (playlistTracksCurrent.Count() - removeTrackRequestItems.Count() + addTrackURIs.Count()).ToString("#,##0").PadLeft(6)
                    );

                pp.PrintProgress();
            }

            if (sbReport.Length > 0)
            {
                var path = System.IO.Path.Join(Settings._ReportsFolderPath, "Output", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt");
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                System.IO.File.WriteAllText(path, sbReport.ToString());
            }

            Console.WriteLine();
            if (!string.IsNullOrWhiteSpace(removeReport))
                Console.WriteLine(removeReport);
            Console.WriteLine("Created " + createPlaylistCounter.ToString("#,##0").PadLeft(5) + " new playlists.");
            Console.WriteLine("Sorted  " + sortedPlaylistIDs.Count().ToString("#,##0").PadLeft(5) + " existing playlists.");
            Console.WriteLine("Removed " + removedTracksCounter.ToString("#,##0").PadLeft(5) + " existing tracks.");
            Console.WriteLine("Removed " + removedDupesCounter.ToString("#,##0").PadLeft(5) + " existing duplicates.");
            Console.WriteLine("Added   " + addedTracksCounter.ToString("#,##0").PadLeft(5) + " new tracks.");
            Console.WriteLine();


        }

        private static void UpdateReadme(IList<PlaylistSpec> playlistSpecs = null)
        {
            if (Program.Settings._VerboseDebug)
                Console.WriteLine("Program.ProjectPath: " + Program.ProjectPath);

            var readmePath = System.IO.Path.Join(Program.ProjectPath, "README.MD");
            var readmeTemplatePath = System.IO.Path.Join(Program.ProjectPath, "MarkdownTemplates/README.MD");
            var csprojPath = System.IO.Path.Join(Program.ProjectPath, Program.AssemblyName + ".csproj");
            var smallDocsDirPath = System.IO.Path.Join(Program.ProjectPath, "Small Docs");

            if (Program.Settings._VerboseDebug)
            {
                Console.WriteLine("readmePath: " + readmePath);
                Console.WriteLine("readmeTemplatePath: " + readmeTemplatePath);
            }

            if (new string[] { readmePath, readmeTemplatePath }
                .Any(path => string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                )
            {
                Console.WriteLine("Markdown files not found. --update-readme only works in development.");
                return;
            }

            var titleText = Program.TitleText_Large().Indent();
            var appName = Program.AssemblyName.Replace("_", " ");
            var argsHelp = Help.ArgumentHelp.Substring(Help.ArgumentHelp.IndexOf("Options:") + "Options:".Length).Indent();
            var optionsHelp = Help.OptionHelp.Indent();
            var paramsHelp = Help.ParameterHelp.Indent();
            var configSettings = Help.ConfigSettingsHelp.Indent();

            var csprojText = System.IO.File.ReadAllText(csprojPath);
            var packageRefRegex = new Regex("PackageReference Include=\"(.+?)\"");
            var packageVersionRegex = new Regex("PackageReference.+?Version=\"(.+?)\"");
            var packageNames = packageRefRegex.Matches(csprojText).Select(m => m.Groups.Values.Last().Value).ToArray();
            var packageVersions = packageVersionRegex.Matches(csprojText).Select(m => m.Groups.Values.Last().Value).ToArray();

            //provide links for packages
            //packages are added and removed with the project, but links have to be manually added here
            var packageLinks = new Dictionary<string, string>();
            packageLinks.Add("APOD.Net", "https://github.com/MarcusOtter/APOD.Net");
            packageLinks.Add("colorthief.imagesharp", "https://github.com/Corona-Studio/ColorThief.ImageSharp");
            packageLinks.Add("colorthief.imagesharp.shared", "https://github.com/Corona-Studio/ColorThief.ImageSharp");
            packageLinks.Add("CsvHelper", "https://github.com/JoshClose/CsvHelper");
            packageLinks.Add("figgle", "https://github.com/drewnoakes/figgle");
            packageLinks.Add("ini-parser", "https://github.com/rickyah/ini-parser");
            packageLinks.Add("MediaTypeMap", "https://github.com/samuelneff/MimeTypeMap");
            packageLinks.Add("SixLabors.ImageSharp", "https://github.com/SixLabors/ImageSharp");
            packageLinks.Add("SixLabors.ImageSharp.Drawing", "https://github.com/SixLabors/ImageSharp.Drawing");
            packageLinks.Add("SpotifyAPI.Web", "https://github.com/JohnnyCrazy/SpotifyAPI-NET");
            packageLinks.Add("System.CommandLine.DragonFruit", "https://github.com/dotnet/command-line-api/blob/main/docs/Your-first-app-with-System-CommandLine-DragonFruit.md");
            packageLinks.Add("System.Drawing.Common", "https://www.nuget.org/packages/System.Drawing.Common/");
            packageLinks.Add("Unsplash.Net", "https://github.com/unsplash-net/unsplash-net");

            //add links in the names if available
            packageNames = packageNames.Select(x => (packageLinks.ContainsKey(x)
                ? "[" + x + "](" + packageLinks[x] + ")"
                : x))
                .ToArray();

            var packageText = new StringBuilder();
            for (int i = 0; i < packageNames.Count() -1; i++)
                packageText.AppendLine("- " + packageNames[i] + " - " + packageVersions[i]);
            packageText.AppendLine("- ...and you as Mega Man X!");

            var readmeText = System.IO.File.ReadAllText(readmePath);
            var readmeTemplateText = System.IO.File.ReadAllText(readmeTemplatePath);

            readmeTemplateText = readmeTemplateText.Replace("[title text]", titleText);
            readmeTemplateText = readmeTemplateText.Replace("[app name]", appName);
            readmeTemplateText = readmeTemplateText.Replace("[argument help]", argsHelp);
            readmeTemplateText = readmeTemplateText.Replace("[playlist options]", optionsHelp);
            readmeTemplateText = readmeTemplateText.Replace("[playlist parameters]", paramsHelp);
            readmeTemplateText = readmeTemplateText.Replace("[config settings]", configSettings);
            readmeTemplateText = readmeTemplateText.Replace("[packages]", packageText.ToString());

            //standardize new lines, don't want them flipping about between platforms
            readmeTemplateText = readmeTemplateText.ReplaceLineEndings("\r\n");

            if (readmeTemplateText != readmeText)
            {
                System.IO.File.WriteAllText(readmePath, readmeTemplateText);
                System.IO.File.WriteAllText(System.IO.Path.Join(smallDocsDirPath, "options.md"), optionsHelp);
                System.IO.File.WriteAllText(System.IO.Path.Join(smallDocsDirPath, "parameters.md"), paramsHelp);
                System.IO.File.WriteAllText(System.IO.Path.Join(smallDocsDirPath, "config.md"), configSettings);
                Console.WriteLine("README.MD updated");
            }
            else
                Console.WriteLine("No update to README.MD needed.");


        }
        static void RunReports(MySpotifyWrapper spotifyWrapper)
        {
            ReportLikedGenre(spotifyWrapper);
            ReportPlaylistDetails(spotifyWrapper);
            // ReportClauseTest(spotifyWrapper);
        }
        static void ReportLikedGenre(MySpotifyWrapper spotifyWrapper)
        {
            Console.WriteLine();
            Console.WriteLine("---liked genre report---");
            Console.WriteLine("started at " + DateTime.Now.ToString());

            //TODO simplify this; the data you need is already in likedTracks
            var artistIDs = spotifyWrapper.LikedTracks.SelectMany(t => t.ArtistIds)
                .Distinct()
                .ToList();

            //get the artists
            var artists = spotifyWrapper.GetArtists(artistIDs);

            //report records, assemble!

            var artistAllGenres = artists.SelectMany(a => a.Genres)
                .ToArray();

            var artistFirstGenres = artists.Select(a => a.Genres.FirstOrDefault())
                .ToArray();

            var trackAllGenres = spotifyWrapper.LikedTracks
                .SelectMany(t => t.ArtistGenres)
                .ToArray();

            var trackFirstGenres = spotifyWrapper.LikedTracks
                .Select(t => t.ArtistGenres.FirstOrDefault())
                .ToArray();

            //write reports!
            WriteGroupyReport(artistAllGenres, System.IO.Path.Join(Settings._ReportsFolderPath, "Genres", "All Artist Genres"));
            WriteGroupyReport(artistFirstGenres, System.IO.Path.Join(Settings._ReportsFolderPath, "Genres", "First Artist Genres"));
            //WriteGenreReport(albumAllGenres, System.IO.Path.Join(Settings._ReportsFolderPath, "Genres", "All Album Genres"));
            //WriteGenreReport(albumFirstGenres, System.IO.Path.Join(Settings._ReportsFolderPath, "Genres", "First Album Genres"));
            WriteGroupyReport(trackAllGenres, System.IO.Path.Join(Settings._ReportsFolderPath, "Genres", "All Track Genres"));
            WriteGroupyReport(trackFirstGenres, System.IO.Path.Join(Settings._ReportsFolderPath, "Genres", "First Track Genres"));

        }

        static void ReportPlaylistDetails(MySpotifyWrapper spotifyWrapper)
        {
            Console.WriteLine();
            Console.WriteLine("---playlist details report---");
            Console.WriteLine("started at " + DateTime.Now.ToString());

            var dir = System.IO.Path.Join(Settings._ReportsFolderPath, "Playlist Details");
            var invalidFilenameChars = System.IO.Path.GetInvalidFileNameChars();

            var playlists = spotifyWrapper.GetUsersPlaylists(refreshCache: true);

            foreach(var file in System.IO.Directory.GetFiles(dir))
                System.IO.File.Delete(file);

            foreach (var playlist in playlists)
            {
                var tracks = spotifyWrapper.GetTracksByPlaylist(new string[] { playlist.Id });

                var artistNames = tracks.SelectMany(t => t.ArtistNames).ToArray();
                var genreNames = tracks.SelectMany(t => t.ArtistGenres).ToArray();

                WriteGroupyReport(artistNames, System.IO.Path.Join(dir, playlist.Name.Where(c => !invalidFilenameChars.Contains(c)) + " - top artists"));
                WriteGroupyReport(genreNames, System.IO.Path.Join(dir, playlist.Name.Where(c => !invalidFilenameChars.Contains(c)) + " - top genres"));
            }

        }

        private static void WriteGroupyReport(IEnumerable<string> records, string path)
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

            var maxNameLength = records.Max(x => (x ?? nullReplacementString).Length);

            //format report
            var reportLines = records
                .Where(x => x != null)
                .GroupBy(x => x, StringComparer.InvariantCultureIgnoreCase)
                .Select(g => new
                {
                    GroupString = g.Key,
                    RecordCount = g.Count(),
                })
                .OrderByDescending(x => x.RecordCount)
                .ThenBy(x => x.GroupString)
                .Select(g =>
                    (g.GroupString ?? nullReplacementString) + new String(' ', maxNameLength - g.GroupString.Length) + " -- " + g.RecordCount.ToString("#,##0")
                )
                .ToArray()
                ;

            //write report
            System.IO.File.WriteAllLines(path, reportLines);
        }
        static void ReportClauseTest(MySpotifyWrapper spotifyWrapper)
        {
            Console.WriteLine();
            Console.WriteLine("---clause test report---");
            Console.WriteLine("started at " + DateTime.Now.ToString());

            var path = System.IO.Path.Join(Settings._ReportsFolderPath, "Clause Test");

            //var tracks = spotifyWrapper.LikedTracks;
            var tracks = spotifyWrapper.GetTracksByPlaylist(new string[] { "*pvgc*auto*" });

            var records = tracks.Select(t => new
            {
                t.ArtistNames,
                t.AlbumName,
                TrackName = t.Name,
                t.ParsedTrackName.TrackShortName,
                t.ParsedTrackName.AlbumShortName,
                t.ParsedTrackName.FromClause,
                t.ParsedTrackName.ForClause,
                t.ParsedTrackName.FeaturingClause,
                t.ParsedTrackName.FeatClause,
                t.ParsedTrackName.RemixClause,
                t.ParsedTrackName.CutClause,
                t.ParsedTrackName.EditClause,
                t.ParsedTrackName.DubClause,
                t.ParsedTrackName.CoverClause,
                t.ParsedTrackName.InstrumentalClause,
                t.ParsedTrackName.AcousticClause

            }).ToArray();

            records.ToCSV(path);
        }
    }
}
