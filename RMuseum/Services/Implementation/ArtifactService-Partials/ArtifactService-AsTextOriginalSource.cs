using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
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
        /// start setting an artifact items as text original source
        /// </summary>
        /// <param name="ganjoorCatId"></param>
        /// <param name="artifactId"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartSettingArtifactAsTextOriginalSource(int ganjoorCatId, Guid artifactId)
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
                                    var job = (await jobProgressServiceEF.NewJob($"SettingArtifactAsTextOriginalSource({ganjoorCatId}, {artifactId})", "Query Poems Data")).Result;

                                    try
                                    {
                                        List<int> poemIdSet = new List<int>();
                                        await _EnumerateGanjoorCatPoemIdSet(context, poemIdSet, ganjoorCatId);
                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, "Query Links Data");
                                        var links = await context.GanjoorLinks.Where(l => l.ArtifactId == artifactId).ToListAsync();
                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, "Removing Old Originals");
                                        foreach (var poemId in poemIdSet)
                                        {
                                            var poemLinks = await context.GanjoorLinks.Where(l => l.GanjoorPostId == poemId && l.IsTextOriginalSource == true).ToListAsync();
                                            foreach (var poemLink in poemLinks)
                                            {
                                                poemLink.IsTextOriginalSource = false;
                                                context.Update(poemLink);
                                            }
                                        }
                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, "Updating");
                                        foreach (var link in links)
                                        {
                                            if(poemIdSet.Where(p => p == link.GanjoorPostId).Any())
                                            {
                                                link.IsTextOriginalSource = true;
                                                context.Update(link);
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

        private async Task _EnumerateGanjoorCatPoemIdSet(RMuseumDbContext context, List<int> poemIdSet, int catId)
        {
            var catPoemIdSets = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == catId).Select(p => p.Id).ToListAsync();
            if (catPoemIdSets.Count > 0)
                poemIdSet.AddRange(catPoemIdSets);
            var subCatIdSet = await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == catId).Select(c => c.Id).ToListAsync();
            foreach (var subCatId in subCatIdSet)
            {
                await _EnumerateGanjoorCatPoemIdSet(context, poemIdSet, subCatId);
            }
        }
    }
}
