using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.ImportJob;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace RMuseum.Services.Implementation
{

    /// <summary>
    /// IArtifactService implementation
    /// </summary>
    public partial class ArtifactService : IArtifactService
    {
        /// <summary>
        /// from http://www.bl.uk
        /// </summary>
        /// <param name="resourceNumber">grenville_xli_f001r</param>
        /// <param name="friendlyUrl"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromBritishLibrary(string resourceNumber, string friendlyUrl, bool skipUpload)
        {
            string url = $"http://www.bl.uk/manuscripts/Viewer.aspx?ref={resourceNumber}";
            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.BritishLibrary && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                    .SingleOrDefaultAsync()
                )
                !=
                null
                )
            {
                return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing {url}");
            }

            if (string.IsNullOrEmpty(friendlyUrl))
            {
                friendlyUrl = resourceNumber;
            }

            if (
                (await _context.Artifacts.Where(a => a.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync())
                !=
                null
               )
            {
                return new RServiceResult<bool>(false, $"duplicated artifact friendly url '{friendlyUrl}'");
            }



            ImportJob job = new ImportJob()
            {
                JobType = JobType.BritishLibrary,
                ResourceNumber = resourceNumber,
                FriendlyUrl = friendlyUrl,
                SrcUrl = url,
                QueueTime = DateTime.Now,
                ProgressPercent = 0,
                Status = ImportJobStatus.NotStarted
            };


            await _context.ImportJobs.AddAsync
                (
                job
                );

            await _context.SaveChangesAsync();


            try
            {

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                            try
                            {


                                using (var client = new HttpClient())
                                {
                                    client.Timeout = TimeSpan.FromMinutes(5);
                                    using (var result = await client.GetAsync(url))
                                    {
                                        if (result.IsSuccessStatusCode)
                                        {
                                            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                            {
                                                if (
                                                   (await context.Artifacts.Where(a => a.FriendlyUrl == job.FriendlyUrl).SingleOrDefaultAsync())
                                                   !=
                                                   null
                                                  )
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "aborted because of duplicated friendly url";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }
                                                RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from {url}", $"extracted from {url}")
                                                {
                                                    Status = PublishStatus.Draft,
                                                    DateTime = DateTime.Now,
                                                    LastModified = DateTime.Now,
                                                    CoverItemIndex = 0,
                                                    FriendlyUrl = friendlyUrl
                                                };


                                                List<RTagValue> meta = new List<RTagValue>();
                                                RTagValue tag;

                                                string html = await result.Content.ReadAsStringAsync();


                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                {
                                                    job.StartTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Running;
                                                    job.SrcContent = html;
                                                    importJobUpdaterDb.Update(job);
                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                }


                                                int nIdxStart = html.IndexOf("PageList");
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "PageList not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nIdxStart = html.IndexOf("value=\"", nIdxStart);
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "value after PageList not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nIdxStart += "value=\"".Length;

                                                string strPageList = html.Substring(nIdxStart, html.IndexOf('"', nIdxStart) - nIdxStart);

                                                nIdxStart = html.IndexOf("TextList");
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "TextList not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nIdxStart = html.IndexOf("value=\"", nIdxStart);
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "value after TextList not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nIdxStart += "value=\"".Length;

                                                string strTextList = html.Substring(nIdxStart, html.IndexOf('"', nIdxStart) - nIdxStart);

                                                nIdxStart = html.IndexOf("TitleList");
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "TitleList not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nIdxStart = html.IndexOf("value=\"", nIdxStart);
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "value after TitleList not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nIdxStart += "value=\"".Length;

                                                string strTitleList = html.Substring(nIdxStart, html.IndexOf('"', nIdxStart) - nIdxStart);

                                                string[] PageUrls = strPageList.Split("||", StringSplitOptions.None);
                                                string[] PageTexts = strTextList.Split("||", StringSplitOptions.None);
                                                string[] PageTitles = strTitleList.Split("||", StringSplitOptions.None);

                                                if (PageUrls.Length != PageTexts.Length || PageTexts.Length != PageTitles.Length)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "PageUrls.Length != PageTexts.Length || PageTexts.Length != PageTitles.Length";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                tag = await TagHandler.PrepareAttribute(context, "Title", "Untitled", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Contributor Names", "Unknown", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);

                                                book.Tags = meta.ToArray();


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "British Library", 1);
                                                string viewerUrl = $"http://www.bl.uk/manuscripts/FullDisplay.aspx?ref={resourceNumber.Substring(0, resourceNumber.LastIndexOf('_'))}";
                                                tag.ValueSupplement = viewerUrl;

                                                meta.Add(tag);

                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                int order = 0;
                                                for (int i = 0; i < PageUrls.Length; i++)
                                                {
                                                    if (PageUrls[i] == "##")
                                                        continue;
                                                    order++;

                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.ProgressPercent = order;
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }


                                                    RArtifactItemRecord page = new RArtifactItemRecord()
                                                    {
                                                        Name = $"تصویر {order}",
                                                        NameInEnglish = $"Image {order}",
                                                        Description = "",
                                                        DescriptionInEnglish = "",
                                                        Order = order,
                                                        FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                                        LastModified = DateTime.Now
                                                    };


                                                    List<RTagValue> pageTags = new List<RTagValue>();
                                                    tag = await TagHandler.PrepareAttribute(context, "Source", "British Library", 1);
                                                    tag.ValueSupplement = $"http://www.bl.uk/manuscripts/Viewer.aspx?ref={PageUrls[i]}";
                                                    pageTags.Add(tag);


                                                    if (!string.IsNullOrEmpty(PageTitles[i]))
                                                    {
                                                        RTagValue toc = await TagHandler.PrepareAttribute(context, "Title in TOC", PageTitles[i], 1);
                                                        toc.ValueSupplement = "1";//font size
                                                        pageTags.Add(toc);
                                                    }

                                                    if (!string.IsNullOrEmpty(PageTexts[i]))
                                                    {
                                                        tag = await TagHandler.PrepareAttribute(context, "Label", PageTexts[i], 1);
                                                        pageTags.Add(tag);

                                                    }

                                                    page.Tags = pageTags.ToArray();


                                                    bool recovered = false;
                                                    if (
                                                       File.Exists
                                                       (
                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                       )
                                                       &&
                                                       File.Exists
                                                       (
                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                       )
                                                       &&
                                                       File.Exists
                                                       (
                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                       )
                                                       )
                                                    {
                                                        RServiceResult<RPictureFile> picture = await _pictureFileService.RecoverFromeFiles(page.Name, page.Description, 1,
                                                            viewerUrl,
                                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                            $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                        if (picture.Result != null)
                                                        {
                                                            recovered = true;
                                                            page.Images = new RPictureFile[] { picture.Result };
                                                            page.CoverImageIndex = 0;

                                                            if (book.CoverItemIndex == (order - 1))
                                                            {
                                                                book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                            }

                                                            pages.Add(page);
                                                        }

                                                    }
                                                    if (!recovered)
                                                    {
                                                        if (
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )

                                                       )
                                                        {
                                                            File.Delete
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           );
                                                        }
                                                        if (

                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )

                                                       )
                                                        {
                                                            File.Delete
                                                            (
                                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                            );
                                                        }
                                                        if (

                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                       )
                                                        {
                                                            File.Delete
                                                            (
                                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                            );
                                                        }

                                                        /*
                                                         failed multithread attempt:

                                                            BLTileMixer mixer = new BLTileMixer();
                                                            RServiceResult<Stream> blResult = await mixer.DownloadMix(PageUrls[i], order);
                                                         */


                                                        Dictionary<(int x, int y), Image> tiles = new Dictionary<(int x, int y), Image>();
                                                        int max_x = -1;
                                                        for (int x = 0; ; x++)
                                                        {
                                                            string imageUrl = $"http://www.bl.uk/manuscripts/Proxy.ashx?view={PageUrls[i]}_files/13/{x}_0.jpg";
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
                                                                        tiles.Add((x, 0), tile);
                                                                        max_x = x;
                                                                    }
                                                                    catch (ArgumentException)//in other cases throw the exception
                                                                    {
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                                {
                                                                    job.EndTime = DateTime.Now;
                                                                    job.Status = ImportJobStatus.Failed;
                                                                    job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                                    importJobUpdaterDb.Update(job);
                                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                                }

                                                                imageResult.Dispose();
                                                                return;
                                                            }
                                                        }

                                                        int max_y = -1;
                                                        for (int y = 1; ; y++)
                                                        {
                                                            string imageUrl = $"http://www.bl.uk/manuscripts/Proxy.ashx?view={PageUrls[i]}_files/13/0_{y}.jpg";
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
                                                                    if (imageStream.Length <= 248)
                                                                        break;
                                                                    imageStream.Position = 0;
                                                                    try
                                                                    {
                                                                        Image tile = Image.FromStream(imageStream);
                                                                        tiles.Add((0, y), tile);
                                                                        max_y = y;
                                                                    }
                                                                    catch (ArgumentException)//in other cases throw the exception
                                                                    {
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                                {
                                                                    job.EndTime = DateTime.Now;
                                                                    job.Status = ImportJobStatus.Failed;
                                                                    job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                                    importJobUpdaterDb.Update(job);
                                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                                }

                                                                imageResult.Dispose();
                                                                return;
                                                            }
                                                        }

                                                        for (int x = 0; x <= max_x; x++)
                                                            for (int y = 0; y <= max_y; y++)
                                                                if (tiles.TryGetValue((x, y), out Image tmp) == false)
                                                                {
                                                                    string imageUrl = $"http://www.bl.uk/manuscripts/Proxy.ashx?view={PageUrls[i]}_files/13/{x}_{y}.jpg";
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
                                                                                break;
                                                                            imageStream.Position = 0;
                                                                            tiles.Add((x, y), Image.FromStream(imageStream));
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                                        {
                                                                            job.EndTime = DateTime.Now;
                                                                            job.Status = ImportJobStatus.Failed;
                                                                            job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                                            importJobUpdaterDb.Update(job);
                                                                            await importJobUpdaterDb.SaveChangesAsync();
                                                                        }

                                                                        imageResult.Dispose();
                                                                        return;
                                                                    }
                                                                }

                                                        int tileWidth = tiles[(0, 0)].Width;
                                                        int tileHeight = tiles[(0, 0)].Height;

                                                        int imageWidth = tileWidth * (max_x + 1);
                                                        int imageHeight = tileHeight * (max_y + 1);

                                                        using (Image image = new Bitmap(imageWidth, imageHeight))
                                                        {
                                                            using (Graphics g = Graphics.FromImage(image))
                                                            {
                                                                for (int x = 0; x <= max_x; x++)
                                                                    for (int y = 0; y < max_y; y++)
                                                                    {
                                                                        g.DrawImage(tiles[(x, y)], new Point(x * tileWidth, y * tileHeight));
                                                                        tiles[(x, y)].Dispose();
                                                                    }

                                                                using (Stream imageStream = new MemoryStream())
                                                                {
                                                                    ImageCodecInfo jpgEncoder = _pictureFileService.GetEncoder(ImageFormat.Jpeg);

                                                                    Encoder myEncoder =
                                                                        Encoder.Quality;
                                                                    EncoderParameters jpegParameters = new EncoderParameters(1);

                                                                    EncoderParameter qualityParameter = new EncoderParameter(myEncoder, 92L);
                                                                    jpegParameters.Param[0] = qualityParameter;

                                                                    image.Save(imageStream, jpgEncoder, jpegParameters);
                                                                    RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, $"http://www.bl.uk/manuscripts/Viewer.aspx?ref={PageUrls[i]}", imageStream, $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                                    if (picture.Result == null)
                                                                    {
                                                                        throw new Exception($"_pictureFileService.Add : {picture.ExceptionString}");
                                                                    }

                                                                    page.Images = new RPictureFile[] { picture.Result };
                                                                    page.CoverImageIndex = 0;

                                                                    if (book.CoverItemIndex == (order - 1))
                                                                    {
                                                                        book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                    }

                                                                    pages.Add(page);
                                                                }
                                                            }
                                                        }

                                                        GC.Collect();



                                                    }
                                                }


                                                book.Items = pages.ToArray();
                                                book.ItemCount = pages.Count;

                                                if (pages.Count == 0)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "Pages.Count == 0";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                await context.Artifacts.AddAsync(book);
                                                await context.SaveChangesAsync();

                                                var resFTPUpload = await _UploadArtifactToExternalServer(book, context, skipUpload);
                                                if (!string.IsNullOrEmpty(resFTPUpload.ExceptionString))
                                                {
                                                    job.EndTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Failed;
                                                    job.Exception = $"UploadArtifactToExternalServer: {resFTPUpload.ExceptionString}";
                                                    job.ArtifactId = book.Id;
                                                    job.EndTime = DateTime.Now;
                                                    context.Update(job);
                                                    await context.SaveChangesAsync();
                                                    return;
                                                }

                                                job.ProgressPercent = 100;
                                                job.Status = ImportJobStatus.Succeeded;
                                                job.ArtifactId = book.Id;
                                                job.EndTime = DateTime.Now;
                                                context.Update(job);
                                                await context.SaveChangesAsync();


                                            }
                                        }
                                        else
                                        {
                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                            {
                                                job.EndTime = DateTime.Now;
                                                job.Status = ImportJobStatus.Failed;
                                                job.Exception = $"Http result is not ok ({result.StatusCode}) for {url}";
                                                importJobUpdaterDb.Update(job);
                                                await importJobUpdaterDb.SaveChangesAsync();
                                            }
                                            return;
                                        }

                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                {
                                    job.EndTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Failed;
                                    job.Exception = exp.ToString();
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                            }
                        }
                    );



                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

    }
}
