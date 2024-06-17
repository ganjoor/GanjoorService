using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using RMuseum.DbContext;
using System.Collections.Generic;
using RSecurityBackend.Services.Implementation;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        public void TagCategoryWithSource(int catId, string sourceUrlSlug)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                   var job = (await jobProgressServiceEF.NewJob($"TagCategoryWithSource(catId: {catId} - source:{sourceUrlSlug})", "Query data")).Result;
                                   try
                                   {
                                       var page = await context.GanjoorPages.AsNoTracking().Where(p => p.FullUrl == $"/sources/{sourceUrlSlug}").SingleOrDefaultAsync();
                                       if(page == null)
                                       {
                                           await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"Page not found : /sources/{sourceUrlSlug}");
                                       }

                                       string sourceName = page.Title;
                                       switch(sourceUrlSlug)
                                       {
                                           case "wikidorj":
                                               sourceName = "ویکی‌درج";
                                               break;
                                           case "frankfurt":
                                               sourceName = "دانشگاه فرانکفورت";
                                               break;
                                           case "tariqmo":
                                               sourceName = "طریق التحقیق دکتر مؤذنی";
                                               break;
                                           case "tebyan":
                                               sourceName = "تبیان";
                                               break;
                                        
                                       }

                                       List<int> catIdList = new List<int>
                                       {
                                           catId
                                       };
                                       await _populateCategoryChildren(context, catId, catIdList);
                                       int poemCount = 0;
                                       int progress = 0;
                                       foreach (int catId in catIdList)
                                       {
                                           var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == catId).ToListAsync();
                                           poemCount += poems.Count;
                                           foreach (var poem in poems)
                                           {
                                               poem.SourceName = sourceName;
                                               poem.SourceUrlSlug = sourceUrlSlug;
                                               context.Update(poem);
                                               await jobProgressServiceEF.UpdateJob(job.Id, progress, $"{progress} از {poemCount}");
                                           }
                                       }

                                   }
                                   catch (Exception exp)
                                   {
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                   }

                               }
                           });
        }
    }
}