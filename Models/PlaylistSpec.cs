using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static spotify_playlist_generator.Program;

namespace spotify_playlist_generator.Models
{
    enum PlaylistType
    {
        Likes,
        AllByArtist,
        Top,
        None,
    }

    enum ObjectType
    {
        Artist,
        Playlist,
        Genre,
        Album,
        Track,
        None,
    }

    enum Sort
    {
        Dont,
        Liked,
        Release,
        Artist,
    }

    internal class PlaylistSpec
    {
        public string Path { get; set; }
        public string[] FolderNames { get; set; }
        public string PlaylistName { get; set; }
        public string FinalPlaylistName
        {
            get
            {
                return (Settings._StartPlaylistsWith ?? string.Empty) + this.PlaylistName;
            }
        }
        [Description("Exchange artist names for artist IDs. Saves time when running but looks worse. Same behaviour as --modify-playlist-file")]
        public bool AddArtistIDs { get; set; }
        [Description("Assume any lines with no parameter are this parameter. Great for pasting lists of artists.")]
        public string Default { get; set; }
        [Description("If the playlist has no tracks, delete it.")]
        public bool DeleteIfEmpty { get; set; }
        [Description("If tracks no longer fall within the scope of the playlist leave them anyway.")]
        public bool DontRemoveTracks { get; set; }
        [Description("Actively keep this playlist sorted. Can also be set globally in config.ini")]
        public bool UpdateSort { get; set; }
        [Description("Exclude liked songs from this playlist.")]
        public bool NoLikes { get; set; }
        [Description("Limit the amount of tracks per artist, prioritizing by popularity.")]
        public int LimitPerArtist { get; set; }
        [Description("Limit the amount of tracks per album, prioritizing by popularity.")]
        public int LimitPerAlbum { get; set; }
        [Description("Don't touch the artwork, even if told to.")]
        public bool LeaveImageAlone { get; set; }
        private bool _sortSet;
        private Sort _sort;
        [Description("How to sort the playlist. If not supplied this is decided based on playlist parameters. Options are [enum values].")]
        public Sort Sort
        {
            get { return _sort; }
            set 
            {
                _sort = value;
                _sortSet = true;
            }
        }
        public IList<string> OptionsErrors { get; private set; }

        public SpecLine[] SpecLines { get; set; }
        public List<FullTrackDetails> Tracks { get; set; }

        public PlaylistSpec(string path)
        {
            UpdateFromDisk(path);
        }
        public PlaylistType GetPlaylistType
        {
            get
            {
                if (!this.SpecLines.Any())
                    return PlaylistType.None;

                return this.SpecLines
                    .Where(line => line.IsValidParameter)
                    .GroupBy(line => line.LineType)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .First();
            }
        }
        public ObjectType GetSubjectType
        {
            get
            {
                if (!this.SpecLines.Any())
                    return ObjectType.None;

                return this.SpecLines
                    .GroupBy(line => line.SubjectType)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .First();
            }
        }


        public PlaylistSpec(string playlistName, string playlistSpecification)
        {
            var fileLines = playlistSpecification.ReplaceLineEndings().Split(Environment.NewLine);
            Initialize(path: null, playlistName: playlistName, fileLines: fileLines);
            this.Write();
        }

        public void UpdateFromDisk()
        {
            UpdateFromDisk(this.Path);
        }

        public void UpdateFromDisk(string path)
        {
            this._sortSet = false;
            var playlistName = System.IO.Path.GetFileNameWithoutExtension(path);
            var filesLines = System.IO.File.ReadAllText(path).ReplaceLineEndings().Split(Environment.NewLine);
            Initialize(path: path, playlistName: playlistName, fileLines: filesLines);
        }

