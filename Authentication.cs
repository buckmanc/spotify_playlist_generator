using System;
using SpotifyAPI.Web;
using IniParser;

namespace spotify_playlist_generator
{


    partial class Program
    {

        public static async System.Threading.Tasks.Task<string> UpdateTokens()
        {
            var tokensIniPath = System.IO.Path.Join(Settings._PlaylistFolderPath, "tokens.ini");
            var iniParser = new FileIniDataParser();

            //create tokens file if it doesn't exist
            if (!System.IO.File.Exists(tokensIniPath))
            {
                var newFile = new IniParser.Model.IniData();
                newFile["CLIENT"]["ID"] = string.Empty;
                newFile["CLIENT"]["Secret"] = string.Empty;
                newFile["USER"]["AccessToken"] = string.Empty;
                newFile["USER"]["RefreshToken"] = string.Empty;
                iniParser.WriteFile(tokensIniPath, newFile);
            }

            //read tokens file
            var tokensIni = iniParser.ReadFile(tokensIniPath);

            //backout early for failure
            //TODO test for file present but section missing
            if (string.IsNullOrWhiteSpace(tokensIni["CLIENT"]["ID"]))
            {
                return string.Empty;
            }

            //login for the first time if there's no access token
            if (string.IsNullOrWhiteSpace(tokensIni["USER"]["AccessToken"]))
            {

                //https://johnnycrazy.github.io/SpotifyAPI-NET/docs/authorization_code

                var loginRequest = new LoginRequest(
                  new Uri("http://localhost:5000"),
                  tokensIni["CLIENT"]["ID"],
                  LoginRequest.ResponseType.Code
                )
                {
                    Scope = new[] {
                    Scopes.PlaylistReadPrivate
                    , Scopes.PlaylistReadCollaborative
                    , Scopes.PlaylistModifyPublic
                    , Scopes.PlaylistModifyPrivate
                    , Scopes.UserLibraryRead
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
                          tokensIni["CLIENT"]["ID"],
                          tokensIni["CLIENT"]["Secret"],
                          code,
                          new Uri("http://localhost:5000")
                          )
                    );


                tokensIni["USER"]["AccessToken"] = response.AccessToken;
                tokensIni["USER"]["RefreshToken"] = response.RefreshToken;
            }
            //only refresh the token
            if (!string.IsNullOrWhiteSpace(tokensIni["USER"]["RefreshToken"]))
            {
                var response = await new OAuthClient().RequestToken(
                      new AuthorizationCodeRefreshRequest(
                          tokensIni["CLIENT"]["ID"], 
                          tokensIni["CLIENT"]["Secret"], 
                          tokensIni["USER"]["RefreshToken"]
                          )
                    );


                tokensIni["USER"]["AccessToken"] = response.AccessToken;
                //no refresh token given during subsequent logins
                //tokensIni["USER"]["RefreshToken"] = response.RefreshToken;
            }
            else
            {
                //return nothing for problem state; main sub warns and exits
                return string.Empty;
            }

            //save updates to disk
            iniParser.WriteFile(tokensIniPath, tokensIni);

            return tokensIni["USER"]["AccessToken"];
        }

    }
}
