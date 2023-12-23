using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spotify_playlist_generator.Models
{
    //the only benefit of this class is local caching
    [Serializable]
    internal class FullPlaylistDetails
    {
        public string Description { get; set; }
        public string Id { get; set; }

        public string Name { get; set; }

        public string OwnerID { get; set; }

        public Guid SessionID { get; set; }
        public string SnapshotId { get; set; }

        public List<string> TrackIds { get; set; }

        /// <summary>
        /// Don't use this constructor. This is only here for the JSON deserializer, not for you.
        /// </summary>
        public FullPlaylistDetails()
        {
        }
        public FullPlaylistDetails(FullPlaylist simplePlaylist, Guid sessionID, List<FullTrackDetails> tracks)
        {
            Initialize(simplePlaylist, sessionID, tracks.Select(t => t.TrackId).ToList());
        }
        public FullPlaylistDetails(FullPlaylist simplePlaylist, Guid sessionID, List<string> trackIds)
        {
            Initialize(simplePlaylist, sessionID, trackIds);
        }

        private void Initialize(FullPlaylist simplePlaylist, Guid sessionID, List<string> trackIds)
        {
            Description = simplePlaylist.Description;
            Id = simplePlaylist.Id;
            Name = simplePlaylist.Name;
            OwnerID = simplePlaylist.Owner.Id;
            SnapshotId = simplePlaylist.SnapshotId;
            TrackIds = trackIds;
            SessionID = sessionID;
        }
    }
}
