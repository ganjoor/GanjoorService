using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using System;
using System.Data;
using System.Linq;
using RMuseum.DbContext;
using System.Collections.Generic;
using RSecurityBackend.Services.Implementation;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        public void BuildCategoryWordCounts()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                          (
                          async token =>
                          {
                              using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                              {
                                  LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                  var job = (await jobProgressServiceEF.NewJob($"BuildCategoryWordCounts", "Query data")).Result;
                                  try
                                  {
                                      var poets = await context.GanjoorPoets.AsNoTracking().Where(p => p.Published).OrderBy(p => p.Id).ToListAsync();
                                      foreach (var poet in poets)
                                      {
                                          var poetCat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poet.Id && c.ParentId == null).SingleAsync();
                                          await _BuildCategoryWordStatsAsync(context, poetCat);
                                      }
                                      await jobProgressServiceEF.UpdateJob(job.Id, 100);
                                  }
                                  catch (Exception exp)
                                  {
                                      await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                  }

                              }
                          });
        }

        private async Task<List<CategoryWordCount>> _BuildCategoryWordStatsAsync(RMuseumDbContext context, GanjoorCat cat)
        {
            List<CategoryWordCount> counts = new List<CategoryWordCount>();
            var children = await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == cat.Id).ToListAsync();
            foreach (var child in children)
            {
                var subcatCounts = await _BuildCategoryWordStatsAsync(context, child);
                if (subcatCounts.Any())
                {
                    foreach (var count in subcatCounts)
                    {
                        var wordCount = counts.Where(c => c.CatId == cat.Id && c.Word == count.Word).SingleOrDefault();
                        if (wordCount != null)
                        {
                            wordCount.Count += count.Count;
                        }
                        else
                        {
                            counts.Add(new CategoryWordCount { CatId = cat.Id, Word = count.Word, Count = count.Count });
                        }
                    }
                    counts.AddRange(subcatCounts);
                }
            }

            var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == cat.Id).ToListAsync();
            foreach (var poem in poems)
            {
                var verses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poem.Id && v.VersePosition != VersePosition.Comment).OrderBy(v => v.VOrder).ToListAsync();
                foreach (var verse in verses)
                {
                    string[] words = verse.Text.Split([' ', '‌']);
                    foreach (var word in words)
                    {
                        var wordCount = counts.Where(c => c.CatId == cat.Id && c.Word == word).SingleOrDefault();
                        if (wordCount != null)
                        {
                            wordCount.Count++;
                        }
                        else
                        {
                            counts.Add(new CategoryWordCount { CatId = cat.Id, Word = word, Count = 1 });
                        }
                    }
                }
            }
            return counts;
        }

    }
}