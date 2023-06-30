using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.Artifact.ViewModels;
using RMuseum.Models.Bookmark;
using RMuseum.Models.Bookmark.ViewModels;
using RMuseum.Models.GanjoorIntegration;
using RMuseum.Models.GanjoorIntegration.ViewModels;
using RMuseum.Models.ImportJob;
using RMuseum.Models.Note;
using RMuseum.Models.Note.ViewModels;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
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
        /// from http://cudl.lib.cam.ac.uk
        /// </summary>
        /// <param name="resourceNumber">MS-RAS-00258</param>
        /// <param name="friendlyUrl"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromCambridge(string resourceNumber, string friendlyUrl, bool skipUpload)
        {
            string url = $"http://cudl.lib.cam.ac.uk/view/{resourceNumber}.json";
            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.Cambridge && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
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
                JobType = JobType.Cambridge,
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

                                                string json = await result.Content.ReadAsStringAsync();
                                                var parsed = JObject.Parse(json);
                                                book.Name = book.NameInEnglish =
                                                     parsed.SelectToken("logicalStructures[*].label").Value<string>();

                                                book.Description = book.DescriptionInEnglish =
                                                    Regex.Replace(
                                                     parsed.SelectToken("descriptiveMetadata[*].abstract.displayForm").Value<string>(),
                                                     "<.*?>", string.Empty);

                                                int tagOrder = 1;
                                                foreach (JToken descriptiveMetadata in parsed.SelectTokens("$.descriptiveMetadata[*]").Children())
                                                {
                                                    foreach (JToken child in descriptiveMetadata.Children())
                                                    {
                                                        if (child.SelectToken("label") != null && child.SelectToken("display") != null)
                                                        {
                                                            if (child.SelectToken("display").Value<string>() == "True")
                                                            {
                                                                string metaName = child.SelectToken("label").Value<string>();
                                                                string metaValue = "";
                                                                if (child.SelectToken("displayForm") != null)
                                                                {
                                                                    metaValue = Regex.Replace(
                                                                         child.SelectToken("displayForm").Value<string>(),
                                                                         "<.*?>", string.Empty);
                                                                    tag = await TagHandler.PrepareAttribute(context, metaName, metaValue, tagOrder++);
                                                                    meta.Add(tag);
                                                                }
                                                                else
                                                                    if (child.SelectToken("value") != null)
                                                                {
                                                                    foreach (JToken value in child.SelectTokens("value").Children())
                                                                    {
                                                                        if (value.SelectToken("displayForm") != null)
                                                                        {
                                                                            metaValue = Regex.Replace(
                                                                                 value.SelectToken("displayForm").Value<string>(),
                                                                                 "<.*?>", string.Empty);
                                                                            tag = await TagHandler.PrepareAttribute(context, metaName, metaValue, tagOrder++);
                                                                            meta.Add(tag);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }

                                                }

                                                string imageReproPageURL = "https://image01.cudl.lib.cam.ac.uk";


                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                {
                                                    job.StartTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Running;
                                                    job.SrcContent = json;
                                                    importJobUpdaterDb.Update(job);
                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                }




                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "University of Cambridge Digital Library", 1);
                                                string viewerUrl = $"http://cudl.lib.cam.ac.uk/view/{resourceNumber}";
                                                tag.ValueSupplement = viewerUrl;

                                                meta.Add(tag);

                                                book.Tags = meta.ToArray();

                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                int order = 0;
                                                foreach (JToken pageToken in parsed.SelectTokens("$.pages").Children())
                                                {
                                                    order++;

                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.ProgressPercent = order;
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }

                                                    string imageUrl = imageReproPageURL + pageToken.SelectToken("downloadImageURL").Value<string>();

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

                                                    List<RTagValue> pageMata = new List<RTagValue>();
                                                    tag = await TagHandler.PrepareAttribute(context, "Source", "University of Cambridge Digital Library", 1);
                                                    tag.ValueSupplement = $"{viewerUrl}/{order}";
                                                    pageMata.Add(tag);

                                                    if (pageToken.SelectToken("label") != null)
                                                    {
                                                        tag = await TagHandler.PrepareAttribute(context, "Label", pageToken.SelectToken("label").Value<string>(), 1);
                                                        pageMata.Add(tag);
                                                    }


                                                    page.Tags = pageMata.ToArray();

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
