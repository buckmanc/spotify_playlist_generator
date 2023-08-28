    AddParameterIDs           Exchange artist names for artist IDs. Saves time when running but looks worse. Same behaviour as --modify-playlist-file
    Default                   Assume any lines with no parameter are this parameter. Great for pasting lists of artists.
    DeleteIfEmpty             If the playlist has no tracks, delete it.
    DontModify                Don't modify the playlist spec file, even if told to.
    DontRemoveTracks          If tracks no longer fall within the scope of the playlist leave them anyway.
    ExceptArtistFromPlaylist  A comma delimited list of artists to not actively include when using one of the ArtistFromPlaylist parameters.
    ID                        The Spotify ID of this playlist after creation. Generally an output.
    LastRun                   The date of last run. Only updated when OnlyRunIfModified is set.
    LastXDays                 Limit to tracks released in the last X days.
    LeaveImageAlone           Don't touch the artwork, even if told to.
    LikedAfter                Limit to tracks liked after this date/time.
    LikedBefore               Limit to tracks liked before this date/time.
    LimitPerAlbum             Limit the amount of tracks per album, prioritizing by popularity.
    LimitPerArtist            Limit the amount of tracks per artist, prioritizing by popularity.
    LongerThan                Limit to tracks shorter than X minutes.
    NoExplicit                Exclude tracks marked explicit.
    NoLikes                   Exclude liked songs from this playlist.
    OnlyCreatePlaylist        Don't run again after the playlist has been created. This can be reset by removing the @ID from the file.
    OnlyRunIfModified         Don't run again unless the file has been modified.
    ReleasedAfter             Limit to tracks released after this date.
    ReleasedBefore            Limit to tracks released before this date.
    ShorterThan               Limit to tracks longer than X minutes.
    Sort                      How to sort the playlist. If not supplied this is decided based on playlist parameters. Options are Don't, Liked, Release, Artist.
    UpdateSort                Actively keep this playlist sorted. Can also be set globally in config.ini