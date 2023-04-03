[title text]
=========

When I finally caved and switched from iTunes to Spotify there was a smart playlist shaped hole in my heart. And thus [app name] was born.

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

[playlist parameters]

<!-- Auto generated -->
## Playlist Options:
Playlist options are preceded by "@" and affect the playlist as a whole.

[playlist options]

<!-- Auto generated -->
## Config File Settings:
Settings that apply globally to all playlists.

[config settings]

<!-- Auto generated -->
## Application Arguments:
Arguments for the program itself. These are not the primary function of the app but provide some nifty functionality.

[argument help]

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
[packages]
