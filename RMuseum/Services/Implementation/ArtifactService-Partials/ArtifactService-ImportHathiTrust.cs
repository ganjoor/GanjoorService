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
using System.Security.Cryptography;
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
        /// from https://catalog.hathitrust.org
        /// </summary>
        /// <param name="resourceNumber">006814127</param>
        /// <param name="friendlyUrl"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromHathiTrust(string resourceNumber, string friendlyUrl, bool skipUpload)
        {
            string url = $"https://catalog.hathitrust.org/Record/{resourceNumber}.xml";
            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.HathiTrust && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
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
                JobType = JobType.HathiTrust,
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

                                                string xml = await result.Content.ReadAsStringAsync();


                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                {
                                                    job.StartTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Running;
                                                    job.SrcContent = xml;
                                                    importJobUpdaterDb.Update(job);
                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                }

                                                string title = "";
                                                string author = "";
                                                string pdfResourceNumber = "";
                                                int tagOrder = 1;
                                                XElement elObject = XDocument.Parse(xml).Root;
                                                foreach (var datafield in elObject.Element("record").Elements("datafield"))
                                                {
                                                    tagOrder++;
                                                    if (datafield.Attribute("tag") == null)
                                                        continue;
                                                    string hathiTrustTag = datafield.Attribute("tag").Value;
                                                    switch (hathiTrustTag)
                                                    {
                                                        case "245":
                                                        case "246":
                                                            foreach (var subfield in datafield.Elements("subfield"))
                                                            {
                                                                if (subfield.Attribute("code") != null)
                                                                {
                                                                    if (subfield.Attribute("code").Value == "a" || subfield.Attribute("code").Value == "f")
                                                                        title = (title + " " + subfield.Value).Trim();
                                                                }
                                                            }
                                                            break;
                                                        case "100":
                                                            foreach (var subfield in datafield.Elements("subfield"))
                                                            {
                                                                if (subfield.Attribute("code") != null)
                                                                {
                                                                    if (subfield.Attribute("code").Value == "a" || subfield.Attribute("code").Value == "d")
                                                                        author = (author + " " + subfield.Value).Trim();
                                                                }
                                                            }
                                                            break;
                                                        case "HOL":
                                                            foreach (var subfield in datafield.Elements("subfield"))
                                                            {
                                                                if (subfield.Attribute("code") != null)
                                                                {
                                                                    if (subfield.Attribute("code").Value == "p")
                                                                        pdfResourceNumber = subfield.Value;
                                                                }
                                                            }
                                                            break;

                                                        default:
                                                            {
                                                                if (int.TryParse(hathiTrustTag, out int tmp))
                                                                {
                                                                    if (tmp >= 100 && tmp <= 900)
                                                                    {
                                                                        string note = "";
                                                                        foreach (var subfield in datafield.Elements("subfield"))
                                                                        {
                                                                            if (subfield.Attribute("code") != null)
                                                                            {
                                                                                note = (note + " " + subfield.Value).Trim();
                                                                            }
                                                                        }

                                                                        tag = await TagHandler.PrepareAttribute(context, "Notes", note, tagOrder);
                                                                        meta.Add(tag);
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                    }

                                                }

                                                if (string.IsNullOrEmpty(pdfResourceNumber))
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "pdfResourceNumber not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                tag = await TagHandler.PrepareAttribute(context, "Title", title, 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Contributor Names", author, 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "HathiTrust Digital Library", 1);
                                                string viewerUrl = $"https://babel.hathitrust.org/cgi/pt?id={pdfResourceNumber}";
                                                tag.ValueSupplement = viewerUrl;

                                                meta.Add(tag);

                                                book.Name = book.NameInEnglish = book.Description = book.DescriptionInEnglish = title;
                                                book.Tags = meta.ToArray();

                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                string lastMD5hash = "";
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

                                                    string imageUrl = $"https://babel.hathitrust.org/cgi/imgsrv/image?id={pdfResourceNumber};seq={order};size=1000;rotation=0";
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

                                                    tag = await TagHandler.PrepareAttribute(context, "Source", "HathiTrust Digital Library", 1);
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
                                                            var imageResult = await client.GetAsync(imageUrl);


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

                                                                    bool lastPage = false;
                                                                    using (var md5 = MD5.Create())
                                                                    {
                                                                        string md5hash = string.Join("", md5.ComputeHash(File.ReadAllBytes(Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg"))).Select(x => x.ToString("X2")));
                                                                        if (md5hash == lastMD5hash)
                                                                        {
                                                                            File.Delete
                                                                                (
                                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                                );
                                                                            File.Delete
                                                                                (
                                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                                );
                                                                            File.Delete
                                                                                (
                                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                                );
                                                                            lastPage = true;
                                                                        }
                                                                        lastMD5hash = md5hash;
                                                                    }

                                                                    if (!lastPage)
                                                                    {
                                                                        page.Images = new RPictureFile[] { picture.Result };
                                                                        page.CoverImageIndex = 0;

                                                                        if (book.CoverItemIndex == (order - 1))
                                                                        {
                                                                            book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                        }

                                                                        pages.Add(page);
                                                                    }
                                                                    else
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
                                                        job.Exception = "ages.Count == 0";
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
