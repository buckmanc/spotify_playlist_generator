using Apod;
using SixLabors.ImageSharp.Drawing;
using spotify_playlist_generator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Unsplash;

namespace spotify_playlist_generator
{
    internal class ImageTools
    {

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
                action:() =>
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
                catch(Exception ex)
                {
                    Console.WriteLine("Error getting random Unsplash image: " + ex.ToString());
                }

            if (photo == null)
                return null;

            return new ImageSource(photo.Urls.Full);
        }
    }
}
