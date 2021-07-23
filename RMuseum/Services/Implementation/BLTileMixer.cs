using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// British Library Tile Downloader And Mixer
    /// </summary>
    public class BLTileMixer
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public BLTileMixer()
        {

        }

        private Dictionary<(int x, int y), Image> _tiles = null;

        public async Task<RServiceResult<Stream>> DownloadMix(string pageUrl, int order)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                _tiles = new Dictionary<(int x, int y), Image>();


                int max_x = -1;
                for (int x = 0; ; x++)
                {
                    string imageUrl = $"http://www.bl.uk/manuscripts/Proxy.ashx?view={pageUrl}_files/13/{x}_0.jpg";
                    var imageResult = await client.GetAsync(imageUrl);

                    int _ImportRetryCount = 5;
                    int _ImportRetryInitialSleep = 500;
                    int retryCount = 0;
                    while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        imageResult.Dispose();
                        Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                        imageResult = await client.GetAsync(imageUrl);
                        retryCount++;
                    }

                    if (imageResult.IsSuccessStatusCode)
                    {
                        using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                        {

                            imageStream.Position = 0;
                            try
                            {
                                Image tile = Image.FromStream(imageStream);
                                _tiles.Add((x, 0), tile);
                                max_x = x;
                            }
                            catch (ArgumentException)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {

                        imageResult.Dispose();
                        return new RServiceResult<Stream>(null, $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}");
                    }
                }

                int max_y = -1;
                for (int y = 1; ; y++)
                {
                    string imageUrl = $"http://www.bl.uk/manuscripts/Proxy.ashx?view={pageUrl}_files/13/0_{y}.jpg";
                    var imageResult = await client.GetAsync(imageUrl);

                    int _ImportRetryCount = 5;
                    int _ImportRetryInitialSleep = 500;
                    int retryCount = 0;
                    while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        imageResult.Dispose();
                        Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                        imageResult = await client.GetAsync(imageUrl);
                        retryCount++;
                    }

                    if (imageResult.IsSuccessStatusCode)
                    {
                        using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                        {
                            imageStream.Position = 0;
                            try
                            {
                                Image tile = Image.FromStream(imageStream);
                                _tiles.Add((0, y), tile);
                                max_y = y;
                            }
                            catch (ArgumentException)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        imageResult.Dispose();
                        return new RServiceResult<Stream>(null, $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}");
                    }
                }

                List<Thread> threadsList = new List<Thread>();

                List<(int x, int y)> unprocessedtiles = new List<(int x, int y)>();
                for (int x = 0; x <= max_x; x++)
                    for (int y = 0; y <= max_y; y++)
                        if (_tiles.TryGetValue((x, y), out Image tmp) == false)
                        {
                            unprocessedtiles.Add((x, y));
                        }

                foreach ((int x, int y) unprocessedtile in unprocessedtiles)
                {
                    Thread th = new Thread
                                (
                                new ThreadStart
                                (
                                async () =>
                                {
                                    await _ProcessTile(pageUrl, unprocessedtile.x, unprocessedtile.y);
                                }
                                )
                                );
                    th.Start();
                    Thread.Sleep(100);
                    threadsList.Add(th);
                }


                foreach (Thread th in threadsList)
                    th.Join();

                int tileWidth = _tiles[(0, 0)].Width;
                int tileHeight = _tiles[(0, 0)].Height;

                int imageWidth = tileWidth * (max_x + 1);
                int imageHeight = tileHeight * (max_y + 1);

                using (Image image = new Bitmap(imageWidth, imageHeight))
                {
                    using (Graphics g = Graphics.FromImage(image))
                    {
                        for (int x = 0; x <= max_x; x++)
                            for (int y = 0; y < max_y; y++)
                            {
                                g.DrawImage(_tiles[(x, y)], new Point(x * tileWidth, y * tileHeight));
                                _tiles[(x, y)].Dispose();
                            }

                        Stream imageStream = new MemoryStream();
                        ImageCodecInfo jpgEncoder = _GetEncoder(ImageFormat.Jpeg);

                        Encoder myEncoder =
                            Encoder.Quality;
                        EncoderParameters jpegParameters = new EncoderParameters(1);

                        EncoderParameter qualityParameter = new EncoderParameter(myEncoder, 92L);
                        jpegParameters.Param[0] = qualityParameter;

                        image.Save(imageStream, jpgEncoder, jpegParameters);
                        imageStream.Position = 0;
                        return new RServiceResult<Stream>(imageStream);
                    }
                }
            }
        }


        private async Task _ProcessTile(string pageUrl, int x, int y)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                string imageUrl = $"http://www.bl.uk/manuscripts/Proxy.ashx?view={pageUrl}_files/13/{x}_{y}.jpg";
                var imageResult = await client.GetAsync(imageUrl);

                int _ImportRetryCount = 5;
                int _ImportRetryInitialSleep = 500;
                int retryCount = 0;
                while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    imageResult.Dispose();
                    Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                    imageResult = await client.GetAsync(imageUrl);
                    retryCount++;
                }

                if (imageResult.IsSuccessStatusCode)
                {
                    using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                    {
                        if (imageStream.Length == 0)
                            return;
                        imageStream.Position = 0;
                        _tiles.Add((x, y), Image.FromStream(imageStream));
                    }
                }
                else
                {
                    imageResult.Dispose();
                }
            }
        }

        /// <summary>
        /// find image encodder
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        private ImageCodecInfo _GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
