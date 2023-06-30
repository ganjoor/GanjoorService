using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.ImportJob;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
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
        /// import from http://www.qajarwomen.org
        /// </summary>
        /// <param name="hardvardResourceNumber">43117279</param>
        /// <param name="friendlyUrl">atame</param>
        /// <param name="srcUrl">http://www.qajarwomen.org/fa/items/1018A10.html</param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromHarvardDirectly(string hardvardResourceNumber, string friendlyUrl, string srcUrl, bool skipUpload)
        {
            try
            {
                if (
                    (
                    await _context.ImportJobs
                        .Where(j => j.JobType == JobType.HarvardDirect && j.ResourceNumber == hardvardResourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                        .SingleOrDefaultAsync()
                    )
                    !=
                    null
                    )
                {
                    return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing harvard direct resource number {hardvardResourceNumber}");
                }

                if (string.IsNullOrEmpty(friendlyUrl))
                {
                    return new RServiceResult<bool>(false, $"Friendly url is empty, harvard direct resource number {hardvardResourceNumber}");
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
                    JobType = JobType.HarvardDirect,
                    ResourceNumber = hardvardResourceNumber,
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
                                    RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from harvard resource number {job.ResourceNumber}", $"extracted from harvard resource number {job.ResourceNumber}")
                                    {
                                        Status = PublishStatus.Draft,
                                        DateTime = DateTime.Now,
                                        LastModified = DateTime.Now,
                                        CoverItemIndex = 0,
                                        FriendlyUrl = friendlyUrl
                                    };


                                    List<RTagValue> meta = new List<RTagValue>();
                                    RTagValue tag;





                                    tag = await TagHandler.PrepareAttribute(context, "Notes", "وارد شده از سایت دنیای زنان در عصر قاجار", 1);
                                    meta.Add(tag);



                                    tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                    meta.Add(tag);

                                    tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                    meta.Add(tag);


                                    tag = await TagHandler.PrepareAttribute(context, "Source", "دنیای زنان در عصر قاجار", 1);
                                    tag.ValueSupplement = $"{job.SrcUrl}";

                                    meta.Add(tag);

                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                    {
                                        job.StartTime = DateTime.Now;
                                        job.Status = ImportJobStatus.Running;
                                        job.SrcContent = $"https://iiif.lib.harvard.edu/manifests/drs:{hardvardResourceNumber}";
                                        importJobUpdaterDb.Update(job);
                                        await importJobUpdaterDb.SaveChangesAsync();
                                    }

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
