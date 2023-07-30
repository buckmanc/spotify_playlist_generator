using Apod;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using spotify_playlist_generator.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unsplash;
using VaderSharp2;

namespace spotify_playlist_generator
{
    internal class ImageTools
    {
        public enum TextAlignment
        {
            BottomLeft,
            BottomRight,
            TopLeft,
            TopRight,
            Center,
        }

        //heavily modified from
        //https://stackoverflow.com/a/71750158
        public static string DownloadFile(string url, string outputFolderPath, string fileNameWithoutExtension)
        {
            using (var client = new HttpClient())
            using (var result = client.GetAsync(url).Result)
            {
                if (!result.IsSuccessStatusCode)
                    return null;

                var ext = MimeTypes.MimeTypeMap.GetExtension(result.Content.Headers.ContentType.MediaType);
                var outputFilePath = System.IO.Path.Join(outputFolderPath, System.IO.Path.ChangeExtension(fileNameWithoutExtension, ext));

                if (!System.IO.Directory.Exists(outputFolderPath))
                    System.IO.Directory.CreateDirectory(outputFolderPath);

                System.IO.File.WriteAllBytes(outputFilePath, result.Content.ReadAsByteArrayAsync().Result);

                return outputFilePath;
            }
        }

        //heavily modified from
        //https://stackoverflow.com/a/71750158
        public static void DownloadFile(string url, string path)
        {
            using (var client = new HttpClient())
            using (var result = client.GetAsync(url).Result)
            {
                if (!result.IsSuccessStatusCode)
                    return;

                var ext = MimeTypes.MimeTypeMap.GetExtension(result.Content.Headers.ContentType.MediaType);
                var fileExt = System.IO.Path.GetExtension(path);
                if (fileExt.ToLower() != ext.ToLower())
                {
                    throw new Exception("Specified path file type does not match downloaded file type.");
                }

                var dir = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                System.IO.File.WriteAllBytes(path, result.Content.ReadAsByteArrayAsync().Result);
            }
        }

        //stackoverflow.com/a/366135
        public static string MakeTinyUrl(string url)
        {
            try
            {
                if (!url.ToLower().StartsWith("http") && !url.ToLower().StartsWith("ftp"))
                {
                    url = "http://" + url;
                }
                if (url.Length <= 30)
                {
                    return url;
                }

                using (var client = new HttpClient())
                using (var result = client.GetAsync("http://tinyurl.com/api-create.php?url=" + url).Result)
                {
                    if (!result.IsSuccessStatusCode)
                        return null;

                    string text;
                    using (var reader = new StreamReader(result.Content.ReadAsStream()))
                    {
                        text = reader.ReadToEnd();
                    }
                    return text;
                }
            }
            catch (Exception)
            {
                return url;
            }
        }

        public static ImageSource GetNasaApodImage()
        {

            //consider consolidating all this apod stuff
            var apodClient = new ApodClient(Program.Tokens.NasaKey);

            //grab a random 10 entries, so that if we stumble across a video we have other options
            //wrapping this in a retry as sometimes it returns malformed json
            var apodResonse = Retry.Do(retryIntervalMilliseconds: 5000, maxAttemptCount: 3,
                action: () =>
            {
                return apodClient.FetchApodAsync(10).Result;
            });

            if (apodResonse.StatusCode != ApodStatusCode.OK)
            {
                Console.WriteLine("Someone's done an oopsie.");
                Console.WriteLine(apodResonse.Error.ErrorCode);
                Console.WriteLine(apodResonse.Error.ErrorMessage);
                Environment.Exit(-1);
            }

            var apod = apodResonse.AllContent.Where(c =>
                c.MediaType == MediaType.Image &&
                !System.IO.Path.GetExtension(c.ContentUrlHD).Like("*mp4")
            ).FirstOrDefault();
            //if (Program.Settings._VerboseDebug)
            //{
            //    Console.WriteLine(apod.Title);
            //    Console.WriteLine(apod.ContentUrl);
            //    Console.WriteLine(apod.Explanation);
            //}

            return new ImageSource(apod.ContentUrlHD);
        }

