[title text]
=========

When I finally caved and switched from iTunes to Spotify there was a smart playlist shaped hole in my heart. And thus [app name] was born.

Maybe you don't have a box to schedule it on. Maybe you don't want to get [Spotify API keys](https://developer.spotify.com/dashboard/). Maybe you have a nerdy friend who could schedule it for you and share a Dropbox folder for your playlist specs. iykyk

This is a personal project and corners have been cut strategically in order to keep things moving within my time constraints. Git commits are less than atomic, file structure isn't the best, etc etc. However, it's been the Alfred of my playlists for a few years now and it isn't likely to retire anytime soon.

=========

* [Installation](#installation)
* [USAGE AND OPTIONS](#usage-and-options)
    * [Playlist Parameters](#playlist-parameters)
    * [Playlist Options](#playlist-options)
    * [Application Arguments](#application-arguments)
	
# Installation
## Option 1 - make like Dolly
0. this is just a draft I'll fix up these steps later
1. install [dotnet](https://dotnet.microsoft.com/en-us/download/dotnet/scripts)
2. get your [Spotify API keys](https://developer.spotify.com/dashboard/)
3. git clone

        git clone https://github.com/buckmanc/spotify_playlist_generator.git

4. add your API keys to the config file
5. run once and do the login process
6. move the playlist folder or keep it the default (put it in your Dropbox and thank me later)
7. make some playlists!
## Option 2 - get a friend to run it for you
1. call up your nerd
2. share a Dropbox folder with them
3. profit


# Options

<!-- Auto generated -->
## Playlist Parameters:
Playlist parameters tell the app what songs to add to the playlist.

[playlist parameters]

<!-- Auto generated -->
## Playlist Options:
Playlist options are preceded by "@" and affect the playlist as a whole.

[playlist options]

<!-- Auto generated -->
## Application Arguments:
[argument help]

# Usage Examples

## Neat Playlists

Want a playlist of all your symphonic metal likes? The playlist spec for that is one line:

    LikesByGenre:symphonic metal

Want all of [a musician's](https://metal-archives.com/artists/Meghan_Wood/494791) side projects in one playlist?

    @default:AllByArtist
    Crown of Asteria
    etc

Find an [albums of the year list](https://forum.metal-archives.com/viewtopic.php?f=1&t=137488), but it's massive and you only want the most popular song from each album? Here you go

    @default:Album
    @LimitPerAlbum:1
    @Sort:File # TODO sync code to this

## Cool Command Line Tricks

### Auto Completion With an Alias

    TODO

### Get and Translate Lyrics

    spot --lyrics | trans -b | less

### Trim Down a Playlist

<!-- Auto generated -->
# Credits
[packages]
