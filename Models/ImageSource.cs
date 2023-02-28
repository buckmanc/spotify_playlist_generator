using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spotify_playlist_generator.Models
{
    internal class ImageSource
    {
        public string URL { get; set; }
		private string _tinyURL;

		public string TinyURL
		{
			get 
			{
				if (_tinyURL == null)
				{
					_tinyURL = ImageTools.MakeTinyUrl(this.URL);
                }
				return _tinyURL; 
			}
		}

		private string _tempFilePath;

		public string TempFilePath
		{
			get 
			{
				if (_tempFilePath == null)
				{
                    _tempFilePath = ImageTools.DownloadFile(this.URL, System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
				}

				return _tempFilePath; 
			}
		}

		public ImageSource(string url)
		{
			URL = url;
		}


	}
}