        public static ImageSource GetUnsplashImage(string search)
        {
            var client = new UnsplashClient(new ClientOptions
            {
                AccessKey = Program.Tokens.UnsplashAccessKey
            });

            Unsplash.Models.Photo.IBasic photo = null;

            if (!string.IsNullOrWhiteSpace(search))
                photo = client.Search.PhotosAsync(search, new Unsplash.Api.SearchPhotosParams(page: 1, perPage: 100))
                    .Result.Results.ToList().Random();
            else
                try
                {
                    photo = client.Photos.GetRandomPhotosAsync(new Unsplash.Api.RandomPhotoFilterOptions(count: 1)).Result.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error getting random Unsplash image: " + ex.ToString());
                }

            if (photo == null)
                return null;

            return new ImageSource(photo.Urls.Full);
        }

        public static void HandleImageStuff(MySpotifyWrapper spotifyWrapper, string playlistName, IEnumerable<string> leaveImageAlonePlaylistNames,
            bool imageAddPhoto, bool imageAddText,
            string imageText, string imageFont,
            TextAlignment imageTextAlignment, int imageRotateDegrees, string imageClone, string imageNerdFontGlyph
            , int testImages = 0
            )
        {
            // adding test mode to this method rather than making a test method is kind of convoluted
            // but allows for more accurate test images
            // of course there are more robust ways but alas time is limited

            Console.Write("Generating covers...");

            var testMode = testImages > 0;
            List<FullPlaylist> playlists;

            if (!string.IsNullOrWhiteSpace(playlistName))
                playlists = BackupAndPrepPlaylistImage(spotifyWrapper, playlistName, leaveImageAlonePlaylistNames);
            else
                playlists = new();

            // you can generate test images from playlists PLUS some random ones
            var totalImages = new int[] { testImages, playlists?.Count ?? 0 }.Max();

            if (!string.IsNullOrWhiteSpace(imageNerdFontGlyph))
            {
                var glyph = NerdFontGlyph(imageNerdFontGlyph);
                if (string.IsNullOrWhiteSpace(glyph))
                {
                    Console.WriteLine("Could not find matching Nerd Font glyph.");
                    return;
                }
                else if (imageAddText)
                    imageText = glyph;
                else
                {
                    Console.WriteLine(glyph);
                    return;
                }
            }

            var pp = new ProgressPrinter(totalImages, (perc, time) => Program.ConsoleWriteAndClearLine("\rGenerating covers: " + perc + ", " + time + " remaining"));
            for (int i = 0; i < totalImages; i++)
            {
                FullPlaylist playlist;
                string sentimentText;
                string path = null;
                string iterationImageText;

                if (testMode)
                {
                    var testDir = System.IO.Path.Join(Program.Settings._ImageTestFolderPath, Program.RunStart.ToString("yyyy-MM-dd"));
                    var testFileInts = !System.IO.Directory.Exists(testDir) ? new int[] { } :
                        System.IO.Directory.GetFiles(testDir, "*.jpg")
                        .Select(file => System.IO.Path.GetFileNameWithoutExtension(file))
                        .Where(file => file.All(c => char.IsNumber(c)))
                        .Select(file => int.Parse(file))
                        .ToArray();
                    var maxFileNum = testFileInts.Any() ? testFileInts.Max() : 0;

                    path = System.IO.Path.Join(testDir, (maxFileNum + 1).ToString() + ".jpg");
                }

                if (playlists?.ElementAtOrDefault(i) != null)
                {
                    playlist = playlists[i];

                    var playlistTracks = spotifyWrapper.GetTracksByPlaylist(new string[] { playlist.Id }).ToArray();

                    var textSampleLines = new List<string>();
                    textSampleLines.AddRange(playlistTracks.Select(t => t.Name));
                    textSampleLines.AddRange(playlistTracks.Select(t => t.AlbumName));
                    textSampleLines.AddRange(playlistTracks.Select(t => t.AlbumName));
                    textSampleLines.AddRange(playlistTracks.SelectMany(t => t.ArtistNames).Distinct());
                    textSampleLines.AddRange(playlistTracks.SelectMany(t => t.ArtistNames).Distinct());
                    textSampleLines.AddRange(playlistTracks.SelectMany(t => t.ArtistNames).Distinct());

                    sentimentText = textSampleLines.Join(", ");
                    path ??= playlist.GetWorkingImagePath();
                }
                else
                {
                    playlist = null;
                    sentimentText = null;
                }

                if (imageNerdFontGlyph?.Trim()?.ToLower() == "random")
                    imageText = NerdFontGlyph(imageNerdFontGlyph);

                iterationImageText = imageText ?? playlist?.Name?.TrimStart(Program.Settings._StartPlaylistsWith) ?? PlaylistNames.Random();
                GenerateCoverArt(workingPath: path,
                    imageAddPhoto: imageAddPhoto,
                    imageAddPhotoSentimentText: sentimentText,
                    imageAddText: imageAddText, imageText: iterationImageText, imageFont: imageFont, imageTextAlignment: imageTextAlignment,
                    imageRotateDegrees: imageRotateDegrees, imageClone: imageClone, spotifyWrapper: spotifyWrapper,
                    imageNerdFontGlyph: imageNerdFontGlyph,
                    imageSource: out ImageSource imageSource
                    );


                //attempt to update image
                var success = !testMode && spotifyWrapper.UploadPlaylistImage(playlist, playlist.GetWorkingImagePath());

                //update description if image update was successful and we know the image source
                if (success && imageSource != null)
                {
                    //prep description update
                    var req = new PlaylistChangeDetailsRequest();
                    var oldAttribText = new Regex(@"Cover: .*").Match(playlist.Description).Value;
                    var newAttribText = "Cover: " + imageSource.TinyURL;
                    if (!string.IsNullOrWhiteSpace(oldAttribText))
                    {
                        req.Description = playlist.Description.Replace(oldAttribText, newAttribText, StringComparison.InvariantCultureIgnoreCase);
                    }
                    else
                    {
                        req.Description = playlist.Description + " " + newAttribText;
                    }

                    spotifyWrapper.spotify.Playlists.ChangeDetails(playlist.Id, req);
                }
                pp.PrintProgress();
            }
            Console.WriteLine();
        }

