using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// Finding Category Poems Duplicates
        /// </summary>
        /// <param name="srcCatId"></param>
        /// <param name="destCatId"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartFindingCategoryPoemsDuplicates(int srcCatId, int destCatId)
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
                                   var job = (await jobProgressServiceEF.NewJob("StartFindingCategoryPoemsDuplicates", "Query data")).Result;
                                   try
                                   {
                                       await _FindCategoryPoemsDuplicates(context, srcCatId, destCatId);
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
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// start removing category duplicates
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="destCatId"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartRemovingCategoryDuplicates(int catId, int destCatId)
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
                                   var job = (await jobProgressServiceEF.NewJob($"RemoveCategoryDuplicates {catId}", "Query data")).Result;
                                   try
                                   {
                                       var dups = await context.GanjoorDuplicates.Where(p => p.SrcCatId == catId && p.DestPoemId != null).OrderBy(p => p.SrcPoemId).ToListAsync();
                                       var poems = await context.GanjoorPoems.Where(p => p.CatId == catId).OrderBy(p => p.Id).ToListAsync();
                                       if (poems.Count != dups.Count)
                                       {
                                           await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "poems.Count != dups.Count");
                                           return;
                                       }

                                       foreach (var poem in poems)
                                       {
                                           if (!dups.Where(d => d.SrcPoemId == poem.Id).Any())
                                           {
                                               await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"poem ID = {poem.Id}, missing destination");
                                               return;
                                           }
                                       }

                                       foreach (var dup in dups)
                                       {
                                           var srcPoem = poems.Where(p => p.Id == dup.SrcPoemId).Single();
                                          
                                           var destPage = await context.GanjoorPages.Where(p => p.Id == dup.DestPoemId).SingleAsync();
                                           destPage.RedirectFromFullUrl = srcPoem.FullUrl;
                                           context.Update(destPage);

                                           var comments = await context.GanjoorComments.Where(c => c.PoemId == dup.SrcPoemId).ToListAsync();
                                           foreach (var comment in comments)
                                           {
                                               comment.PoemId = (int)dup.DestPoemId;
                                           }
                                           context.UpdateRange(comments);

                                           var songs = await context.GanjoorPoemMusicTracks.Where(m => m.PoemId == dup.SrcPoemId).ToListAsync();
                                           foreach (var song in songs)
                                           {
                                               var alreadyAdded = await context.GanjoorPoemMusicTracks.Where(m => m.PoemId == dup.DestPoemId && m.TrackUrl == song.TrackUrl).FirstOrDefaultAsync();
                                               if(alreadyAdded != null)
                                               {
                                                   song.PoemId = (int)dup.DestPoemId;
                                               }
                                           }
                                           context.UpdateRange(songs);

                                           var similars = await context.GanjoorCachedRelatedPoems.Where(s => s.FullUrl == srcPoem.FullUrl).ToListAsync();
                                           context.RemoveRange(similars);

                                           var corrections = await context.GanjoorPoemCorrections.Include(c => c.VerseOrderText).Where(c => c.PoemId == srcPoem.Id).ToListAsync();
                                           context.RemoveRange(corrections);

                                           var page = await context.GanjoorPages.Where(p => p.Id == srcPoem.Id && p.GanjoorPageType == GanjoorPageType.PoemPage).SingleAsync();
                                           context.Remove(page);

                                           await jobProgressServiceEF.UpdateJob(job.Id, 0, srcPoem.FullTitle);
                                       }
                                       context.RemoveRange(dups);
                                       context.RemoveRange(poems);

                                       await jobProgressServiceEF.UpdateJob(job.Id, 1,  "Removing Category and poems");

                                       var catPage = await context.GanjoorPages.Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == catId).SingleAsync();
                                       var destCatPage = await context.GanjoorPages.Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == destCatId).SingleAsync();
                                       destCatPage.RedirectFromFullUrl = catPage.FullUrl;
                                       context.Update(destCatPage);
                                       context.Remove(catPage);

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
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
        private async Task<RServiceResult<bool>> _FindCategoryPoemsDuplicates(RMuseumDbContext context, int srcCatId, int destCatId)
        {
            try
            {
                if (srcCatId == destCatId)
                    return new RServiceResult<bool>(false, "srcCatId == destCatId");
                var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == srcCatId).ToListAsync();
                var targetPoems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == destCatId).ToListAsync();
                var alreayFoundOnes = await context.GanjoorDuplicates.AsNoTracking().Where(p => p.SrcCatId == srcCatId).ToListAsync();
                foreach (var poem in poems)
                {
                    var firstVerse = await context.GanjoorVerses.AsNoTracking().Where(p => p.PoemId == poem.Id).OrderBy(v => v.VOrder).FirstAsync();
                    if (alreayFoundOnes.Any(p => p.SrcPoemId == poem.Id))
                        continue;
                    var probablyTheOnes = targetPoems.Where(p => p.GanjoorMetreId == poem.GanjoorMetreId && p.RhymeLetters == poem.RhymeLetters).ToList();
                    bool found = false;
                    foreach (var probable in probablyTheOnes)
                    {
                        var targetVerses = await context.GanjoorVerses.AsNoTracking().Where(p => p.PoemId == probable.Id).OrderBy(v => v.VOrder).ToListAsync();
                        foreach (var targetVerse in targetVerses)
                        {
                            if (_AreSimilar(firstVerse.Text, targetVerse.Text, false))
                            {
                                context.GanjoorDuplicates.Add
                                    (
                                    new GanjoorDuplicate()
                                    {
                                        SrcCatId = srcCatId,
                                        SrcPoemId = poem.Id,
                                        DestPoemId = probable.Id
                                    }
                                    );
                                await context.SaveChangesAsync();
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                    if (found) continue;
                    foreach (var probable in targetPoems)
                    {
                        var targetVerses = await context.GanjoorVerses.AsNoTracking().Where(p => p.PoemId == probable.Id).OrderBy(v => v.VOrder).ToListAsync();
                        foreach (var targetVerse in targetVerses)
                        {
                            if (_AreSimilar(firstVerse.Text, targetVerse.Text, false))
                            {
                                context.GanjoorDuplicates.Add
                                    (
                                    new GanjoorDuplicate()
                                    {
                                        SrcCatId = srcCatId,
                                        SrcPoemId = poem.Id,
                                        DestPoemId = probable.Id
                                    }
                                    );
                                await context.SaveChangesAsync();
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                }
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private static bool _AreSimilar(string str1, string str2, bool reverse)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return false;
            string[] words2 = str2.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int total = words2.Length;
            int found = 0;
            for (int i = 0; i < total; i++)
            {
                if (str1.IndexOf(words2[i]) != -1)
                    found++;
            }
            if (!reverse)
                return (float)found / total > 0.7f;
            return (float)found / total > 0.7f && _AreSimilar(str2, str1, false);
        }
    }
}