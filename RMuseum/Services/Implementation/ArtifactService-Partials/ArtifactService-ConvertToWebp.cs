using ImageProcessor;
using ImageProcessor.Plugins.WebP.Imaging.Formats;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.IO;
using System.Linq;

namespace RMuseum.Services.Implementation
{

    /// <summary>
    /// IArtifactService implementation
    /// </summary>
    public partial class ArtifactService : IArtifactService
    {
        /// <summary>
        /// convert original images to webp
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartConvertingOriginalImagesToWebp()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                        (
                        async token =>
                        {
                            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                            {
                                LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                var job = (await jobProgressServiceEF.NewJob($"Converting Original Images To Webp", "Query data")).Result;
                                try
                                {
                                    string imageStoragePath = $"{Configuration.GetSection("PictureFileService")["StoragePath"]}";

                                    var artifacts = await context.Artifacts.Include(a => a.Items).ThenInclude(i => i.Images).AsNoTracking().ToArrayAsync();

                                    int artifactNum = 0;
                                    foreach (var artifact in artifacts)
                                    {
                                        await jobProgressServiceEF.UpdateJob(job.Id, ++artifactNum, $"from {artifact.ItemCount} processing {artifact.Name}");
                                        foreach (var item in artifact.Items)
                                        {
                                            foreach (var image in item.Images)
                                            {
                                                if(image.ContentType == "image/jpeg")
                                                {
                                                    var imagePath = Path.Combine(imageStoragePath, image.FolderName, image.StoredFileName);
                                                    var webpPath = Path.Combine(imageStoragePath, image.FolderName,$"{Path.GetFileNameWithoutExtension(image.StoredFileName)}.webp");
                                                    if(File.Exists(imagePath))
                                                    {
                                                        //convert to webp
                                                        using (ImageFactory imageFactory = new ImageFactory(preserveExifData: false))
                                                        {
                                                            imageFactory.Load(imagePath)
                                                                        .Format(new WebPFormat())
                                                                        .Quality(80)
                                                                        .Save(webpPath);
                                                        }
                                                    }
                                                    
                                                    if(File.Exists(webpPath))
                                                    {
                                                        //update db meta data
                                                        var imageInDb = await context.PictureFiles.Where(p => p.Id == image.Id).SingleAsync();
                                                        imageInDb.ContentType = "image/webp";
                                                        imageInDb.StoredFileName = imageInDb.StoredFileName.Replace(".jpg", ".webp", StringComparison.InvariantCultureIgnoreCase);
                                                        context.PictureFiles.Update(imageInDb);
                                                        await context.SaveChangesAsync();

                                                        if(File.Exists(imagePath))
                                                        {
                                                            try
                                                            {
                                                                File.Delete(imagePath);
                                                            }
                                                            catch
                                                            {
                                                                //ignore exception
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                }
                                catch (Exception exp)
                                {
                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                }

                            }
                        });

            return new RServiceResult<bool>(true);
        }
    }
}