        // TODO no spotify references here so image tests can be done locally and API hits can be kept to a minimum
        // spotify wrapper is only passed in for ImageClone
        public static void GenerateCoverArt(
            string workingPath,
            bool imageAddPhoto, string imageAddPhotoSentimentText,
            bool imageAddText, string imageText, string imageFont, TextAlignment imageTextAlignment,
            int imageRotateDegrees, string imageClone, MySpotifyWrapper spotifyWrapper, string imageNerdFontGlyph,
            out ImageSource imageSource)
        {
            imageSource = null;
            SixLabors.ImageSharp.Image<Rgba32> img = null;

            if (!string.IsNullOrWhiteSpace(imageNerdFontGlyph))
            {
                var glyph = NerdFontGlyph(imageNerdFontGlyph);
                if (string.IsNullOrWhiteSpace(glyph))
                {
                    Console.WriteLine("Could not find matching Nerd Font glyph.");
                    return;
                }
                else if (imageAddText)
                    imageText = glyph;
                else
                    Console.WriteLine(glyph);
            }


            if (!string.IsNullOrWhiteSpace(imageClone))
                img = ImageClone(spotifyWrapper, imageClone);
            else if (imageAddPhoto || !System.IO.File.Exists(workingPath))
                img = ImageAddPhoto(path: workingPath, sentimentText: imageAddPhotoSentimentText, imageSource: out imageSource);
            else
                img = SixLabors.ImageSharp.Image.Load<Rgba32>(workingPath);

            if (imageAddText)
                ImageAddText(img, imageText, imageFont, imageTextAlignment);


            // save image
            var jpgEncoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder();
            jpgEncoder.ColorType = SixLabors.ImageSharp.Formats.Jpeg.JpegColorType.Rgb;

            var dir = System.IO.Path.GetDirectoryName(workingPath);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            img.SaveAsJpeg(workingPath, jpgEncoder);
            // img.Dispose();
        }

