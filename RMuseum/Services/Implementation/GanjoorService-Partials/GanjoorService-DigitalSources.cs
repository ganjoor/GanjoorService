using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
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
        /// <summary>
        /// tag with sources
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="sourceUrlSlug"></param>
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
                                       var pages = await context.GanjoorPages.AsNoTracking().Where(p => p.FullUrl.StartsWith("/sources/")).ToListAsync();
                                       foreach (var page in pages)
                                       {
                                           if(false == await context.DigitalSources.Where(d => d.UrlSlug == page.UrlSlug).AnyAsync())
                                           {
                                               string shortName = page.Title;
                                               switch (page.UrlSlug)
                                               {
                                                   case "wikidorj":
                                                       shortName = "ویکی‌درج";
                                                       break;
                                                   case "frankfurt":
                                                       shortName = "دانشگاه فرانکفورت";
                                                       break;
                                                   case "tariqmo":
                                                       shortName = "طریق التحقیق دکتر مؤذنی";
                                                       break;
                                                   case "tebyan":
                                                       shortName = "تبیان";
                                                       break;
                                               }
                                               context.DigitalSources.Add
                                               (
                                                   new DigitalSource()
                                                   {
                                                       UrlSlug = page.UrlSlug,
                                                       ShortName = shortName,
                                                       FullName = page.Title,
                                                       SourceType = "",
                                                       CoupletsCount = 0,
                                                   }
                                               );
                                               await context.SaveChangesAsync();
                                           }
                                       }

                                       var digitalSource = await context.DigitalSources.Where(p => p.UrlSlug == sourceUrlSlug).SingleOrDefaultAsync();
                                       if (digitalSource == null)
                                       {
                                           await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"Digital source not found: {sourceUrlSlug}");
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
                                               poem.SourceName = digitalSource.ShortName;
                                               poem.SourceUrlSlug = sourceUrlSlug;
                                               context.Update(poem);

                                               int coupletCount = await context.GanjoorVerses.AsNoTracking()
                                                .Where(v =>
                                                    v.PoemId == poem.Id
                                                    &&
                                                    (v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1)                                            
                                                    ).CountAsync();
                                               digitalSource.CoupletsCount += coupletCount;
                                               await jobProgressServiceEF.UpdateJob(job.Id, progress, $"{progress} از {poemCount}");
                                           }
                                       }

                                       context.Update(digitalSource);
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                   }
                                   catch (Exception exp)
                                   {
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                   }

                               }
                           });
        }

        /// <summary>
        /// update digital sources stats
        /// </summary>
        public void UpdateDigitalSourcesStats()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                   var job = (await jobProgressServiceEF.NewJob($"UpdateDigitalSourcesStats", "Query data")).Result;
                                   try
                                   {
                                       var pages = await context.GanjoorPages.AsNoTracking().Where(p => p.FullUrl.StartsWith("/sources/")).ToListAsync();
                                       foreach (var page in pages)
                                       {
                                           if (false == await context.DigitalSources.Where(d => d.UrlSlug == page.UrlSlug).AnyAsync())
                                           {
                                               string shortName = page.Title;
                                               switch (page.UrlSlug)
                                               {
                                                   case "wikidorj":
                                                       shortName = "ویکی‌درج";
                                                       break;
                                                   case "frankfurt":
                                                       shortName = "دانشگاه فرانکفورت";
                                                       break;
                                                   case "tariqmo":
                                                       shortName = "طریق التحقیق دکتر مؤذنی";
                                                       break;
                                                   case "tebyan":
                                                       shortName = "تبیان";
                                                       break;
                                               }
                                               context.DigitalSources.Add
                                               (
                                                   new DigitalSource()
                                                   {
                                                       UrlSlug = page.UrlSlug,
                                                       ShortName = shortName,
                                                       FullName = page.Title,
                                                       SourceType = "",
                                                       CoupletsCount = 0,
                                                   }
                                               );
                                               await context.SaveChangesAsync();
                                           }
                                       }

                                       var digitalSources = await context.DigitalSources.ToArrayAsync();
                                       int totalCoupletsCount = 0;
                                       foreach ( var digitalSource in digitalSources )
                                       {
                                           digitalSource.CoupletsCount = 0;
                                           var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.SourceUrlSlug == digitalSource.UrlSlug).ToArrayAsync();
                                           foreach (var poem in poems)
                                           {
                                               int coupletCount = await context.GanjoorVerses.AsNoTracking()
                                                .Where(v =>
                                                    v.PoemId == poem.Id
                                                    &&
                                                    (v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1)
                                                    ).CountAsync();
                                               digitalSource.CoupletsCount += coupletCount;
                                           }
                                           totalCoupletsCount += digitalSource.CoupletsCount;
                                           context.Update(digitalSource);
                                           await jobProgressServiceEF.UpdateJob(job.Id, 50, digitalSource.FullName);
                                       }

                                       var noSourceUrlPoems = await context.GanjoorPoems.AsNoTracking().Where(p => string.IsNullOrEmpty(p.SourceUrlSlug)).ToArrayAsync();
                                       int untaggaed = 0;
                                       foreach (var noSourceUrlPoem in noSourceUrlPoems)
                                       {
                                           int coupletCount = await context.GanjoorVerses.AsNoTracking()
                                            .Where(v =>
                                                v.PoemId == noSourceUrlPoem.Id
                                                &&
                                                (v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1)
                                                ).CountAsync();
                                           totalCoupletsCount += coupletCount;
                                           untaggaed += coupletCount;
                                       }

                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, $"total: {totalCoupletsCount} - untagged: {untaggaed}", true);
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