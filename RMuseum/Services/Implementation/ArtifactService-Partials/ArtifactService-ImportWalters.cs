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
using System.Xml.Linq;

namespace RMuseum.Services.Implementation
{

    /// <summary>
    /// IArtifactService implementation
    /// </summary>
    public partial class ArtifactService : IArtifactService
    {
        /// <summary>
        /// from http://www.thedigitalwalters.org/01_ACCESS_WALTERS_MANUSCRIPTS.html
        /// </summary>
        /// <param name="resourceNumber">W619</param>
        /// <param name="friendlyUrl">golestan-walters-01</param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromWalters(string resourceNumber, string friendlyUrl, bool skipUpload)
        {
            string url = $"http://www.thedigitalwalters.org/Data/WaltersManuscripts/ManuscriptDescriptions/{resourceNumber}_tei.xml";
            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.Walters && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
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
                return new RServiceResult<bool>(false, $"duplicated friendly url '{friendlyUrl}'");
            }

            ImportJob job = new ImportJob()
            {
                JobType = JobType.Walters,
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

                                                string xml = await result.Content.ReadAsStringAsync();


                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                {
                                                    job.StartTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Running;
                                                    job.SrcContent = xml;
                                                    importJobUpdaterDb.Update(job);
                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                }

                                                XElement elObject = XDocument.Parse(xml).Root;

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);






                                                try
                                                {
                                                    foreach (var prop in elObject
                                                        .Elements("{http://www.tei-c.org/ns/1.0}teiHeader").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}fileDesc").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}titleStmt").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}title"))
                                                    {
                                                        string label = prop.Value;
                                                        book.Name = book.NameInEnglish =
                                                            book.Description = book.DescriptionInEnglish =
                                                            label;

                                                        tag = await TagHandler.PrepareAttribute(context, "Title", label, 1);
                                                        meta.Add(tag);
                                                        break;
                                                    }
                                                }
                                                catch
                                                {
                                                    //ignore non-existing = null tags
                                                }

                                                try
                                                {
                                                    foreach (var prop in elObject
                                                        .Elements("{http://www.tei-c.org/ns/1.0}teiHeader").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}fileDesc").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}titleStmt").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}author"))
                                                    {
                                                        string label = prop.Value;
                                                        tag = await TagHandler.PrepareAttribute(context, "Contributor Names", label, 1);
                                                        meta.Add(tag);
                                                        break;
                                                    }
                                                }
                                                catch
                                                {
                                                    //ignore non-existing = null tags
                                                }

                                                try
                                                {
                                                    foreach (var prop in elObject
                                                        .Elements("{http://www.tei-c.org/ns/1.0}teiHeader").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}fileDesc").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}titleStmt").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}respStmt"))
                                                    {
                                                        string label = prop.Elements("{http://www.tei-c.org/ns/1.0}name").First().Value;
                                                        tag = await TagHandler.PrepareAttribute(context, "Contributor Names", label, 1);
                                                        meta.Add(tag);
                                                        break;

                                                    }
                                                }
                                                catch
                                                {
                                                    //ignore non-existing = null tags
                                                }

                                                try
                                                {
                                                    foreach (var prop in elObject
                                                        .Elements("{http://www.tei-c.org/ns/1.0}teiHeader").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}fileDesc").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}notesStmt").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}note"))
                                                    {
                                                        string label = prop.Value;
                                                        tag = await TagHandler.PrepareAttribute(context, "Notes", label, 1);
                                                        meta.Add(tag);
                                                    }
                                                }
                                                catch
                                                {
                                                    //ignore non-existing = null tags
                                                }




                                                tag = await TagHandler.PrepareAttribute(context, "Source", "Digitized Walters Manuscripts", 1);
                                                tag.ValueSupplement = $"http://www.thedigitalwalters.org/Data/WaltersManuscripts/html/{job.ResourceNumber}/";

                                                meta.Add(tag);


                                                book.Tags = meta.ToArray();
                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                int order = 0;

                                                foreach (var surface in elObject
                                                        .Elements("{http://www.tei-c.org/ns/1.0}facsimile").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}surface"))
                                                {
                                                    foreach (var graphic in surface.Elements("{http://www.tei-c.org/ns/1.0}graphic"))
                                                        if (graphic.Attribute("url").Value.Contains("sap.jpg"))
                                                        {

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



                                                            string imageUrl = $"http://www.thedigitalwalters.org/Data/WaltersManuscripts/{resourceNumber}/data/W.{resourceNumber.Substring(1)}/{graphic.Attribute("url").Value}";

                                                            tag = await TagHandler.PrepareAttribute(context, "Source", "Digitized Walters Manuscripts", 1);
                                                            tag.ValueSupplement = imageUrl;
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