        internal static List<FullPlaylist> BackupAndPrepPlaylistImage(MySpotifyWrapper spotifyWrapper, string playlistName, IEnumerable<string> leaveImageAlonePlaylistNames, bool OverwriteBackup = false)
        {
            //technically this method violates the rule of single concern
            //but this is a personal project with limited time, so here we go

            if (string.IsNullOrWhiteSpace(playlistName))
            {
                Console.WriteLine("--playlist-name is required for playlist image operations");
                return null;
            }

            var playlists = spotifyWrapper.GetUsersPlaylists(playlistName, Program.Settings._StartPlaylistsWith);
            playlists.Remove(p => leaveImageAlonePlaylistNames?.Contains(p.Name) ?? false);

            if (!playlists.Any())
            {
                Console.WriteLine("No playlists named \"" + playlistName + "\" found.");
                return playlists;
            }

            var playlistsWithImages = playlists.Where(p => p.Images.Any()).ToList();

            foreach (var playlist in playlistsWithImages)
            {
                //don't download the image if a working copy already exists
                //these are cleared at the beginning of the session and the playlists are NOT refreshed as we go
                //so this will be the most up-to-date copy
                if (!System.IO.File.Exists(playlist.GetWorkingImagePath()))
                    spotifyWrapper.DownloadPlaylistImage(playlist, playlist.GetWorkingImagePath());

                if (!System.IO.File.Exists(playlist.GetBackupImagePath()) || OverwriteBackup)
                {
                    if (!System.IO.Directory.Exists(Program.Settings._ImageBackupFolderPath))
                        System.IO.Directory.CreateDirectory(Program.Settings._ImageBackupFolderPath);

                    System.IO.File.Copy(playlist.GetWorkingImagePath(), playlist.GetBackupImagePath());
                }
            }

            return playlists;
        }

        internal static void RestorePlaylistImage(MySpotifyWrapper spotifyWrapper, string playlistName, IEnumerable<string> leaveImageAlonePlaylistNames)
        {

            if (!System.IO.Directory.Exists(Program.Settings._ImageBackupFolderPath))
            {
                Console.WriteLine("No backup image found for " + playlistName + ".");
                return;
            }

            var playlists = spotifyWrapper.GetUsersPlaylists(playlistName, Program.Settings._StartPlaylistsWith);
            playlists.Remove(p => leaveImageAlonePlaylistNames.Contains(p.Name));
            var playlistsWithBackups = playlists.Where(p => System.IO.File.Exists(p.GetBackupImagePath())).ToArray();

            if (!playlistsWithBackups.Any())
            {
                Console.WriteLine("No backup image found for " + playlistName + ".");
                return;
            }

            //restore and burn
            foreach (var playlist in playlists)
            {
                spotifyWrapper.UploadPlaylistImage(playlist, playlist.GetBackupImagePath());
                System.IO.File.Delete(playlist.GetBackupImagePath());
            }
        }

        static string NerdFontGlyph(string search)
        {
            var url = "https://nerdfonts.com/assets/css/webfont.css";
            var filePath = System.IO.Path.Join(Program.Settings._CacheFolderPath, "nerdfont.css");

            // if not exists or modified more than a week ago
            ImageTools.DownloadFile(url, filePath);

            var css = System.IO.File.ReadAllText(filePath);
            var reggy = new Regex(@"\.(?<key>[a-z\-_]+):\w+{content:""\\(?<value>[a-f0-9]+)""}", RegexOptions.IgnoreCase);
            var nfBreakdown = reggy.Matches(css)
                .Where(m => m.Success)
                .ToDictionary(m => m.Groups["key"].Value, m => m.Groups["value"].Value);

            // Console.WriteLine(nfBreakdown.Count().ToString("#,##0") + " nerd font glyphs found");
            var hexString = string.Empty;

            if (search.Trim().ToLower() == "random")
                hexString = nfBreakdown.ToList().Random().Value;
            else
                hexString = nfBreakdown
                .Where(kvp => kvp.Key.Like(search))
                .Select(kvp => kvp.Value)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(hexString))
                return null;

            var intValue = Int32.Parse(hexString, System.Globalization.NumberStyles.AllowHexSpecifier);
            var outputChar = char.ConvertFromUtf32(intValue);

            return outputChar.ToString();

        }

