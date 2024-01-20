using spotify_playlist_generator.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static spotify_playlist_generator.Program;

namespace spotify_playlist_generator
{
    internal class PlaylistParameterDefinition
    {
        public string ParameterName { get; set; }
        public string[] Aliases { get; set; } = Array.Empty<string>();
        public string Description { get; set; }
        public bool IsExclusion
        {
            get
            {
                var output = this.ParameterName.Trim().StartsWith(Program.dashes);
                return output;
            }
        }

        public List<FullTrackDetails> GetTracks(MySpotifyWrapper spotifyWrapper, IEnumerable<string> parameterValues, IList<FullTrackDetails> likedTracks = null, IList<FullTrackDetails> existingTracks = null, IList<string> exceptArtists = null)
        {
            var output = this.TracksFunc.Invoke(spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists);

            return output;
        }
        protected Func<MySpotifyWrapper, IEnumerable<string>, IList<FullTrackDetails>, IList<FullTrackDetails>, IEnumerable<string>, List<FullTrackDetails>> TracksFunc { get; set; }

        private static IList<PlaylistParameterDefinition> _AllDefinitions;
        public static IList<PlaylistParameterDefinition> AllDefinitions
        {
            get
            {
                //cache it yo
                if (_AllDefinitions != null)
                    return _AllDefinitions;

                _AllDefinitions = new List<PlaylistParameterDefinition>{
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "LikesByArtist",
                        Description = "Liked tracks by this artist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var tracks = likedTracks.Where(t =>
                                t.ArtistNames.Any(artistName => parameterValues.Any(pv => artistName.Like(pv))) ||
                                t.ArtistIds.Any(artistID => parameterValues.Contains(artistID, StringComparer.InvariantCultureIgnoreCase))
                                )
                                .ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "LikesByArtistFromPlaylist",
                        Description = "Liked tracks by all artists in this playlist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var artistIDs = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .GetArtistIDs(exceptArtists);

                            var tracks = spotifyWrapper.LikedTracks.Where(t =>
                                t.ArtistIds.Any(artistID => artistIDs.Contains(artistID, StringComparer.InvariantCultureIgnoreCase))
                                )
                                .ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "LikesFromPlaylist",
                        Description = "All liked tracks in this playlist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var tracks = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .Where(t =>
                                    spotifyWrapper.LikedTracks.Any(lt => 1 == 2
                                        || lt.ComparisonString == t.ComparisonString
                                        || lt.TrackId == t.TrackId
                                        )
                                    )
                                .ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "AllByArtist",
                        Description = "All tracks by this artist. Can be intensive. Specifying artist ID instead of artist name can reduce the load somewhat. Using an artist name is very likely to include duplicate artists with the same name.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var tracks = spotifyWrapper.GetTracksByArtists(parameterValues.ToArray());

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "AllByArtistFromPlaylist",
                        Description = "All tracks by all artists in this playlist",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var artistIDs = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .GetArtistIDs(exceptArtists);

                            var tracks = spotifyWrapper.GetTracksByArtists(artistIDs);

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "AlbumsFromPlaylist",
                        Description = "All tracks from all albums with any song in this playlist",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var albumIDs = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .Select(t => t.AlbumId)
                                .Distinct()
                                .ToArray();

                            var tracks = spotifyWrapper.GetTracksByAlbum(albumIDs);

                            // a simple but weird way to remove any tracks that *only* have artist IDs told to ignore
                            if (exceptArtists?.Any() ?? false)
                            {
                                var artistIDs = tracks.GetArtistIDs(exceptArtists).ToArray();
                                tracks = tracks
                                .Where(t => t.ArtistIds.Any(id => artistIDs.Contains(id)))
                                .ToList()
                                ;
                            }

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "TopByArtist",
                        Description = "The top five tracks on Spotify by this artist. Has the same performance issues as AllByArtist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var tracks = spotifyWrapper.GetTopTracksByArtists(parameterValues);

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "Album",
                        Description = "All tracks on this album. Accepts album ID or names in the form \"Artist Name - Album Name\". Album ID performs significantly faster.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var tracks = spotifyWrapper.GetTracksByAlbum(parameterValues.ToList());

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "TopByArtistFromPlaylist",
                        Description = "The top five tracks on Spotify by every artist in this playlist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var artistIDs = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .GetArtistIDs(exceptArtists);

                            var tracks = spotifyWrapper.GetTopTracksByArtists(artistIDs);

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "LikesByGenre",
                        Description = "All liked tracks with the specified genre. Wildcards are very useful here.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var tracks = spotifyWrapper.LikedTracks.Where(t =>
                            t.ArtistGenres.Any(genreName => parameterValues.Any(g => genreName.Like(g)))
                            )
                            .ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "PlaylistTracks",
                        Description = "Tracks in this playlist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var playlistTracks = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .ToList();

