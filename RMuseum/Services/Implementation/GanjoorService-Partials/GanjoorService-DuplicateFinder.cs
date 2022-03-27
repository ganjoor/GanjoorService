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
                        var meVerse = await context.GanjoorVerses.AsNoTracking().Where(p => p.PoemId == probable.Id).OrderBy(v => v.VOrder).FirstAsync();
                        if(_AreSimilar(firstVerse.Text, meVerse.Text, false))
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
                    if (found) continue;
                    foreach (var probable in targetPoems)
                    {
                        var meVerse = await context.GanjoorVerses.AsNoTracking().Where(p => p.PoemId == probable.Id).OrderBy(v => v.VOrder).FirstAsync();
                        if (_AreSimilar(firstVerse.Text, meVerse.Text, false))
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
                            break;
                        }
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