        static void ImageAddText(SixLabors.ImageSharp.Image<Rgba32> img, string imageText, string imageFont, TextAlignment imageTextAlignment
            )
        {
            imageText = imageText
                .Replace(" - ", Environment.NewLine)
                .Trim(Environment.NewLine)
                .Trim()
                ;

            var thief = new ColorThief.ImageSharp.ColorThief();
            var colorPalette = thief.GetPalette(img, 10);

            //SixLabors.ImageSharp.Color darkColor;
            //SixLabors.ImageSharp.Color lightColor = Color.White;

            //if (colorPalette.Any(x => x.IsDark))
            //    darkColor = Color.Parse(colorPalette
            //        .OrderByDescending(x => x.Color.ToHsl().L)
            //        //.First(x => x.IsDark)
            //        .First()
            //        .Color.ToHexString());
            //else
            //    darkColor = Color.Black;

            //if (colorPalette.Any(x => !x.IsDark))
            //    lightColor = Color.Parse(colorPalette
            //        .OrderBy(x => x.Color.ToHsl().L)
            //        //.First(x => !x.IsDark)
            //        .First()
            //        .Color.ToHexString());
            //else
            //    lightColor = Color.White;

            var darkColor = Color.Parse(colorPalette
                                    //.OrderByDescending(x => x.Color.ToHsl().L)
                                    .OrderBy(x => new int[] { x.Color.R, x.Color.G, x.Color.B }.Average())
                                    .First()
                                    .Color.ToHexString());

            var lightColor = Color.Parse(colorPalette
                                    .OrderByDescending(x => new int[] { x.Color.R, x.Color.G, x.Color.B }.Average())
                                    .First()
                                    .Color.ToHexString());


            var fontPointToPixelRatio = (1.0 / 3) + 1;
            var edgeDistance = (int)Math.Round(img.Height * 0.033333, 0);

            // enbiggen the border if the text is going to be huge
            if (imageText.Length <= 4 && imageTextAlignment == TextAlignment.Center)
                edgeDistance = edgeDistance * 2;
            var targetTextWidth = (img.Width - (edgeDistance * 2));

            var fontSize = 0;

            if (imageTextAlignment == TextAlignment.Center)
            {
                // center is the only size it makes sense to make huge
                // in normal case it will reduce itself to a reasonable amount
                // but for symbols it'll be perfect
                fontSize = (int)Math.Round((img.Height - edgeDistance) * fontPointToPixelRatio);
            }
            else
            {
                //a rough target size, works out to be the max
                fontSize = img.Height / 5;
            }
            var textWrappingLength = -1;

            // TODO package the font with the app
            // TODO try glow instead of outline
            var fontFamily = SystemFonts.Families
                .Where(f => f.Name.Like(imageFont ?? Program.Settings._DefaultFont))
                .ToList()
                .Random();

            if (fontFamily == default)
                fontFamily = SystemFonts.Families.ToList().Random();

            var font = fontFamily.CreateFont((float)fontSize, FontStyle.Regular);

            var txtSizeInitial = TextMeasurer.MeasureBounds(text: imageText, new TextOptions(font));

            if (txtSizeInitial.Width > targetTextWidth)
            {
                // width / scaling factor calc is for wrapping considerations
                // height / font point ratio calc is for determining if the font is too small
                // if text is wrapped prior to this the font height won't be relevant for this anymore
                var scalingFactor = targetTextWidth / txtSizeInitial.Width;
                var heightRatio = (txtSizeInitial.Height * scalingFactor) / img.Height;

                var minimumFontHeightRatio = 0.15;

                if (heightRatio < minimumFontHeightRatio)
                {
                    // TODO would be very cool to allow giant blocks of text to fill the art
                    // need to research how to do that calc
                    var newScalingFactor = (minimumFontHeightRatio * img.Height) / txtSizeInitial.Height;
                    scalingFactor = (float)newScalingFactor;
                    textWrappingLength = targetTextWidth;
                }

                font = fontFamily.CreateFont((float)fontSize * scalingFactor, FontStyle.Regular);
            }
            var txtSizeFinal = TextMeasurer.MeasureBounds(text: imageText, new TextOptions(font) { WrappingLength = textWrappingLength});

            SixLabors.Fonts.HorizontalAlignment mutateHorizontalAlignment;
            SixLabors.Fonts.VerticalAlignment mutateVerticalAlignment;
            PointF origin;

            // missing a few options like top center
            switch (imageTextAlignment)
            {
                case TextAlignment.BottomLeft:
                default:
                    mutateHorizontalAlignment = HorizontalAlignment.Left;
                    mutateVerticalAlignment = VerticalAlignment.Bottom;
                    origin = new PointF(edgeDistance, img.Height - edgeDistance);
                    break;
                case TextAlignment.BottomRight:
                    mutateHorizontalAlignment = HorizontalAlignment.Right;
                    mutateVerticalAlignment = VerticalAlignment.Bottom;
                    origin = new PointF(img.Width - edgeDistance, img.Height - edgeDistance);
                    break;
                case TextAlignment.TopRight:
                    mutateHorizontalAlignment = HorizontalAlignment.Right;
                    mutateVerticalAlignment = VerticalAlignment.Top;
                    origin = new PointF(img.Width - edgeDistance, edgeDistance);
                    break;
                case TextAlignment.TopLeft:
                    mutateHorizontalAlignment = HorizontalAlignment.Left;
                    mutateVerticalAlignment = VerticalAlignment.Top;
                    origin = new PointF(edgeDistance, edgeDistance);
                    break;
                case TextAlignment.Center:
                    mutateHorizontalAlignment = HorizontalAlignment.Center;
                    mutateVerticalAlignment = VerticalAlignment.Center;
                    origin = new PointF(img.Width / 2, img.Height / 2);
                    break;

            }

            // Console.WriteLine("img.Width: " + img.Width.ToString("#,##0.00"));
            // Console.WriteLine("img.Height: " + img.Height.ToString("#,##0.00"));
            // Console.WriteLine("txtSizeFinal.Width: " + txtSizeFinal.Width.ToString("#,##0.00"));
            // Console.WriteLine("txtSizeFinal.Height: " + txtSizeFinal.Height.ToString("#,##0.00"));
            // Console.WriteLine("origin: " + origin.ToString());
            // Console.WriteLine("final font size: " + font.Size.ToString());

            // TODO need to scale between small and big sizes better
            // small fonts need a smaller *ratio* and big fonts need a bigger one
            const float minPenSize = 2f;
            var penSize = (float)Math.Floor(font.Size / 100.00);
            //var penSize = (float)Math.Floor(txtSizeFinal.Width / 70.00);
            if (penSize < minPenSize)
                penSize = minPenSize;

            // intermittent bug occurs here when picking a random font
            // there's likely a font that's incompatible somehow
            img.Mutate(x => x.DrawText(
                textOptions: new TextOptions(font)
                {
                    Origin = origin,
                    WrappingLength = textWrappingLength,
                    HorizontalAlignment = mutateHorizontalAlignment, 
                    VerticalAlignment = mutateVerticalAlignment, 
                },
                text: imageText,
                brush: Brushes.Solid(lightColor),
                pen: Pens.Solid(darkColor, penSize)
                )
            );
        }

