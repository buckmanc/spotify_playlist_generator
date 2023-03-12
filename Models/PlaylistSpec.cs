using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        None
    }

    enum ObjectType
    {
        Artist,
        Playlist,
        Genre,
        Album,
        Track,
        None
    }

    enum Sort
    {
        Liked,
        Release,
        Top,
        Dont
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
        public bool AddArtistIDs { get; set; }
        public string DefaultParameter { get; set; }
        public bool DeleteIfEmpty { get; set; }
        public bool DontRemoveTracks { get; set; }
        public bool MaintainSort { get; set; }
        public bool NoLikes { get; set; }
        public int LimitPerArtist { get; set; }
        public int LimitPerAlbum { get; set; }
        public bool LeaveImageAlone { get; set; }
        public Sort Sort { get; set; }
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
            var playlistName = System.IO.Path.GetFileNameWithoutExtension(path);
            var filesLines = System.IO.File.ReadAllText(path).ReplaceLineEndings().Split(Environment.NewLine);
            Initialize(path: path, playlistName: playlistName, fileLines: filesLines);

        }

        private void Initialize(string path, string playlistName, string[] fileLines)
        {

            this.Path = path;
            this.PlaylistName = playlistName;

            var playlistSettings = fileLines
                .Select(line => line.Split(Program.Settings._CommentString, 2).First())
                .Where(line => line.StartsWith(Program.Settings._ParameterString))
                .Distinct()
                .ToArray();

            this.DefaultParameter = playlistSettings
                .Where(line => line.StartsWith(Settings._ParameterString + "default:", StringComparison.InvariantCultureIgnoreCase))
                .Select(line => line.Split(":", 2).Last())
                .FirstOrDefault()
                ;

            this.DeleteIfEmpty = playlistSettings
                .Any(line => line.ToLower() == Settings._ParameterString.ToLower() + "deleteifempty");

            this.DontRemoveTracks = playlistSettings
                .Any(line => line.ToLower() == Settings._ParameterString.ToLower().Remove("'") + "dontremovetracks");

            this.AddArtistIDs = playlistSettings
                .Any(line => line.ToLower() == Settings._ParameterString.ToLower().Remove("'") + "addartistids");

            this.MaintainSort = playlistSettings
                .Any(line => line.ToLower() == Settings._ParameterString.ToLower() + "maintainsort");

            this.LeaveImageAlone = playlistSettings
                .Any(line => line.ToLower() == Settings._ParameterString.ToLower() + "leaveimagealone");

            this.NoLikes = playlistSettings
                .Any(line => line.ToLower() == Settings._ParameterString.ToLower() + "nolikes");

            var sortString = playlistSettings
                .Where(line => line.StartsWith(Settings._ParameterString + "sort:", StringComparison.InvariantCultureIgnoreCase))
                .Select(line => line.Split(":", 2).Last())
                .FirstOrDefault()
                ;

            // sort enum assigned at the bottom as it depends on spec lines

            var dummy = 0;
            this.LimitPerArtist = playlistSettings
                .Where(line => line.StartsWith(Settings._ParameterString + "limitperartist:", StringComparison.InvariantCultureIgnoreCase))
                .Select(line => line.Split(":", 2).Last())
                .Where(line => int.TryParse(line, out dummy))
                .Select(line => int.Parse(line))
                .FirstOrDefault()
                ;

            this.LimitPerAlbum = playlistSettings
                .Where(line => line.StartsWith(Settings._ParameterString + "limitperalbum:", StringComparison.InvariantCultureIgnoreCase))
                .Select(line => line.Split(":", 2).Last())
                .Where(line => int.TryParse(line, out dummy))
                .Select(line => int.Parse(line))
                .FirstOrDefault()
                ;

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
                    ParameterName = (line.ParameterStart.Length > 1 ? line.ParameterStart.First() : DefaultParameter ?? String.Empty).Trim()
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
            if (sortString != null)
                this.Sort = (Sort)Enum.Parse(typeof(Sort), sortString.Remove("'"), true);
            else if (this.GetPlaylistType == PlaylistType.Likes)
                this.Sort = Sort.Liked;
            else if (this.GetPlaylistType == PlaylistType.AllByArtist || this.GetPlaylistType == PlaylistType.None)
                this.Sort = Sort.Release;
            else if (this.GetPlaylistType == PlaylistType.Top)
                this.Sort = Sort.Top;

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
                else if (this.ParameterName.Like("likes"))
                    return PlaylistType.Likes;
                else if (this.ParameterName.Like("all"))
                    return PlaylistType.AllByArtist;
                else if (this.ParameterName.Like("top"))
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

    }
}
