using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.ImportJob;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{

    /// <summary>
    /// IArtifactService implementation
    /// </summary>
    public partial class ArtifactService : IArtifactService
    {
        /// <summary>
        /// import from server folder
        /// </summary>
        /// <param name="folderPath">C:\Tools\batches\florence</param>
        /// <param name="friendlyUrl">shahname-florence</param>
        /// <param name="srcUrl">https://t.me/dr_khatibi_abolfazl/888</param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromServerFolder(string folderPath, string friendlyUrl, string srcUrl, bool skipUpload)
        {
            try
            {
                if (
                    (
                    await _context.ImportJobs
                        .Where(j => j.JobType == JobType.ServerFolder && j.ResourceNumber == folderPath && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                        .SingleOrDefaultAsync()
                    )
                    !=
                    null
                    )
                {
                    return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing server folder {folderPath}");
                }

                if (string.IsNullOrEmpty(friendlyUrl))
                {
                    return new RServiceResult<bool>(false, $"Friendly url is empty, server folder {folderPath}");
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
                    JobType = JobType.ServerFolder,
                    ResourceNumber = folderPath,
                    FriendlyUrl = friendlyUrl,
                    SrcUrl = srcUrl,
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
                                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                {
                                    RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from server folder {job.ResourceNumber}", $"extracted from server folder {job.ResourceNumber}")
                                    {
                                        Status = PublishStatus.Draft,
                                        DateTime = DateTime.Now,
                                        LastModified = DateTime.Now,
                                        CoverItemIndex = 0,
                                        FriendlyUrl = friendlyUrl
                                    };


                                    List<RTagValue> meta = new List<RTagValue>();
                                    RTagValue tag;


                                    tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                    meta.Add(tag);

                                    tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                    meta.Add(tag);

                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                    {
                                        job.StartTime = DateTime.Now;
                                        job.Status = ImportJobStatus.Running;
                                        job.SrcContent = "";
                                        importJobUpdaterDb.Update(job);
                                        await importJobUpdaterDb.SaveChangesAsync();
                                    }

                                    List<RArtifactItemRecord> pages = await _ImportAndReturnServerFolderJobImages(book, job, 0);

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
        /// start appending from server folder
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="artifactId"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartAppendingFromServerFolder(string folderPath, Guid artifactId, bool skipUpload)
        {
            try
            {

                var nonTrackingBook = await _context.Artifacts.AsNoTracking().Where(b => b.Id == artifactId).SingleOrDefaultAsync();
                if (nonTrackingBook == null)
                    return new RServiceResult<bool>(false, "artifact not found!");

                ImportJob job = new ImportJob()
                {
                    JobType = JobType.AppendFromServerFolder,
                    ResourceNumber = folderPath,
                    FriendlyUrl = nonTrackingBook.FriendlyUrl,
                    SrcUrl = "",
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
                                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                {
                                    var book = await context.Artifacts.Include(a => a.Items).Where(b => b.Id == artifactId).SingleOrDefaultAsync();

                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                    {
                                        job.StartTime = DateTime.Now;
                                        job.Status = ImportJobStatus.Running;
                                        job.SrcContent = "";
                                        importJobUpdaterDb.Update(job);
                                        await importJobUpdaterDb.SaveChangesAsync();
                                    }

                                    List<RArtifactItemRecord> pages = await _ImportAndReturnServerFolderJobImages(book, job, book.ItemCount);

                                    pages.InsertRange(0, book.Items);

                                    book.Items = pages.ToArray();
                                    book.ItemCount = pages.Count;

                                    context.Update(book);
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


        private async Task<List<RArtifactItemRecord>> _ImportAndReturnServerFolderJobImages(RArtifactMasterRecord book, ImportJob job, int order)
        {
            List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();
            string[] fileNames = Directory.GetFiles(job.ResourceNumber, "*.jpg");
            
            foreach (string fileName in fileNames)
            {
                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                {
                    job.ProgressPercent = order * 100 / (decimal)fileNames.Length;
                    importJobUpdaterDb.Update(job);
                    await importJobUpdaterDb.SaveChangesAsync();
                }

                order++;

                RArtifactItemRecord page = new RArtifactItemRecord()
                {
                    Name = $"تصویر {order}",
                    NameInEnglish = $"Image {order} of {book.NameInEnglish}",
                    Description = "",
                    DescriptionInEnglish = "",
                    Order = order,
                    FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                    LastModified = DateTime.Now
                };


                page.Tags = new RTagValue[] { };

                if (
                File.Exists
                (
                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, book.FriendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                )

                                   )
                {
                    File.Delete
                   (
                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, book.FriendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                   );
                }
                if (

                   File.Exists
                   (
                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, book.FriendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                   )

               )
                {
                    File.Delete
                    (
                    Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, book.FriendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                    );
                }
                if (

                   File.Exists
                   (
                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, book.FriendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                   )
               )
                {
                    File.Delete
                    (
                    Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, book.FriendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                    );
                }
                using (FileStream imageStream = new FileStream(fileName, FileMode.Open))
                {
                    RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, job.SrcUrl, imageStream, $"{order}".PadLeft(4, '0') + ".jpg", book.FriendlyUrl);
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

                }


                pages.Add(page);
            }

            return pages;
        }
    }
}
