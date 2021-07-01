using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.ImportJob;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
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
        /// start updating stats page
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartFillingGanjoorLinkOriginalSources()
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
                                var job = (await jobProgressServiceEF.NewJob("UpdateStatsPage", "Total Poets Stats")).Result;

                                try
                                {
                                    var links = await context.GanjoorLinks.ToListAsync();

                                    for (int i = 0; i < links.Count; i++)
                                    {
                                        await jobProgressServiceEF.UpdateJob(job.Id, i);

                                        var link = links[i];

                                        var itemInfo = await context.Items
                                            .Include(i => i.Tags)
                                            .ThenInclude(t => t.RTag)
                                            .Where(i => i.Id == link.ItemId).SingleAsync();

                                        var sourceTag = itemInfo.Tags.Where(t => t.RTag.FriendlyUrl == "source").FirstOrDefault();

                                        if(sourceTag != null)
                                        {
                                            if(!string.IsNullOrEmpty(sourceTag.ValueSupplement) && (sourceTag.ValueSupplement.IndexOf("http") == 0))
                                            {
                                               link.OriginalSourceUrl = sourceTag.ValueSupplement;
                                               link.LinkToOriginalSource = true;
                                               context.GanjoorLinks.Update(link);
                                            }
                                        }

                                    }
                                    await context.SaveChangesAsync();
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