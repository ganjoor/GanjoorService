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
        /// from https://www.loc.gov
        /// </summary>
        /// <param name="resourceNumber">
        /// <example>
        /// m084
        /// </example>
        /// </param>
        /// <param name="friendlyUrl">
        /// <example>
        /// boostan1207
        /// </example>
        /// </param>
        /// <param name="resourcePrefix"></param>
        /// <example>
        /// plmp
        /// </example>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromTheLibraryOfCongress(string resourceNumber, string friendlyUrl, string resourcePrefix, bool skipUpload)
        {
            string url = $"https://www.loc.gov/resource/{resourcePrefix}.{resourceNumber}/?fo=json&st=gallery";

            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.Loc && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
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
                JobType = JobType.Loc,
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
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                {
                                    job.StartTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Running;
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                                int pageCount = 0;
                                int representative_index = 0;
                                //اول یک صفحه را می‌خوانیم تا تعداد صفحات را مشخص کنیم
                                using (var client = new HttpClient())
                                {

                                    using (var result = await client.GetAsync(url))
                                    {
                                        if (result.IsSuccessStatusCode)
                                        {
                                            string json = await result.Content.ReadAsStringAsync();
                                            var parsed = JObject.Parse(json);

                                            pageCount = parsed.SelectToken("resource.segment_count").Value<int>();
                                            representative_index = parsed.SelectToken("resource.representative_index").Value<int>();
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

                                //here might be problems: loc json does not return correct answer when number of segments are more than 1000
                                /*
                                if (pageCount > 1000)
                                {
                                    job.Exception = $"Page count ({pageCount}) was cut to 1000 for this artifact due to loc bug.";
                                    pageCount = 1000;
                                }
                                */

                                //حالا که تعداد صفحات را داریم دوباره می‌خوانیم
                                url = $"https://www.loc.gov/resource/{resourcePrefix}.{resourceNumber}/?c={pageCount}&fo=json&st=gallery";
                                using (var client = new HttpClient())
                                {

                                    using (var result = await client.GetAsync(url))
                                    {
                                        if (result.IsSuccessStatusCode)
                                        {

                                            //here is a problem, this method could be called from a background service where _context is disposed, so I need to renew it
                                            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                            {
                                                RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from {url}", $"extracted from {url}")
                                                {
                                                    Status = PublishStatus.Draft,
                                                    DateTime = DateTime.Now,
                                                    LastModified = DateTime.Now,
                                                    CoverItemIndex = representative_index,
                                                    FriendlyUrl = friendlyUrl
                                                };

                                                string json = await result.Content.ReadAsStringAsync();

                                                job.SrcContent = json;

                                                var parsed = JObject.Parse(json);

                                                var segmentsArray = parsed.SelectToken("segments").ToArray();
                                                //here might be problems: loc json does not return correct answer when number of segments are more than 1000
                                                //I've added some temporary solutions prior
                                                //Here I want to log any paradox I encounter:
                                                if (segmentsArray.Length != pageCount)
                                                {
                                                    job.Exception = $"Page count ({pageCount}) is not equal to number of returned resources ({segmentsArray.Length}).";
                                                }



                                                List<RTagValue> meta = new List<RTagValue>();

                                                string string_value = await HandleSimpleValue(context, parsed, meta, "item.title", "Title");
                                                if (!string.IsNullOrWhiteSpace(string_value))
                                                {
                                                    book.Name = string_value;
                                                    book.NameInEnglish = string_value;
                                                }
                                                await HandleSimpleValue(context, parsed, meta, "item.date", "Date");
                                                string_value = await HandleListValue(context, parsed, meta, "item.other_title", "Other Title");
                                                if (!string.IsNullOrWhiteSpace(string_value))
                                                {
                                                    book.Name = string_value;
                                                }
                                                await HandleListValue(context, parsed, meta, "item.contributor_names", "Contributor Names");
                                                await HandleSimpleValue(context, parsed, meta, "item.shelf_id", "Shelf ID");
                                                await HandleListValue(context, parsed, meta, "item.created_published", "Created / Published");
                                                await HandleListValue(context, parsed, meta, "item.subject_headings", "Subject Headings");
                                                await HandleListValue(context, parsed, meta, "item.notes", "Notes");
                                                await HandleListValue(context, parsed, meta, "item.medium", "Medium");
                                                await HandleListValue(context, parsed, meta, "item.call_number", "Call Number/Physical Location");
                                                await HandleListValue(context, parsed, meta, "item.digital_id", "Digital Id");
                                                await HandleSimpleValue(context, parsed, meta, "item.library_of_congress_control_number", "Library of Congress Control Number");
                                                await HandleChildrenValue(context, parsed, meta, "item.language", "Language");
                                                await HandleListValue(context, parsed, meta, "item.online_format", "Online Format");
                                                await HandleListValue(context, parsed, meta, "item.number_oclc", "OCLC Number");
                                                string_value = await HandleListValue(context, parsed, meta, "item.description", "Description");
                                                if (!string.IsNullOrEmpty(string_value))
                                                {
                                                    book.Description = string_value;
                                                    book.DescriptionInEnglish = string_value;
                                                }
                                                await HandleSimpleValue(context, parsed, meta, "cite_this.chicago", "Chicago citation style");
                                                await HandleSimpleValue(context, parsed, meta, "cite_this.apa", "APA citation style");
                                                await HandleSimpleValue(context, parsed, meta, "cite_this.mla", "MLA citation style");
                                                await HandleChildrenValue(context, parsed, meta, "item.dates", "Dates");
                                                await HandleChildrenValue(context, parsed, meta, "item.contributors", "Contributors");
                                                await HandleChildrenValue(context, parsed, meta, "item.location", "Location");
                                                await HandleListValue(context, parsed, meta, "item.rights", "Rights & Access");

                                                RTagValue tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "Library of Congress, African and Middle East Division, Near East Section Persian Manuscript Collection", 1);
                                                tag.ValueSupplement = url;
                                                string_value = parsed.SelectToken("item.id").Value<string>();
                                                if (!string.IsNullOrWhiteSpace(string_value))
                                                {
                                                    tag.ValueSupplement = string_value;
                                                }

                                                meta.Add(tag);


                                                book.Tags = meta.ToArray();




                                                int order = 0;
                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                //due to loc bug for books with more than 1000 pages relying on segmentsArray changed to hard coded image urls and ....
                                                //foreach (JToken segment in segmentsArray)
                                                for (int pageIndex = 1; pageIndex <= pageCount; pageIndex++)
                                                {
                                                   
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                                    {
                                                        job.ProgressPercent = order * 100 / (decimal)pageCount;
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }

                                                    order++;



                                                    RArtifactItemRecord page = new RArtifactItemRecord()
                                                    {
                                                        Name = $"تصویر {order}",
                                                        NameInEnglish = $"Image {pageIndex} of {book.NameInEnglish}",//segment.SelectToken("title").Value<string>(),
                                                        Description = "",
                                                        DescriptionInEnglish = "",
                                                        Order = order,
                                                        FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                                        LastModified = DateTime.Now
                                                    };

                                                    tag = await TagHandler.PrepareAttribute(context, "Source", "Library of Congress, African and Middle East Division, Near East Section Persian Manuscript Collection", 1);
                                                    tag.ValueSupplement = $"http://www.loc.gov/resource/{resourcePrefix}.{resourceNumber}/?sp={pageIndex}";//segment.SelectToken("id").Value<string>();
                                                    page.Tags = new RTagValue[] { tag };

                                                    string imageUrlPart = $"{pageIndex}".PadLeft(4, '0');
                                                    string imageUrl = $"https://tile.loc.gov/image-services/iiif/service:amed:{resourcePrefix}:{resourceNumber}:{imageUrlPart}/full/pct:100/0/default.jpg";
                                                    //string imageUrl = $"https://tile.loc.gov/image-services/iiif/service:rbc:{resourcePrefix}:2015:{resourceNumber}:{imageUrlPart}/full/pct:100/0/default.jpg";
                                                    /*
                                                    List<string> list = segment.SelectToken("image_url").ToObject<List<string>>();
                                                    if (list != null && list.Count > 0)
                                                    {
                                                        for (int i = 0; i < list.Count; i++)
                                                        {
                                                            if (list[i].IndexOf(".jpg") != -1)
                                                            {
                                                                if (imageUrl == "")
                                                                    imageUrl = list[i];
                                                                else
                                                                {
                                                                    if (imageUrl.IndexOf("#h=") != -1 && imageUrl.IndexOf("&w=", imageUrl.IndexOf("#h=")) != -1)
                                                                    {
                                                                        int h1 = int.Parse(imageUrl.Substring(imageUrl.IndexOf("#h=") + "#h=".Length, imageUrl.IndexOf("&w=") - imageUrl.IndexOf("#h=") - "&w=".Length));
                                                                        if (list[i].IndexOf("#h=") != -1 && list[i].IndexOf("&w=", list[i].IndexOf("#h=")) != -1)
                                                                        {
                                                                            int h2 = int.Parse(list[i].Substring(list[i].IndexOf("#h=") + "#h=".Length, list[i].IndexOf("&w=") - list[i].IndexOf("#h=") - "&w=".Length));

                                                                            if (h2 > h1)
                                                                            {
                                                                                imageUrl = list[i];
                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                        imageUrl = list[i];

                                                                }
                                                            }
                                                        }
                                                    }
                                                    */



                                                    if (!string.IsNullOrEmpty(imageUrl))
                                                    {
                                                        //imageUrl = "https:" + imageUrl.Substring(0, imageUrl.IndexOf('#'));
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

                                                book.Items = pages.ToArray();
                                                book.ItemCount = pages.Count;

                                                if (book.CoverImage == null && pages.Count > 0)
                                                {
                                                    book.CoverImage = RPictureFile.Duplicate(pages[0].Images.First());
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
        /// <summary>
        /// due to a bug in loc json outputs some artifacts with more than 1000 pages were downloaded incompletely
        /// </summary>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        public async Task<RServiceResult<string[]>> ReExamineLocDownloads(bool skipUpload)
        {
            try
            {
                ImportJob[] jobs = await _context.ImportJobs
                    .Include(j => j.Artifact)
                    .Where(j => j.Status == ImportJobStatus.Succeeded && j.JobType == JobType.Loc).ToArrayAsync();

                List<string> scheduled = new List<string>();
                List<ImportJob> rescheduledJobs = new List<ImportJob>();
                foreach (ImportJob job in jobs)
                {

                    int pageCount = 0;
                    //اول یک صفحه را می‌خوانیم تا تعداد صفحات را مشخص کنیم
                    using (var client = new HttpClient())
                    {
                        if (job.Artifact == null)
                            continue;

                        string url = $"https://www.loc.gov/resource/rbc0001.{job.ResourceNumber}/?fo=json&st=gallery"; //plmp

                        var result = await client.GetAsync(url);
                        //using (var result = await client.GetAsync(url))
                        {
                            int _ImportRetryCount = 5;
                            int _ImportRetryInitialSleep = 500;
                            int retryCount = 0;
                            while (retryCount < _ImportRetryCount && !result.IsSuccessStatusCode && result.StatusCode == HttpStatusCode.ServiceUnavailable)
                            {
                                result.Dispose();
                                Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                result = await client.GetAsync(url);
                                retryCount++;
                            }

                            if (result.IsSuccessStatusCode)
                            {
                                string json = await result.Content.ReadAsStringAsync();
                                var parsed = JObject.Parse(json);

                                pageCount = parsed.SelectToken("resource.segment_count").Value<int>();
                            }
                            else
                            {
                                return new RServiceResult<string[]>(null, $"{job.ResourceNumber}: Http result is not ok ({result.StatusCode}) for {url}");
                            }

                            if (pageCount != job.Artifact.ItemCount)
                            {
                                if (scheduled.IndexOf(job.ResourceNumber) == -1)
                                {
                                    scheduled.Add(job.ResourceNumber);
                                    rescheduledJobs.Add(job);

                                }
                            }
                            result.Dispose();
                        }

                    }
                }

                scheduled = new List<string>();
                foreach (ImportJob job in rescheduledJobs)
                {
                    await RemoveArtifact((Guid)job.ArtifactId, false);
                    _context.ImportJobs.Remove(job);
                    await _context.SaveChangesAsync();
                    RServiceResult<bool> rescheduled = await StartImportingFromTheLibraryOfCongress(job.ResourceNumber, job.FriendlyUrl, "rbc0001", skipUpload);//plmp
                    if (rescheduled.Result)
                    {

                        scheduled.Add(job.ResourceNumber);
                    }
                }
                return new RServiceResult<string[]>(scheduled.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<string[]>(null, exp.ToString());
            }
        }
    }
}