        private void Initialize(string path, string playlistName, string[] fileLines)
        {

            this.Path = path;
            this.PlaylistName = playlistName;

            var playlistOptions = fileLines
                .Select(line => line.Split(Program.Settings._CommentString, 2).First())
                .Where(line => line.StartsWith(Program.Settings._ParameterString))
                .Distinct()
                .ToArray();

            this.ParseOptions(playlistOptions);

            this.SpecLines = fileLines
                .Select(line => new
                {
                    RawLine = line,
                    SanitizedStart = line.Split(Program.Settings._CommentString, 2)
                })
                .Select(line => new
                {
                    line.RawLine,
                    SanitizedLine = line.SanitizedStart.First().Trim(),
                    Comment = (line.SanitizedStart.Length > 1 ? line.SanitizedStart.Last() : String.Empty)
                })
                .Select(line => new
                {
                    line.RawLine,
                    line.SanitizedLine,
                    line.Comment,
                    ParameterStart = line.SanitizedLine.Split(":", 2)
                })
                .Select(line => new
                {
                    line.RawLine,
                    line.SanitizedLine,
                    line.Comment,
                    ParameterValue = line.ParameterStart.Last().Trim(),
                    line.ParameterStart
                })
                .Select(line => new SpecLine
                {
                    RawLine = line.RawLine,
                    SanitizedLine = line.SanitizedLine,
                    Comment = line.Comment,
                    ParameterValue = line.ParameterValue,
                    ParameterName = (line.ParameterStart.Length > 1 ? line.ParameterStart.First() : Default ?? String.Empty).Trim()
                })
                .Distinct()
                .ToArray();

            //randomly name the playlist if the user forgot to
            if (string.IsNullOrWhiteSpace(this.Path) && string.IsNullOrWhiteSpace(this.PlaylistName))
            {
                var firstArtistName = this.SpecLines
                    .Select(line => line.ParameterValue)
                    .Where(x => !string.IsNullOrWhiteSpace(x) && !Program.idRegex.Match(x).Success)
                    .FirstOrDefault();

                var rnd = new Random();

                if (firstArtistName != null && rnd.Next(1, 10) == 1)
                {
                    this.PlaylistName = firstArtistName + " etc etc";
                }
                else
                {
                    switch (rnd.Next(1, 25))
                    {
                        case 1:
                            this.PlaylistName = "You forgot to name this playlist";
                            break;
                        case 2:
                            this.PlaylistName = "not every playlist needs a name";
                            break;
                        case 3:
                            this.PlaylistName = "songs my mother warned me about";
                            break;
                        case 4:
                            this.PlaylistName = "Horatio";
                            break;
                        case 5:
                            this.PlaylistName = "a nameless playlist for nameless nights";
                            break;
                        case 6:
                            this.PlaylistName = "songs to hunt small game to";
                            break;
                        case 7:
                            this.PlaylistName = "a playlist with a blank birth certificate";
                            break;
                        case 8:
                            this.PlaylistName = Guid.NewGuid().ToString();
                            break;
                        case 9:
                            this.PlaylistName = "a playlist with an identity crisis";
                            break;
                        case 10:
                            this.PlaylistName = "Alas, poor nameless playlist, I knew him well.";
                            break;

                        default:
                            this.PlaylistName = "Nameless Playlist #" + rnd.Next(301, 999).ToString("##0");
                            break;
                    }

                }
            }

            //assign sorts
            if (!this._sortSet)
            {
                if (this.GetPlaylistType == PlaylistType.Likes)
                    this.Sort = Sort.Liked;
                else if (this.GetPlaylistType == PlaylistType.AllByArtist || this.GetPlaylistType == PlaylistType.None)
                    this.Sort = Sort.Release;
                else if (this.GetPlaylistType == PlaylistType.Top)
                    this.Sort = Sort.Artist;
            }

            //build a path if necessary
            //need to store paths a little better. Hardcoding the folder named "playlists" isn't great
            if (string.IsNullOrWhiteSpace(this.Path) && !string.IsNullOrWhiteSpace(this.PlaylistName))
            {
                this.Path = System.IO.Path.Join(Program.Settings._PlaylistFolderPath, "Playlists", this.PlaylistName + ".txt");
            }

            //spotify api does not support folders
            this.FolderNames = System.IO.Path.GetRelativePath(Program.Settings._PlaylistFolderPath, System.IO.Path.GetDirectoryName(this.Path))
                .Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(folder => folder != "Playlists")
                .ToArray()
                ;

        }

