using System;
using System.Collections.Generic;

namespace Discord.Cea
{
    public static class ImageHelper
    {
        static readonly Dictionary<string, System.Drawing.Image> imageCache = new();

        public static System.Drawing.Image GetImage(string imageUrl)
        {
            if (!imageCache.ContainsKey(imageUrl))
            {
                lock (imageCache)
                {
                    if (!imageCache.ContainsKey(imageUrl))
                    {
                        imageCache[imageUrl] = DownloadImageFromUrl(imageUrl);
                    }
                }
            }

            return imageCache[imageUrl];
        }

        private static System.Drawing.Image DownloadImageFromUrl(string imageUrl)
        {
            System.Drawing.Image image;
            try
            {
                System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(imageUrl);
                webRequest.AllowWriteStreamBuffering = true;
                webRequest.Timeout = 30000;

                System.Net.WebResponse webResponse = webRequest.GetResponse();

                System.IO.Stream stream = webResponse.GetResponseStream();

                image = System.Drawing.Image.FromStream(stream);
                
                webResponse.Close();
            }
            catch (Exception)
            {
                return null;
            }

            return image;
        }
    }
}
