using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Linq;
using System.IO;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IArtifactService implementation
    /// </summary>
    public partial class ArtifactService : IArtifactService
    {
        /// <summary>
        /// start removing original images
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartRemovingOriginalImages()
        {
            try
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem
                            (
                            async token =>
                            {
                                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                                {
                                    LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                    var job = (await jobProgressServiceEF.NewJob("RemovingOriginalImages", "Removing")).Result;

                                    try
                                    {
                                        var srcPath = Configuration.GetSection("PictureFileService")["StoragePath"];
                                        var trashPath = Configuration.GetSection("PictureFileService")["TrashStoragePath"];

                                        var images = await context.PictureFiles.Where(p => p.StoredFileName != null &&  p.SrcUrl != null && p.NormalSizeImageStoredFileName.IndexOf("orig") != 0 ).ToListAsync();

                                        int progress = 0;

                                        for (int i = 0; i < images.Count; i++)
                                        {
                                            var image = images[i];
                                            string targetDir = Path.Combine(trashPath, image.FolderName);
                                            if (!Directory.Exists(targetDir))
                                            {
                                                Directory.CreateDirectory(targetDir);
                                                Directory.CreateDirectory(Path.Combine(targetDir, "orig"));
                                            }
                                            string srcFileName = Path.Combine(Path.Combine(srcPath, image.FolderName), image.StoredFileName);
                                            if(File.Exists(srcFileName))
                                            {
                                                string targetFileName = Path.Combine(Path.Combine(trashPath, image.FolderName), image.StoredFileName);
                                                File.Move(srcFileName, targetFileName, true);
                                                image.StoredFileName = null;
                                                context.Update(image);

                                                if( (i * 100 / images.Count) > progress)
                                                {
                                                    progress = i * 100 / images.Count;
                                                    await jobProgressServiceEF.UpdateJob(job.Id, progress);
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