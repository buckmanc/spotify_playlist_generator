rm ../spot_test_output -r
dotnet publish spotify_playlist_generator.csproj --output /media/content/Coding/spot_test_output/ --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained
ls -hs ../spot_test_output
