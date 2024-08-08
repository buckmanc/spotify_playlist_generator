<a name='assembly'></a>
# spotify_playlist_generator

## Contents

- [CustomRetryHandler](#T-spotify_playlist_generator-CustomRetryHandler 'spotify_playlist_generator.CustomRetryHandler')
  - [#ctor()](#M-spotify_playlist_generator-CustomRetryHandler-#ctor 'spotify_playlist_generator.CustomRetryHandler.#ctor')
  - [#ctor(sleep)](#M-spotify_playlist_generator-CustomRetryHandler-#ctor-System-Func{System-TimeSpan,System-Threading-Tasks-Task}- 'spotify_playlist_generator.CustomRetryHandler.#ctor(System.Func{System.TimeSpan,System.Threading.Tasks.Task})')
  - [RetryAfter](#P-spotify_playlist_generator-CustomRetryHandler-RetryAfter 'spotify_playlist_generator.CustomRetryHandler.RetryAfter')
  - [RetryErrorCodes](#P-spotify_playlist_generator-CustomRetryHandler-RetryErrorCodes 'spotify_playlist_generator.CustomRetryHandler.RetryErrorCodes')
  - [RetryTimes](#P-spotify_playlist_generator-CustomRetryHandler-RetryTimes 'spotify_playlist_generator.CustomRetryHandler.RetryTimes')
  - [TooManyRequestsConsumesARetry](#P-spotify_playlist_generator-CustomRetryHandler-TooManyRequestsConsumesARetry 'spotify_playlist_generator.CustomRetryHandler.TooManyRequestsConsumesARetry')
  - [HandleRetry(request,response,retry,cancellationToken)](#M-spotify_playlist_generator-CustomRetryHandler-HandleRetry-SpotifyAPI-Web-Http-IRequest,SpotifyAPI-Web-Http-IResponse,SpotifyAPI-Web-Http-IRetryHandler-RetryFunc,System-Threading-CancellationToken- 'spotify_playlist_generator.CustomRetryHandler.HandleRetry(SpotifyAPI.Web.Http.IRequest,SpotifyAPI.Web.Http.IResponse,SpotifyAPI.Web.Http.IRetryHandler.RetryFunc,System.Threading.CancellationToken)')
- [ExtensionMethods](#T-spotify_playlist_generator-ExtensionMethods 'spotify_playlist_generator.ExtensionMethods')
  - [AddRange\`\`2(dictionaryTo,dictionaryFrom)](#M-spotify_playlist_generator-ExtensionMethods-AddRange``2-System-Collections-Generic-IDictionary{``0,``1},System-Collections-Generic-IDictionary{``0,``1}- 'spotify_playlist_generator.ExtensionMethods.AddRange``2(System.Collections.Generic.IDictionary{``0,``1},System.Collections.Generic.IDictionary{``0,``1})')
  - [ChunkBy\`\`1(source,chunkSize)](#M-spotify_playlist_generator-ExtensionMethods-ChunkBy``1-System-Collections-Generic-IEnumerable{``0},System-Int32- 'spotify_playlist_generator.ExtensionMethods.ChunkBy``1(System.Collections.Generic.IEnumerable{``0},System.Int32)')
  - [CountOccurrences(value,search)](#M-spotify_playlist_generator-ExtensionMethods-CountOccurrences-System-String,System-String- 'spotify_playlist_generator.ExtensionMethods.CountOccurrences(System.String,System.String)')
  - [Join(value,separator)](#M-spotify_playlist_generator-ExtensionMethods-Join-System-Collections-Generic-IEnumerable{System-String},System-String- 'spotify_playlist_generator.ExtensionMethods.Join(System.Collections.Generic.IEnumerable{System.String},System.String)')
  - [Join(value)](#M-spotify_playlist_generator-ExtensionMethods-Join-System-Collections-Generic-IEnumerable{System-String}- 'spotify_playlist_generator.ExtensionMethods.Join(System.Collections.Generic.IEnumerable{System.String})')
  - [Like(str,pattern)](#M-spotify_playlist_generator-ExtensionMethods-Like-System-String,System-String,System-Boolean- 'spotify_playlist_generator.ExtensionMethods.Like(System.String,System.String,System.Boolean)')
  - [RemoveRange\`\`1(values,collection)](#M-spotify_playlist_generator-ExtensionMethods-RemoveRange``1-System-Collections-Generic-List{``0},System-Collections-Generic-IEnumerable{``0}- 'spotify_playlist_generator.ExtensionMethods.RemoveRange``1(System.Collections.Generic.List{``0},System.Collections.Generic.IEnumerable{``0})')
  - [ToListAsync\`\`1(values,Take)](#M-spotify_playlist_generator-ExtensionMethods-ToListAsync``1-System-Collections-Generic-IAsyncEnumerable{``0},System-Int32- 'spotify_playlist_generator.ExtensionMethods.ToListAsync``1(System.Collections.Generic.IAsyncEnumerable{``0},System.Int32)')
  - [Trim(value,trimString,comparisonType)](#M-spotify_playlist_generator-ExtensionMethods-Trim-System-String,System-String,System-StringComparison- 'spotify_playlist_generator.ExtensionMethods.Trim(System.String,System.String,System.StringComparison)')
  - [TrimEnd(value,trimString,comparisonType)](#M-spotify_playlist_generator-ExtensionMethods-TrimEnd-System-String,System-String,System-StringComparison- 'spotify_playlist_generator.ExtensionMethods.TrimEnd(System.String,System.String,System.StringComparison)')
  - [TrimStart(value,trimString,comparisonType)](#M-spotify_playlist_generator-ExtensionMethods-TrimStart-System-String,System-String,System-StringComparison- 'spotify_playlist_generator.ExtensionMethods.TrimStart(System.String,System.String,System.StringComparison)')
- [FullPlaylistDetails](#T-spotify_playlist_generator-Models-FullPlaylistDetails 'spotify_playlist_generator.Models.FullPlaylistDetails')
  - [#ctor()](#M-spotify_playlist_generator-Models-FullPlaylistDetails-#ctor 'spotify_playlist_generator.Models.FullPlaylistDetails.#ctor')
- [FullTrackDetails](#T-spotify_playlist_generator-Models-FullTrackDetails 'spotify_playlist_generator.Models.FullTrackDetails')
  - [#ctor(fullTrack,fullArtists,topTrack,allTracksTrack,sessionID)](#M-spotify_playlist_generator-Models-FullTrackDetails-#ctor-SpotifyAPI-Web-FullTrack,System-Collections-Generic-IEnumerable{SpotifyAPI-Web-FullArtist},System-Guid,System-Boolean,System-Boolean- 'spotify_playlist_generator.Models.FullTrackDetails.#ctor(SpotifyAPI.Web.FullTrack,System.Collections.Generic.IEnumerable{SpotifyAPI.Web.FullArtist},System.Guid,System.Boolean,System.Boolean)')
  - [#ctor(savedTrack,fullArtists,sessionID)](#M-spotify_playlist_generator-Models-FullTrackDetails-#ctor-SpotifyAPI-Web-SavedTrack,System-Collections-Generic-IEnumerable{SpotifyAPI-Web-FullArtist},System-Guid- 'spotify_playlist_generator.Models.FullTrackDetails.#ctor(SpotifyAPI.Web.SavedTrack,System.Collections.Generic.IEnumerable{SpotifyAPI.Web.FullArtist},System.Guid)')
  - [#ctor()](#M-spotify_playlist_generator-Models-FullTrackDetails-#ctor 'spotify_playlist_generator.Models.FullTrackDetails.#ctor')
- [MySpotifyWrapper](#T-spotify_playlist_generator-MySpotifyWrapper 'spotify_playlist_generator.MySpotifyWrapper')
  - [GetArtists(artistIDs)](#M-spotify_playlist_generator-MySpotifyWrapper-GetArtists-System-Collections-Generic-IEnumerable{System-String}- 'spotify_playlist_generator.MySpotifyWrapper.GetArtists(System.Collections.Generic.IEnumerable{System.String})')
  - [GetTracks(trackIDs,Source_AllTracks)](#M-spotify_playlist_generator-MySpotifyWrapper-GetTracks-System-Collections-Generic-IEnumerable{System-String},System-Boolean- 'spotify_playlist_generator.MySpotifyWrapper.GetTracks(System.Collections.Generic.IEnumerable{System.String},System.Boolean)')
- [Program](#T-spotify_playlist_generator-Program 'spotify_playlist_generator.Program')
  - [Main(playlistFolderPath,listPlaylists,playlistName,playlistSpec,modifyPlaylistFile,excludeCurrentArtist,excludeCurrentAlbum,excludeCurrentTrack,addCurrentArtist,addCurrentAlbum,addCurrentTrack,imageAddPhoto,imageAddText,imageText,imageFont,imageBackup,imageRestore,imageTextAlignment,imageRotateDegrees,imageClone,imageNerdFontGlyph,testImages,play,skipNext,skipPrevious,like,unlike,what,whatElse,reports,lyrics,tabCompletionArgumentNames,updateReadme,verbose,commitAnActOfUnspeakableViolence)](#M-spotify_playlist_generator-Program-Main-System-String,System-Boolean,System-String,System-String,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-String,System-String,System-Boolean,System-Boolean,spotify_playlist_generator-Program-TextAlignment,System-Int32,System-String,System-String,System-Int32,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-String,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Nullable{System-Boolean},System-Boolean- 'spotify_playlist_generator.Program.Main(System.String,System.Boolean,System.String,System.String,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.String,System.String,System.Boolean,System.Boolean,spotify_playlist_generator.Program.TextAlignment,System.Int32,System.String,System.String,System.Int32,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.String,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Nullable{System.Boolean},System.Boolean)')
- [ProgressPrinter](#T-spotify_playlist_generator-ProgressPrinter 'spotify_playlist_generator.ProgressPrinter')
  - [#ctor(Total,Update)](#M-spotify_playlist_generator-ProgressPrinter-#ctor-System-Int32,System-Action{System-String,System-String}- 'spotify_playlist_generator.ProgressPrinter.#ctor(System.Int32,System.Action{System.String,System.String})')
  - [#ctor(Total,Update)](#M-spotify_playlist_generator-ProgressPrinter-#ctor-System-Int32,System-Action{System-String}- 'spotify_playlist_generator.ProgressPrinter.#ctor(System.Int32,System.Action{System.String})')
  - [PrintProgress()](#M-spotify_playlist_generator-ProgressPrinter-PrintProgress 'spotify_playlist_generator.ProgressPrinter.PrintProgress')
- [Retry](#T-spotify_playlist_generator-Retry 'spotify_playlist_generator.Retry')
  - [Do(action,retryIntervalMilliseconds,maxAttemptCount)](#M-spotify_playlist_generator-Retry-Do-System-Action,System-Int32,System-Int32- 'spotify_playlist_generator.Retry.Do(System.Action,System.Int32,System.Int32)')
  - [Do(action,retryIntervalMilliseconds,maxAttemptCount)](#M-spotify_playlist_generator-Retry-Do-System-Action{System-Exception},System-Int32,System-Int32- 'spotify_playlist_generator.Retry.Do(System.Action{System.Exception},System.Int32,System.Int32)')
  - [Do(action,retryIntervalMilliseconds,maxAttemptCount)](#M-spotify_playlist_generator-Retry-Do-System-Action{System-Int32},System-Int32,System-Int32- 'spotify_playlist_generator.Retry.Do(System.Action{System.Int32},System.Int32,System.Int32)')
  - [Do(action,retryIntervalMilliseconds,maxAttemptCount)](#M-spotify_playlist_generator-Retry-Do-System-Action{System-Int32,System-Exception},System-Int32,System-Int32- 'spotify_playlist_generator.Retry.Do(System.Action{System.Int32,System.Exception},System.Int32,System.Int32)')
  - [Do\`\`1(action,retryIntervalMilliseconds,maxAttemptCount)](#M-spotify_playlist_generator-Retry-Do``1-System-Func{``0},System-Int32,System-Int32- 'spotify_playlist_generator.Retry.Do``1(System.Func{``0},System.Int32,System.Int32)')
  - [Do\`\`1(action,retryIntervalMilliseconds,maxAttemptCount)](#M-spotify_playlist_generator-Retry-Do``1-System-Func{System-Exception,``0},System-Int32,System-Int32- 'spotify_playlist_generator.Retry.Do``1(System.Func{System.Exception,``0},System.Int32,System.Int32)')
  - [Do\`\`1(action,retryIntervalMilliseconds,maxAttemptCount)](#M-spotify_playlist_generator-Retry-Do``1-System-Func{System-Int32,``0},System-Int32,System-Int32- 'spotify_playlist_generator.Retry.Do``1(System.Func{System.Int32,``0},System.Int32,System.Int32)')
  - [Do\`\`1(action,retryIntervalMilliseconds,maxAttemptCount)](#M-spotify_playlist_generator-Retry-Do``1-System-Func{System-Int32,System-Exception,``0},System-Int32,System-Int32- 'spotify_playlist_generator.Retry.Do``1(System.Func{System.Int32,System.Exception,``0},System.Int32,System.Int32)')

<a name='T-spotify_playlist_generator-CustomRetryHandler'></a>
## CustomRetryHandler `type`

##### Namespace

spotify_playlist_generator

##### Summary

copied from the API library for slightly more control
https://github.com/JohnnyCrazy/SpotifyAPI-NET/blob/master/SpotifyAPI.Web/RetryHandlers/SimpleRetryHandler.cs

<a name='M-spotify_playlist_generator-CustomRetryHandler-#ctor'></a>
### #ctor() `constructor`

##### Summary

A simple retry handler which retries a request based on status codes with a fixed sleep interval.
  It also supports Retry-After headers sent by spotify. The execution will be delayed by the amount in
  the Retry-After header

##### Returns



##### Parameters

This constructor has no parameters.

<a name='M-spotify_playlist_generator-CustomRetryHandler-#ctor-System-Func{System-TimeSpan,System-Threading-Tasks-Task}-'></a>
### #ctor(sleep) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| sleep | [System.Func{System.TimeSpan,System.Threading.Tasks.Task}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Func 'System.Func{System.TimeSpan,System.Threading.Tasks.Task}') |  |

<a name='P-spotify_playlist_generator-CustomRetryHandler-RetryAfter'></a>
### RetryAfter `property`

##### Summary

Specifies after how many milliseconds should a failed request be retried.

<a name='P-spotify_playlist_generator-CustomRetryHandler-RetryErrorCodes'></a>
### RetryErrorCodes `property`

##### Summary

Error codes that will trigger auto-retry

<a name='P-spotify_playlist_generator-CustomRetryHandler-RetryTimes'></a>
### RetryTimes `property`

##### Summary

Maximum number of tries for one failed request.

<a name='P-spotify_playlist_generator-CustomRetryHandler-TooManyRequestsConsumesARetry'></a>
### TooManyRequestsConsumesARetry `property`

##### Summary

Whether a failure of type "Too Many Requests" should use up one of the allocated retry attempts.

<a name='M-spotify_playlist_generator-CustomRetryHandler-HandleRetry-SpotifyAPI-Web-Http-IRequest,SpotifyAPI-Web-Http-IResponse,SpotifyAPI-Web-Http-IRetryHandler-RetryFunc,System-Threading-CancellationToken-'></a>
### HandleRetry(request,response,retry,cancellationToken) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| request | [SpotifyAPI.Web.Http.IRequest](#T-SpotifyAPI-Web-Http-IRequest 'SpotifyAPI.Web.Http.IRequest') |  |
| response | [SpotifyAPI.Web.Http.IResponse](#T-SpotifyAPI-Web-Http-IResponse 'SpotifyAPI.Web.Http.IResponse') |  |
| retry | [SpotifyAPI.Web.Http.IRetryHandler.RetryFunc](#T-SpotifyAPI-Web-Http-IRetryHandler-RetryFunc 'SpotifyAPI.Web.Http.IRetryHandler.RetryFunc') |  |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') |  |

<a name='T-spotify_playlist_generator-ExtensionMethods'></a>
## ExtensionMethods `type`

##### Namespace

spotify_playlist_generator

<a name='M-spotify_playlist_generator-ExtensionMethods-AddRange``2-System-Collections-Generic-IDictionary{``0,``1},System-Collections-Generic-IDictionary{``0,``1}-'></a>
### AddRange\`\`2(dictionaryTo,dictionaryFrom) `method`

##### Summary

Add elements from one dictionary to another.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| dictionaryTo | [System.Collections.Generic.IDictionary{\`\`0,\`\`1}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IDictionary 'System.Collections.Generic.IDictionary{``0,``1}') |  |
| dictionaryFrom | [System.Collections.Generic.IDictionary{\`\`0,\`\`1}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IDictionary 'System.Collections.Generic.IDictionary{``0,``1}') |  |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TKey |  |
| TValue |  |

<a name='M-spotify_playlist_generator-ExtensionMethods-ChunkBy``1-System-Collections-Generic-IEnumerable{``0},System-Int32-'></a>
### ChunkBy\`\`1(source,chunkSize) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| source | [System.Collections.Generic.IEnumerable{\`\`0}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{``0}') |  |
| chunkSize | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T |  |

<a name='M-spotify_playlist_generator-ExtensionMethods-CountOccurrences-System-String,System-String-'></a>
### CountOccurrences(value,search) `method`

##### Summary

Count how often one string occurs in another.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| value | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| search | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |

<a name='M-spotify_playlist_generator-ExtensionMethods-Join-System-Collections-Generic-IEnumerable{System-String},System-String-'></a>
### Join(value,separator) `method`

##### Summary

Returns a string of the contained elements joined with the specified separator.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| value | [System.Collections.Generic.IEnumerable{System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.String}') |  |
| separator | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |

<a name='M-spotify_playlist_generator-ExtensionMethods-Join-System-Collections-Generic-IEnumerable{System-String}-'></a>
### Join(value) `method`

##### Summary

Returns a string of the contained elements joined.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| value | [System.Collections.Generic.IEnumerable{System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.String}') |  |

<a name='M-spotify_playlist_generator-ExtensionMethods-Like-System-String,System-String,System-Boolean-'></a>
### Like(str,pattern) `method`

##### Summary

Compares the string against a given pattern.

##### Returns

`true` if the string matches the given pattern; otherwise `false`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| str | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The string. |
| pattern | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The pattern to match, where "*" means any sequence of characters, and "?" means any single character. |

<a name='M-spotify_playlist_generator-ExtensionMethods-RemoveRange``1-System-Collections-Generic-List{``0},System-Collections-Generic-IEnumerable{``0}-'></a>
### RemoveRange\`\`1(values,collection) `method`

##### Summary

Removes the elements in the collection from the List<T>.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| values | [System.Collections.Generic.List{\`\`0}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.List 'System.Collections.Generic.List{``0}') |  |
| collection | [System.Collections.Generic.IEnumerable{\`\`0}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{``0}') |  |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T |  |

<a name='M-spotify_playlist_generator-ExtensionMethods-ToListAsync``1-System-Collections-Generic-IAsyncEnumerable{``0},System-Int32-'></a>
### ToListAsync\`\`1(values,Take) `method`

##### Summary

Forces iteration of an IAsyncEnumerable; a shortcut against a manual for each.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| values | [System.Collections.Generic.IAsyncEnumerable{\`\`0}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IAsyncEnumerable 'System.Collections.Generic.IAsyncEnumerable{``0}') |  |
| Take | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T |  |

<a name='M-spotify_playlist_generator-ExtensionMethods-Trim-System-String,System-String,System-StringComparison-'></a>
### Trim(value,trimString,comparisonType) `method`

##### Summary

Removes the specified string from the beginning of a string.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| value | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| trimString | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| comparisonType | [System.StringComparison](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.StringComparison 'System.StringComparison') |  |

<a name='M-spotify_playlist_generator-ExtensionMethods-TrimEnd-System-String,System-String,System-StringComparison-'></a>
### TrimEnd(value,trimString,comparisonType) `method`

##### Summary

Removes the specified string from the beginning of a string.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| value | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| trimString | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| comparisonType | [System.StringComparison](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.StringComparison 'System.StringComparison') |  |

<a name='M-spotify_playlist_generator-ExtensionMethods-TrimStart-System-String,System-String,System-StringComparison-'></a>
### TrimStart(value,trimString,comparisonType) `method`

##### Summary

Removes the specified string from the beginning of a string.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| value | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| trimString | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| comparisonType | [System.StringComparison](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.StringComparison 'System.StringComparison') |  |

<a name='T-spotify_playlist_generator-Models-FullPlaylistDetails'></a>
## FullPlaylistDetails `type`

##### Namespace

spotify_playlist_generator.Models

<a name='M-spotify_playlist_generator-Models-FullPlaylistDetails-#ctor'></a>
### #ctor() `constructor`

##### Summary

Don't use this constructor. This is only here for the JSON deserializer, not for you.

##### Parameters

This constructor has no parameters.

<a name='T-spotify_playlist_generator-Models-FullTrackDetails'></a>
## FullTrackDetails `type`

##### Namespace

spotify_playlist_generator.Models

<a name='M-spotify_playlist_generator-Models-FullTrackDetails-#ctor-SpotifyAPI-Web-FullTrack,System-Collections-Generic-IEnumerable{SpotifyAPI-Web-FullArtist},System-Guid,System-Boolean,System-Boolean-'></a>
### #ctor(fullTrack,fullArtists,topTrack,allTracksTrack,sessionID) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| fullTrack | [SpotifyAPI.Web.FullTrack](#T-SpotifyAPI-Web-FullTrack 'SpotifyAPI.Web.FullTrack') |  |
| fullArtists | [System.Collections.Generic.IEnumerable{SpotifyAPI.Web.FullArtist}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{SpotifyAPI.Web.FullArtist}') | Artists tied to this track. Can include extra artists without issue. |
| topTrack | [System.Guid](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Guid 'System.Guid') |  |
| allTracksTrack | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |
| sessionID | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |

<a name='M-spotify_playlist_generator-Models-FullTrackDetails-#ctor-SpotifyAPI-Web-SavedTrack,System-Collections-Generic-IEnumerable{SpotifyAPI-Web-FullArtist},System-Guid-'></a>
### #ctor(savedTrack,fullArtists,sessionID) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| savedTrack | [SpotifyAPI.Web.SavedTrack](#T-SpotifyAPI-Web-SavedTrack 'SpotifyAPI.Web.SavedTrack') |  |
| fullArtists | [System.Collections.Generic.IEnumerable{SpotifyAPI.Web.FullArtist}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{SpotifyAPI.Web.FullArtist}') | Artists tied to this track. Can include extra artists without issue. |
| sessionID | [System.Guid](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Guid 'System.Guid') |  |

<a name='M-spotify_playlist_generator-Models-FullTrackDetails-#ctor'></a>
### #ctor() `constructor`

##### Summary

Don't use this constructor. This is only here for the JSON deserializer, not for you.

##### Parameters

This constructor has no parameters.

<a name='T-spotify_playlist_generator-MySpotifyWrapper'></a>
## MySpotifyWrapper `type`

##### Namespace

spotify_playlist_generator

<a name='M-spotify_playlist_generator-MySpotifyWrapper-GetArtists-System-Collections-Generic-IEnumerable{System-String}-'></a>
### GetArtists(artistIDs) `method`

##### Summary

Returns artists from the local cache or the Spotify API.

##### Returns

Returns a list of FullArtist

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| artistIDs | [System.Collections.Generic.IEnumerable{System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.String}') |  |

<a name='M-spotify_playlist_generator-MySpotifyWrapper-GetTracks-System-Collections-Generic-IEnumerable{System-String},System-Boolean-'></a>
### GetTracks(trackIDs,Source_AllTracks) `method`

##### Summary

Returns tracks from the local cache or the Spotify API.

##### Returns

Returns a list of FullTrack

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| trackIDs | [System.Collections.Generic.IEnumerable{System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.String}') |  |
| Source_AllTracks | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |

<a name='T-spotify_playlist_generator-Program'></a>
## Program `type`

##### Namespace

spotify_playlist_generator

<a name='M-spotify_playlist_generator-Program-Main-System-String,System-Boolean,System-String,System-String,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-String,System-String,System-Boolean,System-Boolean,spotify_playlist_generator-Program-TextAlignment,System-Int32,System-String,System-String,System-Int32,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-String,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Boolean,System-Nullable{System-Boolean},System-Boolean-'></a>
### Main(playlistFolderPath,listPlaylists,playlistName,playlistSpec,modifyPlaylistFile,excludeCurrentArtist,excludeCurrentAlbum,excludeCurrentTrack,addCurrentArtist,addCurrentAlbum,addCurrentTrack,imageAddPhoto,imageAddText,imageText,imageFont,imageBackup,imageRestore,imageTextAlignment,imageRotateDegrees,imageClone,imageNerdFontGlyph,testImages,play,skipNext,skipPrevious,like,unlike,what,whatElse,reports,lyrics,tabCompletionArgumentNames,updateReadme,verbose,commitAnActOfUnspeakableViolence) `method`

##### Summary

A file based way to add smart playlists to Spotify.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| playlistFolderPath | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | An alternate path for the playlists folder path. Overrides the value found in paths.ini. |
| listPlaylists | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | List existing playlists from the playlists folder. |
| playlistName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of the playlist to run alone, unless combined with --playlist-specs. Supports wildcards. |
| playlistSpec | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | A playlist specification string for use when creating a new playlist from the command line. |
| modifyPlaylistFile | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Exchange artist names for artist IDs. Saves time when running but looks worse. |
| excludeCurrentArtist | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Adds an exclusion line for the currenly playing artist into the playlist. If no --playlist-name is specified the current playlist is used. Intended for refining playlists. |
| excludeCurrentAlbum | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Adds an exclusion line for the currenly playing album into the playlist. If no --playlist-name is specified the current playlist is used. Intended for refining playlists. |
| excludeCurrentTrack | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Adds an exclusion line for the currenly playing track into the playlist. If no --playlist-name is specified the current playlist is used. Intended for refining playlists. |
| addCurrentArtist | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Adds a line for the currenly playing artist into the playlist. If no --playlist-name is specified the current playlist is used. Intended for refining playlists. |
| addCurrentAlbum | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Adds a line for the currenly playing album into the playlist. If no --playlist-name is specified the current playlist is used. Intended for refining playlists. |
| addCurrentTrack | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Adds a line for the currenly playing track into the playlist. If no --playlist-name is specified the current playlist is used. Intended for refining playlists. |
| imageAddPhoto | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Assign a new image to the playlist. |
| imageAddText | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Add text to the playlist image. The playlist name is used if no --image-text is provided. |
| imageText | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Custom text to use for --image-add-text. |
| imageFont | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Font to use for --image-add-text. Can use wildcards. |
| imageBackup | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Happens automatically whenever modifying an image. Calling --image-backup directly overwrites previous backups. |
| imageRestore | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Restore a previously backed up image. |
| imageTextAlignment | [spotify_playlist_generator.Program.TextAlignment](#T-spotify_playlist_generator-Program-TextAlignment 'spotify_playlist_generator.Program.TextAlignment') | Pick one of the corners or center. Defaults to bottom left. |
| imageRotateDegrees | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | Rotate the image. You should really stick to 90 degree increments. |
| imageClone | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Clone the cover art from another of your playlists. |
| imageNerdFontGlyph | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Wildcard search for a Nerd Font glyph. Returns the symbol when used alone or used as the text with --image-add-text. You still have to specify an installed Nerd Font with --image-font. |
| testImages | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | Generate x test images instead of updating live playlists. Stored in the playlist folder. |
| play | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Play --playlist-name. If no playlist is provided, toggle playback. Can be used with --playlist-spec to build a new playlist and play it afterward. |
| skipNext | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Skip forward. |
| skipPrevious | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Skip backward. |
| like | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Like the current track. |
| unlike | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Unlike the current track. |
| what | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Print details about the current track. |
| whatElse | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Print more details about the current track. |
| reports | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Run only the reports. |
| lyrics | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Pass currently playing info to an external lyrics app specified in the config file. |
| tabCompletionArgumentNames | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | A space delimited list of these arguments to pass to the bash "complete" function. |
| updateReadme | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Update readme.md. Only used in development. |
| verbose | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Print more logging messages. Pretty messy. Overrides the settings file. |
| commitAnActOfUnspeakableViolence | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | I wouldn't really do it... would I? |

<a name='T-spotify_playlist_generator-ProgressPrinter'></a>
## ProgressPrinter `type`

##### Namespace

spotify_playlist_generator

##### Summary

A class for reporting progress to the console.

<a name='M-spotify_playlist_generator-ProgressPrinter-#ctor-System-Int32,System-Action{System-String,System-String}-'></a>
### #ctor(Total,Update) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Total | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |
| Update | [System.Action{System.String,System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Action 'System.Action{System.String,System.String}') |  |

<a name='M-spotify_playlist_generator-ProgressPrinter-#ctor-System-Int32,System-Action{System-String}-'></a>
### #ctor(Total,Update) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Total | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |
| Update | [System.Action{System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Action 'System.Action{System.String}') |  |

<a name='M-spotify_playlist_generator-ProgressPrinter-PrintProgress'></a>
### PrintProgress() `method`

##### Summary



##### Returns



##### Parameters

This method has no parameters.

<a name='T-spotify_playlist_generator-Retry'></a>
## Retry `type`

##### Namespace

spotify_playlist_generator

##### Summary

A class to facilitate multiple attempts on API calls

<a name='M-spotify_playlist_generator-Retry-Do-System-Action,System-Int32,System-Int32-'></a>
### Do(action,retryIntervalMilliseconds,maxAttemptCount) `method`

##### Summary

report on nothing, return nothing

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| action | [System.Action](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Action 'System.Action') |  |
| retryIntervalMilliseconds | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |
| maxAttemptCount | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |

<a name='M-spotify_playlist_generator-Retry-Do-System-Action{System-Exception},System-Int32,System-Int32-'></a>
### Do(action,retryIntervalMilliseconds,maxAttemptCount) `method`

##### Summary

report on exceptions, return nothing

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| action | [System.Action{System.Exception}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Action 'System.Action{System.Exception}') |  |
| retryIntervalMilliseconds | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |
| maxAttemptCount | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |

<a name='M-spotify_playlist_generator-Retry-Do-System-Action{System-Int32},System-Int32,System-Int32-'></a>
### Do(action,retryIntervalMilliseconds,maxAttemptCount) `method`

##### Summary

report on attempts, return nothing

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| action | [System.Action{System.Int32}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Action 'System.Action{System.Int32}') |  |
| retryIntervalMilliseconds | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |
| maxAttemptCount | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |

<a name='M-spotify_playlist_generator-Retry-Do-System-Action{System-Int32,System-Exception},System-Int32,System-Int32-'></a>
### Do(action,retryIntervalMilliseconds,maxAttemptCount) `method`

##### Summary

report on attempts and exceptions, return nothing

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| action | [System.Action{System.Int32,System.Exception}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Action 'System.Action{System.Int32,System.Exception}') |  |
| retryIntervalMilliseconds | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |
| maxAttemptCount | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |

<a name='M-spotify_playlist_generator-Retry-Do``1-System-Func{``0},System-Int32,System-Int32-'></a>
### Do\`\`1(action,retryIntervalMilliseconds,maxAttemptCount) `method`

##### Summary

report on nothing, return T

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| action | [System.Func{\`\`0}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Func 'System.Func{``0}') |  |
| retryIntervalMilliseconds | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |
| maxAttemptCount | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T |  |

<a name='M-spotify_playlist_generator-Retry-Do``1-System-Func{System-Exception,``0},System-Int32,System-Int32-'></a>
### Do\`\`1(action,retryIntervalMilliseconds,maxAttemptCount) `method`

##### Summary

report on exceptions, return T

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| action | [System.Func{System.Exception,\`\`0}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Func 'System.Func{System.Exception,``0}') |  |
| retryIntervalMilliseconds | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |
| maxAttemptCount | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T |  |

<a name='M-spotify_playlist_generator-Retry-Do``1-System-Func{System-Int32,``0},System-Int32,System-Int32-'></a>
### Do\`\`1(action,retryIntervalMilliseconds,maxAttemptCount) `method`

##### Summary

report on attempts, return T

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| action | [System.Func{System.Int32,\`\`0}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Func 'System.Func{System.Int32,``0}') |  |
| retryIntervalMilliseconds | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |
| maxAttemptCount | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T |  |

<a name='M-spotify_playlist_generator-Retry-Do``1-System-Func{System-Int32,System-Exception,``0},System-Int32,System-Int32-'></a>
### Do\`\`1(action,retryIntervalMilliseconds,maxAttemptCount) `method`

##### Summary

report on attempts and exceptions, return T

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| action | [System.Func{System.Int32,System.Exception,\`\`0}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Func 'System.Func{System.Int32,System.Exception,``0}') |  |
| retryIntervalMilliseconds | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |
| maxAttemptCount | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') |  |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T |  |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.AggregateException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.AggregateException 'System.AggregateException') |  |
