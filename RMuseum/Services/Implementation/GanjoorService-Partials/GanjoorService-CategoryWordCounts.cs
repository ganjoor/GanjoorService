using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using System;
using System.Data;
using System.Linq;
using RMuseum.DbContext;
using System.Collections.Generic;
using RSecurityBackend.Services.Implementation;
using System.Threading.Tasks;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Models.Generic;
using RMuseum.Models.Ganjoor.ViewModels;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// category word counts
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="term"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, CategoryWordCount[] Items)>> GetCategoryWordCountsAsync(int catId, string term, PagingParameterModel paging)
        {
            try
            {
                var source =
                     from cwd in _context.CategoryWordCounts
                     where cwd.CatId == catId && (string.IsNullOrEmpty(term) || cwd.Word.Contains(term))
                     orderby cwd.RowNmbrInCat
                     select
                     cwd;

                (PaginationMetadata PagingMeta, CategoryWordCount[] Items) paginatedResult =
                    await QueryablePaginator<CategoryWordCount>.Paginate(source, paging);

                return new RServiceResult<(PaginationMetadata PagingMeta, CategoryWordCount[] Items)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, CategoryWordCount[] Items)>((null, null), exp.ToString());
            }
        }

        /// <summary>
        /// category words summary
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<CategoryWordCountSummary>> GetCategoryWordCountSummaryAsync(int catId)
        {
            try
            {
                var res = await _context.CategoryWordCountSummaries.AsNoTracking().Where(c => c.CatId == catId).SingleOrDefaultAsync();
                if(res == null)
                    return new RServiceResult<CategoryWordCountSummary>(new CategoryWordCountSummary() { CatId = catId, UniqueWordCount = 0, TotalWordCount = 0 });
                return new RServiceResult<CategoryWordCountSummary>(res);
            }
            catch (Exception exp)
            {
                return new RServiceResult<CategoryWordCountSummary>(null, exp.ToString());
            }
        }

        /// <summary>
        /// comparison of word counts for poets
        /// </summary>
        /// <param name="term"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, PoetOrCatWordStat[] Items)>> GetCategoryWordCountsByPoetsAsync(string term, PagingParameterModel paging)
        {
            try
            {
                var source =
                     from cwd in _context.CategoryWordCounts
                     join catsum in _context.CategoryWordCountSummaries on cwd.CatId equals catsum.CatId
                     join cat in _context.GanjoorCategories on cwd.CatId equals cat.Id
                     where cat.ParentId == null && cwd.Word == term
                     orderby cwd.Count descending
                     select
                     new PoetOrCatWordStat()
                     {
                         Id = cat.PoetId,
                         Name =  cat.Title,
                         Count = cwd.Count,
                         RowNmbrInCat = cwd.RowNmbrInCat,
                         TotalWordCount = catsum.TotalWordCount,
                     };

                (PaginationMetadata PagingMeta, PoetOrCatWordStat[] Items) paginatedResult =
                    await QueryablePaginator<PoetOrCatWordStat>.Paginate(source, paging);

                return new RServiceResult<(PaginationMetadata PagingMeta, PoetOrCatWordStat[] Items)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, PoetOrCatWordStat[] Items)>((null, null), exp.ToString());
            }
        }


        /// <summary>
        /// build word counts
        /// </summary>
        /// <param name="reset"></param>
        /// <param name="poetId"></param>
        public async Task BuildCategoryWordCountsAsync(bool reset, int poetId)
        {
            int lastFinishedPoetId = 0;
            if (!reset)
            {
                var resOption = await _optionsService.GetValueAsync("CategoryWordCountsLastPoetId", null, null);
                if (!string.IsNullOrEmpty(resOption.Result))
                {
                    lastFinishedPoetId = int.Parse(resOption.Result);
                }
            }
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
                                      var poets = poetId == 0 ? await context.GanjoorPoets.AsNoTracking().Where(p => p.Published && p.Id > lastFinishedPoetId).OrderBy(p => p.Id).ToListAsync()
                                      : await context.GanjoorPoets.AsNoTracking().Where(p => p.Published && p.Id == poetId).OrderBy(p => p.Id).ToListAsync();
                                      foreach (var poet in poets)
                                      {
                                          var poetCat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poet.Id && c.ParentId == null).SingleAsync();
                                          await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname);
                                          var wordCounts = await _BuildCategoryWordStatsAsync(context, poetCat, jobProgressServiceEF, job);
                                          if(true == await context.CategoryWordCounts.Where(c => c.CatId == poetCat.Id).AnyAsync() 
                                              ||
                                            true == await context.CategoryWordCountSummaries.Where(c => c.CatId == poetCat.Id).AnyAsync()
                                          )
                                          {
                                              List<int> catIdList = new List<int>
                                               {
                                                   poetCat.Id
                                               };
                                              await _populateCategoryChildren(context, poetCat.Id, catIdList);
                                              await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname + $": Removing old data");
                                              foreach (var catId in catIdList)
                                              {
                                                  var oldData1 = await context.CategoryWordCounts.Where(c => c.CatId == catId).ToListAsync();
                                                  context.RemoveRange(oldData1);
                                                  var oldData2 = await context.CategoryWordCountSummaries.Where(c => c.CatId == catId).ToListAsync();
                                                  context.RemoveRange(oldData2);
                                                  await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname + $": Removing old data {catId}");
                                              }
                                          }
                                          await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname + $": In Memory => DbContext - {wordCounts.Count} - Sorting:");
                                          if (wordCounts.Any())
                                          {
                                              List<int> catIdList = new List<int>
                                               {
                                                   poetCat.Id
                                               };
                                              await _populateCategoryChildren(context, poetCat.Id, catIdList);
                                              foreach (var catId in catIdList)
                                              {
                                                  var catWordCounts = wordCounts.Where(w => w.CatId == catId).ToList();
                                                  if (catWordCounts.Any())
                                                  {
                                                      await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname + $": In Memory => DbContext CatId: {catId} - {catWordCounts.Count} - Sorting Started.");
                                                      catWordCounts.Sort((a, b) => b.Count.CompareTo(a.Count));
                                                      await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname + $": In Memory => DbContext CatId: {catId} - {catWordCounts.Count} - Sorting Finished.");
                                                      for (int i = 0; i < catWordCounts.Count; i++)
                                                      {
                                                          catWordCounts[i].RowNmbrInCat = i + 1;
                                                      }
                                                  }

                                                  context.Add
                                                  (
                                                      new CategoryWordCountSummary()
                                                      {
                                                          CatId = catId,
                                                          UniqueWordCount = catWordCounts.Count,
                                                          TotalWordCount = catWordCounts.Sum(w => w.Count)
                                                      }
                                                  );
                                              }

                                              for (int i = 0; i < wordCounts.Count; i++)
                                              {
                                                  context.Add(wordCounts[i]);
                                                  await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname + $": Saving {i} of {wordCounts.Count}, {wordCounts[i].Word}");
                                              }
                                          }
                                          
                                          RGenericOption option = await context.Options.Where(o => o.Name == "CategoryWordCountsLastPoetId" && o.RAppUserId == null).SingleOrDefaultAsync();
                                          if (option != null)
                                          {
                                              option.Value = poetCat.PoetId.ToString();
                                              context.Options.Update(option);
                                              await context.SaveChangesAsync();
                                          }
                                          else
                                          {
                                              RGenericOption newOption = new RGenericOption
                                              {
                                                  Name = "CategoryWordCountsLastPoetId",
                                                  Value = poetCat.PoetId.ToString(),
                                                  RAppUserId = null
                                              };
                                              context.Options.Add(newOption);
                                              await context.SaveChangesAsync();
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
        }

        private async Task<List<CategoryWordCount>> _BuildCategoryWordStatsAsync(RMuseumDbContext context, GanjoorCat cat, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job)
        {
            List<CategoryWordCount> counts = new List<CategoryWordCount>();
            var children = await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == cat.Id).ToListAsync();
            foreach (var child in children)
            {
                var subcatCounts = await _BuildCategoryWordStatsAsync(context, child, jobProgressServiceEF, job);
                if (subcatCounts.Any())
                {
                    foreach (var subcatCount in subcatCounts.Where(c => c.CatId == child.Id))
                    {
                        var wordCount = counts.Where(c => c.CatId == cat.Id && c.Word == subcatCount.Word).SingleOrDefault();
                        if (wordCount != null)
                        {
                            wordCount.Count += subcatCount.Count;
                        }
                        else
                        {
                            counts.Add(new CategoryWordCount { CatId = cat.Id, Word = subcatCount.Word, Count = subcatCount.Count });
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
                    string[] words = LanguageUtils.MakeTextSearchable(verse.Text).Split([' ', '‌']);
                    foreach (var word in words)
                    {
                        if(string.IsNullOrEmpty(word)) continue;
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
                await jobProgressServiceEF.UpdateJob(job.Id, poem.Id);
            }
            return counts;
        }

        /// <summary>
        /// fill CategoryWordCounts.RowNmbrInCat
        /// </summary>
        public void FillCategoryWordCountsRowNmbrInCat()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"FillCategoryWordCountsRowNmbrInCat", "Query data")).Result;
                      try
                      {
                          var poets = await context.GanjoorPoets.AsNoTracking().Where(p => p.Published).OrderBy(p => p.Id).ToListAsync();
                          foreach (var poet in poets)
                          {
                              var poetCat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poet.Id && c.ParentId == null).SingleAsync();
                              await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname);
                              if (true == await context.CategoryWordCounts.Where(c => c.CatId == poetCat.Id).AnyAsync())
                              {
                                  List<int> catIdList = new List<int>
                                   {
                                       poetCat.Id
                                   };
                                  await _populateCategoryChildren(context, poetCat.Id, catIdList);
                                  foreach (var catId in catIdList)
                                  {
                                      var catWordCounts = await context.CategoryWordCounts.Where(w => w.CatId == catId).ToListAsync();
                                      if (catWordCounts.Any())
                                      {
                                          catWordCounts.Sort((a, b) => b.Count.CompareTo(a.Count));
                                          for (int i = 0; i < catWordCounts.Count; i++)
                                          {
                                              catWordCounts[i].RowNmbrInCat = i + 1;
                                              context.Update(catWordCounts[i]);
                                              await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname + $": Saving {i} of {catWordCounts.Count}");
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

        }

        /// <summary>
        /// fill CategoryWordCountSummaries
        /// </summary>
        public void FillCategoryWordCountSummaries()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"FillCategoryWordCountSummaries", "Query data")).Result;
                      try
                      {
                          var poets = await context.GanjoorPoets.AsNoTracking().Where(p => p.Published).OrderBy(p => p.Id).ToListAsync();
                          foreach (var poet in poets)
                          {
                              var poetCat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poet.Id && c.ParentId == null).SingleAsync();
                              await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname);
                              if (true == await context.CategoryWordCounts.Where(c => c.CatId == poetCat.Id).AnyAsync())
                              {
                                  List<int> catIdList = new List<int>
                                   {
                                       poetCat.Id
                                   };
                                  await _populateCategoryChildren(context, poetCat.Id, catIdList);
                                  foreach (var catId in catIdList)
                                  {
                                      var uniqueCount = await context.CategoryWordCounts.Where(w => w.CatId == catId).CountAsync();
                                      if (uniqueCount > 0)
                                      {
                                          var totalCount = await context.CategoryWordCounts.Where(w => w.CatId == catId).SumAsync(w => w.Count);
                                          context.Add
                                          (
                                              new CategoryWordCountSummary()
                                              {
                                                  CatId = catId,
                                                  UniqueWordCount = uniqueCount,
                                                  TotalWordCount = totalCount
                                              }
                                          );
                                          await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname + $": Saving {catId}");
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

        }



    }
}