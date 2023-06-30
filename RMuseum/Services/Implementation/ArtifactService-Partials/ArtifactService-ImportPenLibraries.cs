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

namespace RMuseum.Services.Implementation
{

    /// <summary>
    /// IArtifactService implementation
    /// </summary>
    public partial class ArtifactService : IArtifactService
    {
        /// <summary>
        /// from http://www.library.upenn.edu/
        /// </summary>
        /// <param name="resourceNumber">MEDREN_9949222153503681</param>
        /// <param name="friendlyUrl"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromPenLibraries(string resourceNumber, string friendlyUrl, bool skipUpload)
        {
            string url = $"http://dla.library.upenn.edu/dla/medren/pageturn.html?id={resourceNumber}&rotation=0&size=0";
            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.PennLibraries && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
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
                JobType = JobType.PennLibraries,
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

                                                string title = "";
                                                string author = "";
                                                int tagOrder = 1;

                                                int nIdxStart = html.IndexOf("https://repo.library.upenn.edu/djatoka/resolver?");
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "https://repo.library.upenn.edu/djatoka/resolver? not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                string firstImageUrl = html.Substring(nIdxStart, html.IndexOf('"', nIdxStart) - nIdxStart).Replace("&amp;", "&");

                                                nIdxStart = html.IndexOf("recordinfolabel");
                                                while (nIdxStart != -1)
                                                {
                                                    nIdxStart += "recordinfolabel\">".Length;
                                                    int nIdxEnd = html.IndexOf(":", nIdxStart);
                                                    string recordinfolabel = html.Substring(nIdxStart, nIdxEnd - nIdxStart);
                                                    nIdxStart = html.IndexOf("recordinfotext", nIdxEnd);
                                                    nIdxStart += "recordinfotext\">".Length;
                                                    nIdxEnd = html.IndexOf("</td>", nIdxStart);
                                                    string recordinfotext = html.Substring(nIdxStart, nIdxEnd - nIdxStart).Replace("</div>", "<div>").Replace("\n", "").Replace("\r", "").Trim();

                                                    string[] values = recordinfotext.Split("<div>", StringSplitOptions.RemoveEmptyEntries);

                                                    foreach (string value in values)
                                                    {
                                                        if (value.Trim().Length == 0)
                                                            continue;
                                                        if (recordinfolabel == "Title")
                                                        {
                                                            title = value.Trim();
                                                            tag = await TagHandler.PrepareAttribute(context, "Title", title, 1);
                                                            meta.Add(tag);
                                                        }
                                                        else
                                                        if (recordinfolabel == "Author")
                                                        {
                                                            author = value.Trim();
                                                            tag = await TagHandler.PrepareAttribute(context, "Contributor Names", author, 1);
                                                            meta.Add(tag);
                                                        }
                                                        else
                                                        {
                                                            tag = await TagHandler.PrepareAttribute(context, recordinfolabel, value.Trim(), tagOrder++);
                                                            meta.Add(tag);
                                                        }
                                                    }

                                                    nIdxStart = html.IndexOf("recordinfolabel", nIdxEnd);

                                                }



                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "Penn Libraries", 1);
                                                string viewerUrl = $"http://dla.library.upenn.edu/dla/medren/detail.html?id={resourceNumber}";
                                                tag.ValueSupplement = viewerUrl;

                                                meta.Add(tag);

                                                book.Name = book.NameInEnglish = book.Description = book.DescriptionInEnglish = title;
                                                book.Tags = meta.ToArray();

                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                int order = 0;
                                                while (true)
                                                {
                                                    order++;

                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.ProgressPercent = order;
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }

                                                    string imageUrl = firstImageUrl;

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

                                                    tag = await TagHandler.PrepareAttribute(context, "Source", "Penn Libraries", 1);
                                                    tag.ValueSupplement = viewerUrl;
                                                    page.Tags = new RTagValue[] { tag };

                                                    if (!string.IsNullOrEmpty(imageUrl))
                                                    {
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
                                                                imageUrl,
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

                                                            if (order > 1)
                                                            {
                                                                string pageUrl = $"http://dla.library.upenn.edu/dla/medren/pageturn.html?id={resourceNumber}&doubleside=0&rotation=0&size=0&currentpage={order}";
                                                                var pageResult = await client.GetAsync(pageUrl);

                                                                if (pageResult.StatusCode == HttpStatusCode.NotFound)
                                                                {
                                                                    break;//finished
                                                                }

                                                                string pageHtml = await pageResult.Content.ReadAsStringAsync();
                                                                nIdxStart = pageHtml.IndexOf("https://repo.library.upenn.edu/djatoka/resolver?");
                                                                if (nIdxStart == -1)
                                                                {
                                                                    if (order > 1)
                                                                        break; //finished
                                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                                    {
                                                                        job.EndTime = DateTime.Now;
                                                                        job.Status = ImportJobStatus.Failed;
                                                                        job.Exception = $"https://repo.library.upenn.edu/djatoka/resolver? not found on page {order}";
                                                                        importJobUpdaterDb.Update(job);
                                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                                    }
                                                                    return;
                                                                }

                                                                imageUrl = pageHtml.Substring(nIdxStart, pageHtml.IndexOf('"', nIdxStart) - nIdxStart).Replace("&amp;", "&");

                                                            }
                                                            var imageResult = await client.GetAsync(imageUrl);

                                                            if (imageResult.StatusCode == HttpStatusCode.NotFound)
                                                            {
                                                                break;//finished
                                                            }


                                                            int _ImportRetryCount = 200;
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
                                                                    RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, imageUrl, imageStream, $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
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

                                                            imageResult.Dispose();
                                                            GC.Collect();
                                                        }
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
