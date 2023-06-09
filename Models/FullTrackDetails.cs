using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace spotify_playlist_generator.Models
{
    [Serializable]
    internal class FullTrackDetails
    {
        public string AlbumId { get; set; }
        public string AlbumName { get; set; }
        public string AlbumType { get; set; }
        public List<string> ArtistGenres { get; set; }
        public List<string> ArtistIds { get; set; }
        public List<string> ArtistNames { get; set; }

        [NonSerialized] //liked is only valid for this session, so don't save it
        public DateTime LikedAt;
        public string Name { get; set; }
        public int Popularity { get; set; }
        public DateOnly ReleaseDate { get; set; }
        public Guid SessionID { get; set; }
        public string TrackId { get; set; }
        public int TrackNumber { get; set; }
        public string TrackUri { get; set; }

        [NonSerialized] //liked is only valid for this session, so don't save it
        public bool Source_Liked;
        [NonSerialized] //top is only valid for this session, so don't save it
        public bool Source_Top;

        public bool Source_AllTracks { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="fullTrack"></param>
        /// <param name="fullArtists">Artists tied to this track. Can include extra artists without issue.</param>
        /// <param name="topTrack"></param>
        /// <param name="allTracksTrack"></param>
        /// <param name="sessionID"></param>
        public FullTrackDetails(FullTrack fullTrack, IEnumerable<FullArtist> fullArtists, Guid sessionID
            , bool topTrack = false, bool allTracksTrack = false)
        {
            Initialize(fullTrack, fullArtists, sessionID:sessionID, topTrack: topTrack, allTracksTrack: allTracksTrack);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="savedTrack"></param>
        /// <param name="fullArtists">Artists tied to this track. Can include extra artists without issue.</param>
        /// <param name="sessionID"></param>
        public FullTrackDetails(SavedTrack savedTrack, IEnumerable<FullArtist> fullArtists, Guid sessionID)
        {
            Initialize(savedTrack.Track, fullArtists, sessionID: sessionID, savedTrack: savedTrack);
        }

        /// <summary>
        /// Don't use this constructor. This is only here for the JSON deserializer, not for you.
        /// </summary>
        public FullTrackDetails()
        {
        }


        private void Initialize(FullTrack fullTrack, IEnumerable<FullArtist> fullArtists, Guid sessionID, SavedTrack savedTrack = null
            , bool topTrack = false, bool allTracksTrack = false)
        {
            // how the hell is fullArtists getting in here null?
            if (fullTrack == null)
                throw new ArgumentNullException(nameof(fullTrack));
            else if (fullArtists == null)
                throw new ArgumentNullException(nameof(fullArtists));

            //make sure this is only the related artists
            fullArtists = fullArtists.Where(a => fullTrack.Artists.Any(ax => ax.Id == a.Id)).ToArray();

            //map properties over to our wrapper
            AlbumId = fullTrack.Album.Id;
            AlbumName = fullTrack.Album.Name;
            AlbumType = fullTrack.Album.AlbumType;
            ArtistGenres = fullArtists.SelectMany(a => a.Genres).ToList();
            ArtistIds = fullArtists.Select(a => a.Id).ToList();
            ArtistNames = fullArtists.Select(a => a.Name).ToList();
            Name = fullTrack.Name;
            Popularity = fullTrack.Popularity;
            var year = fullTrack.Album.ReleaseDate.Substring(0, 4);
            var month = fullTrack.Album.ReleaseDate.SoftSubstring(5, 2).NullIfEmpty() ?? "01";
            var day = fullTrack.Album.ReleaseDate.SoftSubstring(8, 2).NullIfEmpty() ?? "01";
            ReleaseDate = new DateOnly(int.Parse(year), int.Parse(month), int.Parse(day));
            TrackId = fullTrack.Id;
            TrackNumber = fullTrack.TrackNumber;
            TrackUri = fullTrack.Uri;
            SessionID = sessionID;
            Source_AllTracks = allTracksTrack;
            Source_Top = topTrack;

            if (savedTrack != null)
            {
                LikedAt = savedTrack.AddedAt;
                Source_Liked = true;
            }
        }

        public override bool Equals(object obj)
        {
            var item = obj as FullTrackDetails;

            if (item == null)
            {
                return false;
            }

            return TrackId.Equals(item.TrackId);
        }

        public bool AlbumStringIsMatch(string value)
        {

            var valueArray = value.Split(Program.dashes, 2, StringSplitOptions.TrimEntries);

            //if given one id and it doesn't match the album, return false
            if (valueArray.Length == 1 && Program.idRegex.IsMatch(value))
                return this.AlbumId == value;
            else if (valueArray.Length <= 1)
                return false;

            var artistNameOrID = valueArray[0];
            var albumName = valueArray[1];

            //if album name doesn't match, return false
            if (!(
                        this.AlbumName.Like(albumName) ||
                        this.AlbumName.LevenshteinPercentChange(albumName) <= 0.25)
                )
                return false;

            //return match on artist id or artist name as appropro
            if (Program.idRegex.IsMatch(artistNameOrID))
                return this.ArtistIds.Any(id => id == artistNameOrID);
            else
                return this.ArtistNames.Any(name => name.Like(artistNameOrID));

        }

        public override int GetHashCode()
        {
            return TrackId.GetHashCode();
        }

        public override string ToString()
        {
            return this.ArtistNames.Join(", ") + " - " + this.Name + " " + this.ReleaseDate.ToString("yyyy-mm-dd");
        }
    }
}
