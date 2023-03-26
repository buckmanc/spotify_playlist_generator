﻿using spotify_playlist_generator.Models;
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
        public string Description { get; set; }
        public bool Exclusion { get => this.ParameterName.Trim().StartsWith("-"); }

        public List<FullTrackDetails> GetTracks(MySpotifyWrapper spotifyWrapper, IEnumerable<string> parameterValues, IList<FullTrackDetails> likedTracks = null, IList<FullTrackDetails> existingTracks = null)
        {
            return this.TracksFunc.Invoke(spotifyWrapper, parameterValues, likedTracks, existingTracks);
        }
        protected Func<MySpotifyWrapper, IEnumerable<string>, IList<FullTrackDetails>, IList<FullTrackDetails>, List<FullTrackDetails>> TracksFunc { get; set; }

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
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
                        {
                            var tracks = likedTracks.Where(t =>
                                t.ArtistNames.Any(artistName => parameterValues.Contains(artistName, StringComparer.InvariantCultureIgnoreCase)) ||
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
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
                        {
                            var artistIDs = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .SelectMany(t => t.ArtistIds)
                                .Distinct()
                                .ToArray();

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
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
                        {
                            var tracks = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .Where(t => spotifyWrapper.LikedTracks.Contains(t))
                                .ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "AllByArtist",
                        Description = "All tracks by this artist. Can be intensive. Specifying artist ID instead of artist name can reduce the load somewhat. Using an artist name is very likely to include duplicate artists with the same name.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
                        {
                            var tracks = spotifyWrapper.GetTracksByArtists(parameterValues.ToArray());

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "AllByArtistFromPlaylist",
                        Description = "All tracks by all artists in this playlist",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
                        {
                            var artistIDs = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .SelectMany(t => t.ArtistIds)
                                .Distinct()
                                .ToArray();

                            var tracks = spotifyWrapper.GetTracksByArtists(artistIDs);

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "TopByArtist",
                        Description = "The top five tracks on Spotify by this artist. Has the same performance issues as AllByArtist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
                        {
                            var tracks = spotifyWrapper.GetTopTracksByArtists(parameterValues);

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "Album",
                        Description = "All tracks on this album. Accepts album ID or names in the form \"Artist Name - Album Name\". Album ID performs significantly faster.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
                        {
                            var tracks = spotifyWrapper.GetTracksByAlbum(parameterValues);

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "TopByArtistFromPlaylist",
                        Description = "The top five tracks on Spotify by every artist in this playlist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
                        {
                            var artistIDs = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .SelectMany(t => t.ArtistIds)
                                .Distinct()
                                .ToArray();
                            
                            var tracks = spotifyWrapper.GetTopTracksByArtists(artistIDs);

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "LikesByGenre",
                        Description = "All liked tracks with the specified genre. Wildcards are very useful here.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
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
                        ParameterName = "-Artist",
                        Description = "Exclude all tracks matching this artist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
                        {
                            var tracks = existingTracks.Where(t =>
                                t.ArtistNames.Any(artistName => parameterValues.Contains(artistName, StringComparer.InvariantCultureIgnoreCase)) ||
                                t.ArtistIds.Any(artistID => parameterValues.Contains(artistID, StringComparer.InvariantCultureIgnoreCase))
                                ).ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "-Album",
                        Description = "Exclude all tracks matching this album.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
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
                                    //artist check
                                    (
                                    t.ArtistNames.Any(artistName => excludeAlbumDetails.Any(exAlbum => exAlbum.ArtistNameOrID.ToLower() == artistName.ToLower())) ||
                                    t.ArtistIds.Any(artistID => excludeAlbumDetails.Any(exAlbum => exAlbum.ArtistNameOrID.ToLower() == artistID.ToLower()))
                                    )
                                    && excludeAlbumDetails.Any(exAlbum => exAlbum.ArtistNameOrID.ToLower() == t.AlbumName.ToLower())
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
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
                        {
                            var playlistTracks = spotifyWrapper.GetTracksByPlaylist(parameterValues)
                                .ToArray();

                            var tracks = existingTracks.Where(t => playlistTracks.Contains(t)).ToList();

                            return tracks;
                        }
                    },
                    new PlaylistParameterDefinition()
                    {
                        ParameterName = "-PlaylistArtists",
                        Description = "Exclude all artists in this playlist.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
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
                        ParameterName = "-Genre",
                        Description = "Exclude all tracks matching this genre.",
                        TracksFunc = (spotifyWrapper, parameterValues, likedTracks, existingTracks) =>
                        {
                            var tracks = existingTracks.Where(t =>
                                    t.ArtistGenres.Any(g => parameterValues.Any(eg => g.Like(eg)))
                                ).ToList();

                            return tracks;
                        }
                    },


                };

                return _AllDefinitions;
            }


        }
    }
}