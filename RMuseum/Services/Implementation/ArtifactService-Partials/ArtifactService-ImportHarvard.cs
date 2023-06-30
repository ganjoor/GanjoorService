using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
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
        /// from https://curiosity.lib.harvard.edu
        /// </summary>
        /// <param name="url">example: https://curiosity.lib.harvard.edu/islamic-heritage-project/catalog/40-990114893240203941</param>
        /// <param name="friendlyUrl"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromHarvard(string url, string friendlyUrl, bool skipUpload)
        {
            try
            {
                if (
                    (
                    await _context.ImportJobs
                        .Where(j => j.JobType == JobType.Harvard && j.ResourceNumber == url && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
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
                    return new RServiceResult<bool>(false, $"Friendly url is empty, url = {url}");
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
                    JobType = JobType.Harvard,
                    ResourceNumber = url,
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

                                                string html = await result.Content.ReadAsStringAsync();


                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                {
                                                    job.StartTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Running;
                                                    job.SrcContent = html;
                                                    importJobUpdaterDb.Update(job);
                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                }

                                                int nStartIndex = html.IndexOf("<dt");
                                                while (nStartIndex != -1)
                                                {
                                                    nStartIndex = html.IndexOf(">", nStartIndex);
                                                    if (nStartIndex == -1)
                                                        break;
                                                    nStartIndex++;
                                                    string tagName = html.Substring(nStartIndex, html.IndexOf(":", nStartIndex) - nStartIndex);
                                                    nStartIndex = html.IndexOf("<dd", nStartIndex);
                                                    if (nStartIndex == -1)
                                                        break;
                                                    nStartIndex = html.IndexOf(">", nStartIndex);
                                                    if (nStartIndex == -1)
                                                        break;
                                                    nStartIndex++;
                                                    string tagValues = html.Substring(nStartIndex, html.IndexOf("</dd>", nStartIndex) - nStartIndex);
                                                    foreach (string tagValuePart in tagValues.Split("<br/>", StringSplitOptions.RemoveEmptyEntries))
                                                    {
                                                        string tagValue = tagValuePart;
                                                        bool href = false;
                                                        if (tagValue.IndexOf("<a href=") != -1)
                                                        {
                                                            href = true;
                                                            tagValue = tagValue.Substring(tagValue.IndexOf('>') + 1);
                                                            tagValue = tagValue.Substring(0, tagValue.IndexOf('<'));
                                                        }
                                                        tag = await TagHandler.PrepareAttribute(context, tagName, tagValue, 1);
                                                        if (href)
                                                            tag.ValueSupplement = tagValue;
                                                        meta.Add(tag);
                                                    }

                                                    nStartIndex = html.IndexOf("<dt", nStartIndex + 1);
                                                }




                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "Harvard University Islamic Heritage Project", 1);
                                                tag.ValueSupplement = $"{job.SrcUrl}";

                                                meta.Add(tag);

                                                nStartIndex = html.IndexOf("https://pds.lib.harvard.edu/pds/view/");
                                                if (nStartIndex == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "Not found https://pds.lib.harvard.edu/pds/view/";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nStartIndex += "https://pds.lib.harvard.edu/pds/view/".Length;

                                                string hardvardResourceNumber = html.Substring(nStartIndex, html.IndexOf('\"', nStartIndex) - nStartIndex);

                                                List<RArtifactItemRecord> pages = (await _InternalHarvardJsonImport(hardvardResourceNumber, job, friendlyUrl, context, book, meta)).Result;
                                                if (pages == null)
                                                {
                                                    return;
                                                }


                                                book.Tags = meta.ToArray();

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

        private async Task<RServiceResult<List<RArtifactItemRecord>>> _InternalHarvardJsonImport(string hardvardResourceNumber, ImportJob job, string friendlyUrl, RMuseumDbContext context, RArtifactMasterRecord book, List<RTagValue> meta)
        {
            List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

            using (var client = new HttpClient())
            {
                using (var jsonResult = await client.GetAsync($"https://iiif.lib.harvard.edu/manifests/drs:{hardvardResourceNumber}"))
                {
                    if (jsonResult.IsSuccessStatusCode)
                    {
                        string json = await jsonResult.Content.ReadAsStringAsync();
                        var parsed = JObject.Parse(json);
                        book.Name = book.NameInEnglish = book.Description = book.DescriptionInEnglish =
                            parsed.SelectToken("label").Value<string>();


                        RTagValue tag;

                        tag = await TagHandler.PrepareAttribute(context, "Title", book.Name, 1);
                        meta.Add(tag);

                        tag = await TagHandler.PrepareAttribute(context, "Contributor Names", "تعیین نشده", 1);
                        meta.Add(tag);

                        List<string> labels = new List<string>();
                        foreach (JToken structure in parsed.SelectTokens("$.structures[*].label"))
                        {
                            labels.Add(structure.Value<string>());
                        }

                        int order = 0;
                        var canvases = parsed.SelectToken("sequences").First().SelectToken("canvases").ToArray();
                        int pageCount = canvases.Length;
                        foreach (JToken canvas in canvases)
                        {
                            order++;
                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                            {
                                job.ProgressPercent = order * 100 / (decimal)pageCount;
                                importJobUpdaterDb.Update(job);
                                await importJobUpdaterDb.SaveChangesAsync();
                            }
                            string label = canvas.SelectToken("label").Value<string>();
                            if (labels.Where(l => l.IndexOf(label) != -1).SingleOrDefault() != null)
                                label = labels.Where(l => l.IndexOf(label) != -1).SingleOrDefault();
                            string imageUrl = canvas.SelectTokens("images[*]").First().SelectToken("resource").SelectToken("@id").Value<string>();
                            RArtifactItemRecord page = new RArtifactItemRecord()
                            {
                                Name = $"تصویر {order}",
                                NameInEnglish = label,
                                Description = "",
                                DescriptionInEnglish = "",
                                Order = order,
                                FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                LastModified = DateTime.Now
                            };



                            tag = await TagHandler.PrepareAttribute(context, "Source", "Harvard University Islamic Heritage Project", 1);
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
                                        return new RServiceResult<List<RArtifactItemRecord>>(null, "failed");
                                    }

                                    imageResult.Dispose();
                                    GC.Collect();
                                }
                            }
                        }
                    }
                    else
                    {
                        using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                        {
                            job.EndTime = DateTime.Now;
                            job.Status = ImportJobStatus.Failed;
                            job.Exception = $"Http result is not ok ({jsonResult.StatusCode}) for https://iiif.lib.harvard.edu/manifests/drs:{hardvardResourceNumber}";
                            importJobUpdaterDb.Update(job);
                            await importJobUpdaterDb.SaveChangesAsync();
                        }
                        return new RServiceResult<List<RArtifactItemRecord>>(null, "failed");
                    }
                }
            }

            return new RServiceResult<List<RArtifactItemRecord>>(pages);


        }

    }
}