                            return playlistTracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "-Artist",
                        Description = "Exclude all tracks matching this artist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var tracks = existingTracks.Where(t =>
                                t.ArtistNames.Any(artistName => parameterValues.Any(pv => artistName.Like(pv))) ||
                                t.ArtistIds.Any(artistID => parameterValues.Contains(artistID, StringComparer.InvariantCultureIgnoreCase))
                                ).ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "-Album",
                        Description = "Exclude all tracks matching this album.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var separators = new string[] { Settings._SeparatorString, ":" };

                            var excludeAlbumDetails = parameterValues
                                .Select(x => x.Split(separators, 2, StringSplitOptions.TrimEntries))
                                .Where(x => x.Length == 2)
                                .Select(x => new
                                {
                                    ArtistNameOrID = x[0],
                                    AlbumName = x[1]
                                })
                                    .ToArray();

                            var albumIDs = parameterValues
                                .Where(x => idRegex.Match(x).Success && !separators.Any(sep => x.Contains(sep)))
                                .ToArray();

                            var tracks = new List<FullTrackDetails>();

                            tracks.AddRange(
                                existingTracks.Where(t =>
                                excludeAlbumDetails.Any(exAlbum =>
                                    (
                                    t.ArtistNames.Any(artistName => artistName.Like(exAlbum.ArtistNameOrID)) ||
                                    t.ArtistIds.Any(artistID => artistID == exAlbum.ArtistNameOrID)
                                    )
                                    && t.AlbumName.Like(exAlbum.AlbumName)
                                )
                                )
                                );

                            tracks.AddRange(
                                existingTracks.Where(t => albumIDs.Any(exAlbumID => t.AlbumId == exAlbumID))
                            );

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "-PlaylistTracks",
                        Description = "Exclude all tracks in this playlist.",
                        Aliases = new string[] { "-TracksFromPlaylist" },
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var playlistTracks = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .ToArray();

                            var tracks = existingTracks
                                .Where(t =>
                                    playlistTracks.Any(pt => 1 == 2
                                        || pt.ComparisonString == t.ComparisonString
                                        || pt.TrackId == t.TrackId
                                        )
                                    )
                                .ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "-PlaylistArtists",
                        Description = "Exclude all artists in this playlist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var artistIDs = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .SelectMany(t => t.ArtistIds)
                                .Distinct()
                                .ToArray();

                            var tracks = existingTracks.Where(t =>
                                t.ArtistIds.Any(artistID => artistIDs.Contains(artistID, StringComparer.InvariantCultureIgnoreCase))
                                ).ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "-PlaylistAlbums",
                        Description = "Exclude all albums in this playlist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var albumsIDs = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .Select(t => t.AlbumId)
                                .Distinct()
                                .ToArray();

                            var tracks = existingTracks.Where(t =>
                                albumsIDs.Contains(t.AlbumId)
                                ).ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "-Genre",
                        Description = "Exclude all tracks matching this genre.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var tracks = existingTracks.Where(t =>
                                    t.ArtistGenres.Any(g => parameterValues.Any(eg => g.Like(eg)))
                                ).ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "-Track",
                        Description = "Exclude all tracks with a matching name or ID.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var tracks = existingTracks.Where(t =>
                                    parameterValues.Any(x => t.Name.Like(x)) ||
                                    parameterValues.Any(x => t.TrackId == x)
                                ).ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "-From",
                        Description = "Exclude all tracks with a matching \"from\" clause in the track name. This is primarily useful for soundtrack and video game covers.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var tracks = existingTracks.Where(t =>
                                    parameterValues.Any(x => t.ParsedTrackName.FromClause.Like(x))
                                ).ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "Track",
                        Description = "Single track by ID.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks, exceptArtists) =>
                        {
                            var tracks = spotifyWrapper.GetTracks(parameterValues);
                            return tracks;
                        }
                    },


                };

                return _AllDefinitions;
            }


        }

        private static string[] _ValidNamesAndAliases;

        public static string[] ValidNamesAndAliases
        {
            get 
            {
                if (_ValidNamesAndAliases == null)
                {
                    var names = PlaylistParameterDefinition
                        .AllDefinitions
                        .Select(ppd => ppd.ParameterName)
                        .ToList();
                    var aliases = PlaylistParameterDefinition
                        .AllDefinitions
                        .SelectMany(ppd => ppd.Aliases)
                        .ToList();

                    // use this as a chance to check for alias collisions
                    if (aliases.Count() != aliases
                        .Select(x => x.Standardize())
                        .Distinct().Count())
                        throw new Exception("Playlist parameter alias collision detected!");

                    _ValidNamesAndAliases = names.Union(aliases).Distinct().ToArray();
                }

                return _ValidNamesAndAliases;
            }
        }

    }
}
