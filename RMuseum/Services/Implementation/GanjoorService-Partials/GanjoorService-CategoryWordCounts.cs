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
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using RMuseum.Migrations;

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
            var source =
                 from cwd in _context.CategoryWordCounts
                 where cwd.CatId == catId && (string.IsNullOrEmpty(term) || cwd.Word.Contains(term) )
                 orderby cwd.RowNmbrInCat
                 select
                 cwd;

            (PaginationMetadata PagingMeta, CategoryWordCount[] Items) paginatedResult =
                await QueryablePaginator<CategoryWordCount>.Paginate(source, paging);

            return new RServiceResult<(PaginationMetadata PagingMeta, CategoryWordCount[] Items)>(paginatedResult);
        }


        /// <summary>
        /// build word counts
        /// </summary>
        public async Task BuildCategoryWordCountsAsync(bool reset)
        {
            int lastFinishedPoetId = 0;
            if (!reset)
            {
                var resOption = await _optionsService.GetValueAsync("CategoryWordCountsLastPoetId", null);
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
                                      if (reset)
                                      {
                                          var existing = await context.CategoryWordCounts.ToListAsync();
                                          if (existing.Any())
                                          {
                                              context.RemoveRange(existing);
                                              await context.SaveChangesAsync();
                                          }
                                      }
                                      var poets = await context.GanjoorPoets.AsNoTracking().Where(p => p.Published && p.Id > lastFinishedPoetId).OrderBy(p => p.Id).ToListAsync();
                                      foreach (var poet in poets)
                                      {
                                          var poetCat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poet.Id && c.ParentId == null).SingleAsync();
                                          await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname);
                                          var wordCounts = await _BuildCategoryWordStatsAsync(context, poetCat, true, jobProgressServiceEF, job);
                                          if(true == await context.CategoryWordCounts.Where(c => c.CatId == poetCat.Id).AnyAsync())
                                          {
                                              List<int> catIdList = new List<int>
                                               {
                                                   poetCat.Id
                                               };
                                              await _populateCategoryChildren(context, poetCat.Id, catIdList);
                                              await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname + $": Removing old data");
                                              foreach (var catId in catIdList)
                                              {
                                                  var oldData = await context.CategoryWordCounts.Where(c => c.CatId == catId).ToListAsync();
                                                  context.RemoveRange(oldData);
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
                                                      catWordCounts.Sort((a, b) => b.Count.CompareTo(a.Count));
                                                      for (int i = 0; i < catWordCounts.Count; i++)
                                                      {
                                                          catWordCounts[i].RowNmbrInCat = i + 1;
                                                      }
                                                  }
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

        private async Task<List<CategoryWordCount>> _BuildCategoryWordStatsAsync(RMuseumDbContext context, GanjoorCat cat, bool poetCat, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job)
        {
            List<CategoryWordCount> counts = new List<CategoryWordCount>();
            var children = await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == cat.Id).ToListAsync();
            foreach (var child in children)
            {
                var subcatCounts = await _BuildCategoryWordStatsAsync(context, child, false, jobProgressServiceEF, job);
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
                            counts.Add(new CategoryWordCount { CatId = cat.Id, Word = count.Word, Count = count.Count, PoetCat = poetCat });
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
                            counts.Add(new CategoryWordCount { CatId = cat.Id, Word = word, Count = 1, PoetCat = poetCat });
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


    }
}