        public void ParseOptions(string[] lines)
        {
            var optionProps = typeof(PlaylistSpec).GetProperties()
                                .Where(prop => !string.IsNullOrWhiteSpace((prop.GetCustomAttribute(typeof(DescriptionAttribute), true) as DescriptionAttribute)?.Description))
                                .ToArray();

            var errorText = new List<string>();
            foreach (var line in lines)
            {
                //skip non-option lines
                if (!line.StartsWith(Program.Settings._ParameterString))
                    continue;

                //parse out values
                var keyValue = line.Split(":", 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var optionName = keyValue.First();
                var optionValue = (keyValue.Length > 1 ? keyValue.Last() : null);
                var prop = optionProps.Where(p => p.Name.Like(optionName)).FirstOrDefault();
                if (prop == null)
                {
                    errorText.Add("Unknown option: " + optionName);
                    continue;
                }

                //assign values based on property/option type
                var badValue = false;
                if (prop.PropertyType == typeof(bool))
                {
                    //user can either specify the value or just provide the option name for "true"
                    if (string.IsNullOrWhiteSpace(optionValue))
                        prop.SetValue(this, true);
                    else if (bool.TryParse(optionValue, out var result))
                        prop.SetValue(this, result);
                    else
                        badValue = true;
                }
                else if (prop.PropertyType == typeof(string))
                    prop.SetValue(this, optionValue);
                else if (prop.PropertyType == typeof(int))
                {
                    if (int.TryParse(optionValue, out var result))
                        prop.SetValue(this, result);
                    else
                        badValue = true;
                }
                else if (prop.PropertyType.IsEnum)
                {
                    if (Enum.TryParse(prop.PropertyType, optionValue.Remove("'"), true, out var result))
                        prop.SetValue(this, result);
                    else
                        badValue= true;
                }

                //report on bad values
                if (badValue)
                    errorText.Add("Invalid option value for " + optionName + ": " + optionValue);
            }

            this.OptionsErrors = errorText;
        }
        public string[] GetParameterValues(string ParameterName)
        {
            if (this.GetGroupedParameters().TryGetValue(ParameterName.Trim(), out var output))
                return output;

            return Array.Empty<string>();
        }

        public Dictionary<string, string[]> GetGroupedParameters()
        {
            var output = this.SpecLines
                .Where(line => line.IsValidParameter)
                .GroupBy(line => line.ParameterName.Trim(), StringComparer.InvariantCultureIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(line => line.ParameterValue).ToArray(), StringComparer.InvariantCultureIgnoreCase)
                ;
            return output;
        }

        public void Write()
        {
            System.IO.File.WriteAllLines(this.Path, this.SpecLines.Select(line => line.RawLine.ReplaceLineEndings()));
        }

        public override string ToString()
        {
            return this.PlaylistName + " SpecLines:" + this.SpecLines.Count().ToString("#,##0");
        }
    }


    internal class SpecLine
    {
        public string Comment { get; set; }
        public string RawLine { get; set; }
        public string SanitizedLine { get; set; }
        public string ParameterName { get; set; }
        public string ParameterValue { get; set; }

        public bool IsValidParameter
        {
            get
            {
                return
                    !this.SanitizedLine.StartsWith(Program.Settings._ParameterString) &&
                    !string.IsNullOrWhiteSpace(this.SanitizedLine) &&
                    !string.IsNullOrWhiteSpace(this.ParameterValue);
                    ;
            }
        }

        public PlaylistType LineType
        {
            get
            {
                if (!this.IsValidParameter)
                    return PlaylistType.None;
                else if (this.ParameterName.Like("likes*"))
                    return PlaylistType.Likes;
                else if (this.ParameterName.Like("all*") || this.ParameterName.Like("album*"))
                    return PlaylistType.AllByArtist;
                else if (this.ParameterName.Like("top*"))
                    return PlaylistType.Top;
                else
                    return PlaylistType.None;
            }
        }

        public ObjectType SubjectType
        {
            get
            {
                if (!this.IsValidParameter)
                    return ObjectType.None;
                else if (this.ParameterName.Like("playlist"))
                    return ObjectType.Playlist;
                else if (this.ParameterName.Like("artist"))
                    return ObjectType.Artist;
                else if (this.ParameterName.Like("genre"))
                    return ObjectType.Genre;
                else if (this.ParameterName.Like("album"))
                    return ObjectType.Album;
                else if (this.ParameterName.Like("track"))
                    return ObjectType.Track;
                else
                    return ObjectType.None;
            }
        }

        public override string ToString()
        {
            return this.RawLine;
        }

    }
}