        static void ImageRotate(SixLabors.ImageSharp.Image img, int imageRotateDegrees)
        {
            img.Mutate(x => x.Rotate((float)imageRotateDegrees));
        }
        static SixLabors.ImageSharp.Image<Rgba32> ImageClone(MySpotifyWrapper spotifyWrapper, string imageClonePlaylistName) 
        {
            var sourcePlaylist = BackupAndPrepPlaylistImage(spotifyWrapper, imageClonePlaylistName, new string[] { }).FirstOrDefault();

            if (sourcePlaylist == null)
            {
                Console.WriteLine("Could not find " + imageClonePlaylistName);
                return null;
            }

            return SixLabors.ImageSharp.Image.Load<Rgba32>(sourcePlaylist.GetWorkingImagePath());
        }


        private static Random _random;
        static SixLabors.ImageSharp.Image<Rgba32> ImageAddPhoto(string path, out ImageSource imageSource, string sentimentText = null)
        {
            double sentimentScore = 0;

            if (!string.IsNullOrWhiteSpace(sentimentText))
            {
                var analyzer = new SentimentIntensityAnalyzer();
                var sentiment = analyzer.PolarityScores(sentimentText);
                sentimentScore = sentiment.Compound;
            }
            else
            {
                // assign a random sentiment if no sample is provided
                _random ??= new Random();
                sentimentScore = _random.NextDouble(-1, 1);
            }

            var searchTerm = new string[] {
                    //"concert", "music", "record player", "party", "guitar", "karaoke"
                    "forest", "mountains", "scenic", "nature", "dark nature", "landscape", "night"
                }
                .Random();

            //unsplash image for positive sentiment, nasa image for negative sentiment
            if (sentimentScore >= 0)
            {
                imageSource = ImageTools.GetUnsplashImage(searchTerm);
            }
            else
            {
                // nasa astronomy picture of the day
                imageSource = ImageTools.GetNasaApodImage();
            }

            // TODO error check for failure to decode image here
            // SixLabors.ImageSharp.UnknownImageFormatException
            //scale and crop image to fit
            var img = SixLabors.ImageSharp.Image.Load<Rgba32>(imageSource.TempFilePath);
            
                var targetDim = 640;

                var minDim = new int[] { img.Width, img.Height }.Min();
                // make it a little bigger so we can punch an image out of the middle
                var ratio = (targetDim * 1.5) / minDim;
                var resizeSize = new Size((int)Math.Round(img.Width * ratio, 0), (int)Math.Round(img.Height * ratio, 0));

                //if (ratio > 1 || resizeSize.Width == 0 || resizeSize.Height == 0)
                //{
                //    resizeSize = new Size(minDim, minDim);
                //}

                //Console.WriteLine("targetDim:	" + targetDim.ToString());
                //Console.WriteLine("minDim:	" + minDim.ToString());
                //Console.WriteLine("ratio:	" + ratio.ToString());
                //Console.WriteLine("resizeSize:	" + resizeSize.Width.ToString() + " width, " + resizeSize.Height.ToString() + " height");

                img.Mutate(
                    i => i
                            .Resize(resizeSize)
                            .Crop(new Rectangle(
                                x: (resizeSize.Width - targetDim) / 2,
                                y: (resizeSize.Height - targetDim) / 2,
                                width: targetDim,
                                height: targetDim
                                ))
                            );
                return img;
        }

