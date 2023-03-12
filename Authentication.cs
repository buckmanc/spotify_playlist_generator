using System;
using SpotifyAPI.Web;
using IniParser;

namespace spotify_playlist_generator
{


    partial class Program
    {

        public static async System.Threading.Tasks.Task<string> UpdateTokens()
        {
            var iniParser = new FileIniDataParser();

            //create tokens file if it doesn't exist
            if (!System.IO.File.Exists(Settings._TokensIniPath))
            {
                if (Program.Settings._VerboseDebug)
                    Console.WriteLine("[auth] Writing new config file...");

                var newFile = new IniParser.Model.IniData();
                newFile["SPOTIFY CLIENT"]["ID"] = string.Empty;
                newFile["SPOTIFY CLIENT"]["Secret"] = string.Empty;
                newFile["SPOTIFY USER"]["AccessToken"] = string.Empty;
                newFile["SPOTIFY USER"]["RefreshToken"] = string.Empty;
                newFile["NASA"]["Key"] = string.Empty;
                newFile["UNSPLASH"]["AccessKey"] = string.Empty;
                newFile["UNSPLASH"]["SecretKey"] = string.Empty;
                iniParser.WriteFile(Settings._TokensIniPath, newFile);
            }

            //read tokens file
            var tokensIni = iniParser.ReadFile(Settings._TokensIniPath);

            //backout early for failure
            //TODO test for file present but section missing
            if (string.IsNullOrWhiteSpace(tokensIni["SPOTIFY CLIENT"]["ID"]))
            {
                if (Program.Settings._VerboseDebug)
                    Console.WriteLine("[auth] No Spotify client ID...");

                return string.Empty;
            }

            //login for the first time if there's no access token
            if (string.IsNullOrWhiteSpace(tokensIni["SPOTIFY USER"]["AccessToken"]))
            {
                if (Program.Settings._VerboseDebug)
                    Console.WriteLine("[auth] Running first time login proceedure...");

                //https://johnnycrazy.github.io/SpotifyAPI-NET/docs/authorization_code

                var loginRequest = new LoginRequest(
                  new Uri("http://localhost:5000"),
                  tokensIni["SPOTIFY CLIENT"]["ID"],
                  LoginRequest.ResponseType.Code
                )
                {
                    Scope = new[] {
                    Scopes.PlaylistReadPrivate
                    , Scopes.PlaylistReadCollaborative
                    , Scopes.PlaylistModifyPublic
                    , Scopes.PlaylistModifyPrivate
                    , Scopes.UserLibraryRead
                    , Scopes.UgcImageUpload
                    , Scopes.UserModifyPlaybackState
                    , Scopes.UserReadPlaybackState
                    , Scopes.UserReadCurrentlyPlaying
                }
                };
                var uri = loginRequest.ToUri();

                var ps = new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };

                //TODO somehow detect if the user is on a CLI/browserless installation and display the URL to the user with instructions instead
                System.Diagnostics.Process.Start(ps);

                //TODO spin up a temp local web server to auto receive the code if not pasted
                //would only work if the user is running this locally and not on a personal server
                //https://docs.microsoft.com/en-us/dotnet/api/system.net.httplistener?redirectedfrom=MSDN&view=net-5.0

                Console.WriteLine("Please login to Spotify in your browser. Once done, paste the URL from the resulting error page here.");

                //wait for user input
                var code = Console.ReadLine();
                code = code.Replace("http://localhost:5000/?code=", string.Empty);

                //use the auth code to get access/refresh tokens
                var response = await new OAuthClient().RequestToken(
                      new AuthorizationCodeTokenRequest(
                          tokensIni["SPOTIFY CLIENT"]["ID"],
                          tokensIni["SPOTIFY CLIENT"]["Secret"],
                          code,
                          new Uri("http://localhost:5000")
                          )
                    );


                tokensIni["SPOTIFY USER"]["AccessToken"] = response.AccessToken;
                tokensIni["SPOTIFY USER"]["RefreshToken"] = response.RefreshToken;

                if (Program.Settings._VerboseDebug)
                    Console.WriteLine("[auth] Saving access and refresh tokens...");
            }
            //only refresh the token
            if (!string.IsNullOrWhiteSpace(tokensIni["SPOTIFY USER"]["RefreshToken"]))
            {
                if (Program.Settings._VerboseDebug)
                    Console.WriteLine("[auth] Getting a new access token...");

                var response = await new OAuthClient().RequestToken(
                      new AuthorizationCodeRefreshRequest(
                          tokensIni["SPOTIFY CLIENT"]["ID"], 
                          tokensIni["SPOTIFY CLIENT"]["Secret"], 
                          tokensIni["SPOTIFY USER"]["RefreshToken"]
                          )
                    );


                tokensIni["SPOTIFY USER"]["AccessToken"] = response.AccessToken;
                //no refresh token given during subsequent logins
                //tokensIni["SPOTIFY USER"]["RefreshToken"] = response.RefreshToken;
            }
            else
            {
                if (Program.Settings._VerboseDebug)
                    Console.WriteLine("[auth] Problem state.");

                //return nothing for problem state; main sub warns and exits
                return string.Empty;
            }
                
            Program.Tokens.NasaKey = tokensIni["NASA"]["Key"];
            Program.Tokens.UnsplashAccessKey = tokensIni["UNSPLASH"]["AccessKey"];
            Program.Tokens.UnsplashSecretKey = tokensIni["UNSPLASH"]["SecretKey"];

            //save updates to disk
            iniParser.WriteFile(Settings._TokensIniPath, tokensIni);

            if (Program.Settings._VerboseDebug)
                Console.WriteLine("[auth] Writing updates and returning access token.");

            return tokensIni["SPOTIFY USER"]["AccessToken"];
        }

    }
}
