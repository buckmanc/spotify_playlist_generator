
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace spotify_playlist_generator_tests_redux
{
    [TestClass]
    public class UnitTest1
    {
        [Timeout(TestTimeout.Infinite)]
        [TestMethod]
        public void GetTracksByPlaylistTest()
        {
            Console.WriteLine("---starting test---");
            spotify_playlist_generator.Program.GetConfig();
            spotify_playlist_generator.Program.Settings._VerboseDebug = false;
            using var spotifyWrapper = new MySpotifyWrapper();

            var output = spotifyWrapper.GetTracksByPlaylist(new string[] { "piano video game covers" });

            Assert.IsTrue(output.Any());
        }

        [Timeout(TestTimeout.Infinite)]
        [TestMethod]
        public void TestMethod1()
        {
            Console.WriteLine("---starting test---");
            spotify_playlist_generator.Program.GetConfig();
            spotify_playlist_generator.Program.Settings._VerboseDebug = false;
            using var spotifyWrapper = new MySpotifyWrapper();

            var testParamValues = new Dictionary<ObjectType, List<string>>();
            testParamValues.Add(ObjectType.Playlist, new List<string>() { "liked - froglord", "*exvangelical*" });
            testParamValues.Add(ObjectType.Album, new List<string>() { "froglord - the mystic toad", "bridge city sinners - here's to the devil" });
            testParamValues.Add(ObjectType.Artist, new List<string>() { "froglord", "acryl madness"});
            testParamValues.Add(ObjectType.Genre, new List<string>() { "electro swing", "deathcore", "atmospheric black metal" });
            // testParamValues.Add(ObjectType.Track, new List<string>() { "https://open.spotify.com/track/1epoUrb1D12CNVM9uF53O6?si=1x-3aTMCTauuO2NzpjerKw", "2EHRRkUaAX7tOczzRxI898" });
            testParamValues.Add(ObjectType.Track, new List<string>() { "1epoUrb1D12CNVM9uF53O6", "3mDNCJEbFquGVsM1leQfmT" });

            try
            {
                var likedTracks = spotifyWrapper.LikedTracks;

                Assert.IsTrue(likedTracks.Any());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail();
            }

            var parameters = PlaylistParameterDefinition
                .AllDefinitions
                .OrderBy(def => def.ParameterName.Length)
                .ToArray();

            foreach (var def in parameters)
            {

                if (def.IsExclusion && def.GetObjectType() == ObjectType.None)
                    continue;

                foreach (var paramValue in testParamValues[def.GetObjectType()])
                {

                    Console.Write("testing " + def.ParameterName + " with " + paramValue + "...");

                    var results = def.GetTracks(
                            spotifyWrapper,
                        parameterValues: new List<string>() { paramValue },
                        likedTracks: spotifyWrapper.LikedTracks,
                        existingTracks: spotifyWrapper.LikedTracks
                        // exceptArtists: (def.ParameterName.Like("*FromPlaylist") ? "Froglord" : null)
                        );

                    //var results = new List<string>();

                    if (results.Any())
                        Console.WriteLine("success!");
                    else
                        Console.WriteLine("failed!");

                    Assert.IsTrue(results.Any());
                }
            }

            Assert.IsTrue(true);
        }
    }
}