        // samples generated by chatgpt
        private static string[] PlaylistNames = new string[] {
 "Chill Vibes"
, "Feel Good Favorites"
, "Party Hits"
, "Road Trip Jams"
, "Acoustic Delights"
, "Throwback Anthems"
, "Summer Tunes"
, "Late Night Grooves"
, "Workout Motivation"
, "Relaxing Instrumentals"
, "Indie Gems"
, "R&B Soulful Sounds"
, "Energetic Pop"
, "Mellow Beats"
, "Country Classics"
, "Electronic Dance Mix"
, "Rock Legends"
, "Hip-Hop Bangers"
, "Soothing Classical"
, "Feel-Good 90s"
, "Latin Fiesta"
, "EDM Festival Anthems"
, "Golden Oldies"
, "Singer-Songwriter Showcase"
, "Fresh Discoveries"
, "Alternative Hits"
, "Motown Magic"
, "Reggae Rhythms"
, "Piano Melodies"
, "Tropical House Party"
, "Throwback Hip-Hop"
, "Soulful Jazz"
, "Upbeat Pop Hits"
, "80s Flashback"
, "Coffee Shop Vibes"
, "Indie Folk Favorites"
, "Classic Rock Anthems"
, "Rhythmic Rap"
, "Relaxing Nature Sounds"
, "Dancehall Fever"
, "Motivational Upbeats"
, "Smooth Jazz"
, "Pop Divas"
, "Funky Grooves"
, "90s Alternative"
, "Country Roadtrip"
, "Deep House Chillout"
, "Classic R&B Hits"
, "Epic Film Soundtracks"
, "Salsa Sensation"
, "Acoustic Covers"
, "Indie Pop Mix"
, "Soulful Ballads"
, "Top Hits of Today"
, "Retro Rewind"
, "Hip-Hop Classics"
, "Relaxing Spa Music"
, "Caribbean Vibes"
, "Energetic EDM"
, "Feel-Good Funk"
, "Summer Pool Party"
, "Smooth Rhythms"
, "Pop Punk Anthems"
, "Jazzy Smooth Vocals"
, "Classical Masterpieces"
, "Rock Ballads"
, "Hip-Hop Party Starters"
, "Instrumental Focus"
, "Sunny Day Tunes"
, "Indie Rock Collection"
, "Soulful Motown Classics"
, "Reggae Chillout"
, "Piano and Strings"
, "Tropical Pop Hits"
, "Throwback 2000s"
, "Country Love Songs"
, "Deep House Vibes"
, "R&B Slow Jams"
, "Epic Orchestral Soundscapes"
, "Latin Pop Party"
, "Acoustic Mellow"
, "Indie Electronic Mix"
, "Smooth Saxophone Sounds"
, "Contemporary Pop Hits"
, "Folk and Americana"
, "Rock Revival"
, "Hip-Hop Club Bangers"
, "Ambient Meditation"
, "Caribbean Reggaeton"
, "Feel-Good Ukulele"
, "Indie Soul Chill"
, "Motown Dancefloor"
, "Salsa Romantica"
, "Classical Piano Melodies"
, "Indie Pop/Rock Fusion"
, "Soulful Love Songs"
, "Pop Hits Rewind"
, "Chillstep Relaxation"
, "Funky Disco Grooves"
, "Summer Beach Vibes"};
    }
}
