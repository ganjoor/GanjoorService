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
        /// from http://pudl.princeton.edu/
        /// </summary>
        /// <param name="resourceNumber">dj52w476m</param>
        /// <param name="friendlyUrl"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromPrinceton(string resourceNumber, string friendlyUrl, bool skipUpload)
        {
            string url = $"http://pudl.princeton.edu/mdCompiler2.php?obj={resourceNumber}";
            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.Princeton && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
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
                JobType = JobType.Princeton,
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
                                                foreach (var prop in elObject.Element("dmd").Element("properties").Elements("property"))
                                                {
                                                    if (prop.Element("label") == null)
                                                        continue;
                                                    string label = prop.Element("label").Value.Replace(":", "");
                                                    int order = 1;
                                                    foreach (var value in prop.Elements("valueGrp").Elements("value"))
                                                    {
                                                        tag = await TagHandler.PrepareAttribute(context, label, value.Value, order);
                                                        if (value.Attribute("href") != null)
                                                        {
                                                            if (value.Attribute("href").Value.IndexOf("http://localhost") != 0)
                                                            {
                                                                tag.ValueSupplement = value.Attribute("href").Value;
                                                            }
                                                        }
                                                        meta.Add(tag);

                                                        if (label == "Title")
                                                        {
                                                            book.Name = book.NameInEnglish =
                                                            book.Description = book.DescriptionInEnglish =
                                                            value.Value;
                                                        }
                                                        order++;
                                                    }
                                                }

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "Princeton Digital Library of Islamic Manuscripts", 1);
                                                tag.ValueSupplement = $"http://pudl.princeton.edu/objects/{job.ResourceNumber}";

                                                meta.Add(tag);


                                                book.Tags = meta.ToArray();
                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                foreach (var structure in elObject.Elements("structure"))
                                                {
                                                    if (structure.Attribute("type") != null && structure.Attribute("type").Value == "RelatedObjects")
                                                    {
                                                        if (structure.Element("div") == null || structure.Element("div").Element("OrderedList") == null)
                                                        {
                                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                            {
                                                                job.EndTime = DateTime.Now;
                                                                job.Status = ImportJobStatus.Failed;
                                                                job.Exception = "structure[RelatedObjects].div.OrderedList is null";
                                                                importJobUpdaterDb.Update(job);
                                                                await importJobUpdaterDb.SaveChangesAsync();
                                                                return;
                                                            }
                                                        }


                                                        int pageCount = structure.Element("div").Element("OrderedList").Elements("div").Count();
                                                        int inlineOrder = 0;

                                                        foreach (var div in structure.Element("div").Element("OrderedList").Elements("div"))
                                                        {
                                                            inlineOrder++;
                                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                            {
                                                                job.ProgressPercent = inlineOrder * 100 / (decimal)pageCount;
                                                                importJobUpdaterDb.Update(job);
                                                                await importJobUpdaterDb.SaveChangesAsync();
                                                            }

                                                            int order = int.Parse(div.Attribute("order").Value);
                                                            RArtifactItemRecord page = new RArtifactItemRecord()
                                                            {
                                                                Name = $"تصویر {order}",
                                                                NameInEnglish = div.Attribute("label").Value,
                                                                Description = "",
                                                                DescriptionInEnglish = "",
                                                                Order = order,
                                                                FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                                                LastModified = DateTime.Now
                                                            };

                                                            string imageUrl = div.Attribute("img").Value;
                                                            imageUrl = "https://libimages.princeton.edu/loris/" + imageUrl.Substring(imageUrl.LastIndexOf(":") + 1);
                                                            imageUrl += $"/full/,{div.Attribute("h").Value}/0/default.jpg";

                                                            tag = await TagHandler.PrepareAttribute(context, "Source", "Princeton Digital Library of Islamic Manuscripts", 1);
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
                                                }

                                                foreach (var structure in elObject.Elements("structure"))
                                                {
                                                    if (structure.Attribute("type") != null && structure.Attribute("type").Value == "Physical")
                                                    {
                                                        if (structure.Element("RTLBoundManuscript") != null)
                                                        {
                                                            foreach (var leaf in structure.Element("RTLBoundManuscript").Elements("Leaf"))
                                                            {
                                                                foreach (var side in leaf.Elements("Side"))
                                                                {
                                                                    int pageOrder = int.Parse(side.Attribute("order").Value);
                                                                    tag = await TagHandler.PrepareAttribute(context, "Leaf Side", side.Attribute("label").Value, 100);
                                                                    RArtifactItemRecord page = pages.Where(p => p.Order == pageOrder).SingleOrDefault();
                                                                    if (page != null)
                                                                    {
                                                                        List<RTagValue> tags = new List<RTagValue>(page.Tags);
                                                                        tags.Add(tag);
                                                                        page.Tags = tags;
                                                                    }
                                                                }
                                                            }
                                                            foreach (var folio in structure.Element("RTLBoundManuscript").Elements("Folio"))
                                                            {
                                                                foreach (var side in folio.Elements("Side"))
                                                                {
                                                                    int pageOrder = int.Parse(side.Attribute("order").Value);
                                                                    tag = await TagHandler.PrepareAttribute(context, "Folio Side", folio.Attribute("label").Value + ":" + side.Attribute("label").Value, 101);
                                                                    RArtifactItemRecord page = pages.Where(p => p.Order == pageOrder).SingleOrDefault();
                                                                    if (page != null)
                                                                    {
                                                                        List<RTagValue> tags = new List<RTagValue>(page.Tags);
                                                                        tags.Add(tag);
                                                                        page.Tags = tags;
                                                                    }
                                                                }
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
                                                        job.Exception = "ages.Count == 0";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                await context.Artifacts.AddAsync(book);
                                                await context.SaveChangesAsync();

                                                var resFTPUpload = await _UploadArtifactToExternalServer(book, context, skipUpload);
                                                if(!string.IsNullOrEmpty(resFTPUpload.ExceptionString))
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
