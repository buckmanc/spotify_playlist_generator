                      _   _  __       
      ___ _ __   ___ | |_(_)/ _|_   _ 
     / __| '_ \ / _ \| __| | |_| | | |
     \__ \ |_) | (_) | |_| |  _| |_| |
     |___/ .__/ \___/ \__|_|_|  \__, |
         |_|                    |___/ 
    
            _             _ _     _   
      _ __ | | __ _ _   _| (_)___| |_ 
     | '_ \| |/ _` | | | | | / __| __|
     | |_) | | (_| | |_| | | \__ \ |_ 
     | .__/|_|\__,_|\__, |_|_|___/\__|
     |_|            |___/             
    
                                      _             
       __ _  ___ _ __   ___ _ __ __ _| |_ ___  _ __ 
      / _` |/ _ \ '_ \ / _ \ '__/ _` | __/ _ \| '__|
     | (_| |  __/ | | |  __/ | | (_| | || (_) | |   
      \__, |\___|_| |_|\___|_|  \__,_|\__\___/|_|   
      |___/                                         
    
=========

When I finally caved and switched from iTunes to Spotify there was a smart playlist shaped hole in my heart. And thus spotify playlist generator was born.

Maybe you don't have a box to schedule it on. Maybe you don't want to get [Spotify API keys](https://developer.spotify.com/dashboard/). Maybe you have a nerdy friend who could schedule it for you and share a Dropbox folder for your playlist specs. iykyk

This is a personal project and corners have been cut strategically in order to keep things moving within my time constraints. Git commits are less than atomic, file structure isn't the best, unit tests are AWOL, ~~documentation is completely absent,~~ the rubber duck is an unpaid intern, etc etc. However, it's been the Alfred of my playlists for a few years now and it isn't likely to retire to a European cafe anytime soon.

This documentation is a work in progress. ymmv

## Contents

* [Installation](#installation)
* [Options](#options)
    * [Playlist Parameters](#playlist-parameters)
    * [Playlist Options](#playlist-options)
    * [Config File Settings](#config-file-settings)
    * [Application Arguments](#application-arguments)
* [Usage Examples](#usage-examples)
* [Cool Command Line Tricks](#cool-command-line-tricks)
* [Credits](#credits)
    
# Installation
## Option 1 - make like Dolly
1. install [dotnet](https://dotnet.microsoft.com/en-us/download/dotnet/scripts)
2. get your [Spotify API keys](https://developer.spotify.com/dashboard/)
3. git clone

        git clone https://github.com/buckmanc/spotify_playlist_generator.git

4. add your API keys to the config file
5. run once and do the login process

        dotnet run

6. move the playlist folder or keep the default (put it in your Dropbox and edit wherever)
7. make some playlists!
8. schedule it to run nightly, weekly, once every full moon, etc.

## Option 2 - get a friend to run it for you
1. message your resident nerd
2. share a Dropbox folder with them
3. do some login stuff
4. profit

## Option 3
1. buy it
2. store it

# Options

<!-- Auto generated -->
## Playlist Parameters:
Playlist parameters tell the app what songs to add to the playlist. The core use of the app is right here.

All of these can accept the name of the artist/album/playlist or the Spotify ID. (You can pull the IDs from the share URL.) Most of these accept wildcards. (All except AllByArtist and TopByArtist... but those *might* work anyway.) Playlist names only work for playlists you follow or own, otherwise the ID is required.

LikesByGenre is the most broadly useful playlist parameter.

    Album                      All tracks on this album. Accepts album ID or names in the form "Artist Name - Album Name". Album ID performs significantly faster.
    AlbumsFromPlaylist         All tracks from all albums with any song in this playlist
    AllByArtist                All tracks by this artist. Can be intensive. Specifying artist ID instead of artist name can reduce the load somewhat. Using an artist name is very likely to include duplicate artists with the same name.
    AllByArtistFromPlaylist    All tracks by all artists in this playlist
    LikesByArtist              Liked tracks by this artist.
    LikesByArtistFromPlaylist  Liked tracks by all artists in this playlist.
    LikesByGenre               All liked tracks with the specified genre. Wildcards are very useful here.
    LikesFromPlaylist          All liked tracks in this playlist.
    PlaylistTracks             Tracks in this playlist.
    TopByArtist                The top five tracks on Spotify by this artist. Has the same performance issues as AllByArtist.
    TopByArtistFromPlaylist    The top five tracks on Spotify by every artist in this playlist.
    Track                      Single track by ID.
    -Album                     Exclude all tracks matching this album.
    -Artist                    Exclude all tracks matching this artist.
    -From                      Exclude all tracks with a matching "from" clause in the track name. This is primarily useful for soundtrack and video game covers.
    -Genre                     Exclude all tracks matching this genre.
    -PlaylistAlbums            Exclude all albums in this playlist.
    -PlaylistArtists           Exclude all artists in this playlist.
    -PlaylistTracks            Exclude all tracks in this playlist.
    -Track                     Exclude all tracks with a matching name or ID.

<!-- Auto generated -->
## Playlist Options:
Playlist options are preceded by "@" and affect the playlist as a whole.

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

<!-- Auto generated -->
## Config File Settings:
Settings that apply globally to all playlists.

    documentation pending

<!-- Auto generated -->
## Application Arguments:
Arguments for the program itself. These are not the primary function of the app but provide some nifty functionality.

    
    --playlist-folder-path <playlist-folder-path>                            playlistFolderPath
    --list-playlists                                                         listPlaylists
    --playlist-name <playlist-name>                                          playlistName
    --playlist-spec <playlist-spec>                                          playlistSpec
    --modify-playlist-file                                                   modifyPlaylistFile
    --exclude-current-artist                                                 excludeCurrentArtist
    --exclude-current-album                                                  excludeCurrentAlbum
    --exclude-current-track                                                  excludeCurrentTrack
    --add-current-artist                                                     addCurrentArtist
    --add-current-album                                                      addCurrentAlbum
    --add-current-track                                                      addCurrentTrack
    --image-add-photo                                                        imageAddPhoto
    --image-add-text                                                         imageAddText
    --image-text <image-text>                                                imageText
    --image-font <image-font>                                                imageFont
    --image-backup                                                           imageBackup
    --image-restore                                                          imageRestore
    --image-text-alignment <BottomLeft|BottomRight|Center|TopLeft|TopRight>  imageTextAlignment
    --image-rotate-degrees <image-rotate-degrees>                            imageRotateDegrees
    --image-clone <image-clone>                                              imageClone
    --image-nerd-font-glyph <image-nerd-font-glyph>                          imageNerdFontGlyph
    --test-images <test-images>                                              testImages
    --play                                                                   play
    --skip-next                                                              skipNext
    --skip-previous                                                          skipPrevious
    --like                                                                   like
    --like-id <like-id>                                                      likeId
    --unlike                                                                 unlike
    --what                                                                   what
    --what-else                                                              whatElse
    --what-id                                                                whatId
    --reports                                                                reports
    --download-album-art                                                     downloadAlbumArt
    --lyrics                                                                 lyrics
    --tab-completion-argument-names                                          tabCompletionArgumentNames
    --update-readme                                                          updateReadme
    --verbose                                                                verbose
    --version                                                                Show version information
    -?, -h, --help                                                           Show help and usage information

# Usage Examples

## Neat Playlists

Want a playlist of all your symphonic metal likes? The playlist spec for that is one line:

    LikesByGenre:symphonic metal

Or how about every liked song with "metal" in the genre? Also one line:

    LikesByGenre:*metal*

Want all of [a musician's](https://metal-archives.com/artists/Meghan_Wood/494791) side projects in one playlist? Slap the names in a playlist like this. (Note two ways of dealing with overlapping artist names.)

    @default:AllByArtist
    Crown of Asteria
    Cave Mouth
    Golden Light
    Andvari
    Enon Chapel
    Great Cold Emptiness
    6hXonF1DK45IuMCemMiyD2 # Heksebrann
    Iarnvidjur
    Tomblord
    -Artist:5FKFbl2AqE6xL5bMEkXk4y # wrong cave mouth

Find an [albums of the year list](https://forum.metal-archives.com/viewtopic.php?f=1&t=137488), but it's massive and you only want the most popular song from each album? Here you go:

    @default:Album
    @LimitPerAlbum:1
    @Sort:Don't
    @UpdateSort
    
    Blind Guardian - The God Machine
    Immolation - Acts of God
    Negative Plane - The Pact....
    Sumerlands - Dreamkiller
    Voivod - Synchro Anarchy
    White Ward - False Light
    Sigh - Shiki
    Blut Aus Nord - Disharmonium - Undreamable Abysses

## Cool Command Line Tricks

### Auto Completion With an Alias

    TODO

### Get and Translate Lyrics

    TODO

    spot --lyrics | trans -b | less

### Trim Down a Playlist
    
    TODO
    
    spot --exclude-current-artist

<!-- Auto generated -->
# Credits
- [APOD.Net](https://github.com/MarcusOtter/APOD.Net) - 1.0.1
- [colorthief.imagesharp](https://github.com/Corona-Studio/ColorThief.ImageSharp) - 1.0.0
- [colorthief.imagesharp.shared](https://github.com/Corona-Studio/ColorThief.ImageSharp) - 1.0.0
- coverlet.collector - 6.0.1
- [CsvHelper](https://github.com/JoshClose/CsvHelper) - 31.0.2
- [figgle](https://github.com/drewnoakes/figgle) - 0.5.1
- [ini-parser](https://github.com/rickyah/ini-parser) - 2.5.2
- [MediaTypeMap](https://github.com/samuelneff/MimeTypeMap) - 2.1.0
- MetaBrainz.MusicBrainz - 6.1.0
- MetaBrainz.MusicBrainz.CoverArt - 6.0.0
- microsoft.bcl.asyncinterfaces - 8.0.0
- MSTest.TestFramework - 3.2.2
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) - 13.0.3
- [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) - 3.1.3
- [SixLabors.ImageSharp.Drawing](https://github.com/SixLabors/ImageSharp.Drawing) - 2.1.2
- [SpotifyAPI.Web](https://github.com/JohnnyCrazy/SpotifyAPI-NET) - 7.1.1
- [SpotifyAPI.Web.Auth](https://github.com/JohnnyCrazy/SpotifyAPI-NET) - 7.1.1
- [SpotifyAPI.Web](https://github.com/JohnnyCrazy/SpotifyAPI-NET) - 7.0.2
- [SpotifyAPI.Web.Auth](https://github.com/JohnnyCrazy/SpotifyAPI-NET) - 7.0.2
- [System.CommandLine.DragonFruit](https://github.com/dotnet/command-line-api/blob/main/docs/Your-first-app-with-System-CommandLine-DragonFruit.md) - 0.4.0-alpha.22272.1
- [System.Drawing.Common](https://www.nuget.org/packages/System.Drawing.Common/) - 8.0.2
- [Unsplash.Net](https://github.com/unsplash-net/unsplash-net) - 1.0.0
- vadersharp2 - 3.3.2.1
- ...and you as Mega Man X!

