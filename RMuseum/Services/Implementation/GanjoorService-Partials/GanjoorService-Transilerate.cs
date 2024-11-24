using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;
using RMuseum.DbContext;
using System.Collections.Generic;
using RSecurityBackend.Services.Implementation;
using RMuseum.Utils;
using RMuseum.Models.Ganjoor;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// transilerate verse data from Persian to Tajiki
        /// </summary>
        public void Transilerate()
        {
            string formData = Configuration["TransilerateFormData"];
            _backgroundTaskQueue.QueueBackgroundWorkItem
                          (
                          async token =>
                          {
                              using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                              {
                                  LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                  var job = (await jobProgressServiceEF.NewJob($"Transilerate", "Query data")).Result;
                                  try
                                  {
                                      string error;
                                      var poets = await context.GanjoorPoets.AsNoTracking().Where(p => p.Published && p.Id > 5).OrderBy(p => p.Id).ToListAsync();
                                      foreach (var poet in poets)
                                      {
                                          await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname);
                                          var poetCat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poet.Id && c.ParentId == null).SingleAsync();
                                          List<int> catIdList = new List<int>
                                               {
                                                   poetCat.Id
                                               };
                                          await _populateCategoryChildren(context, poetCat.Id, catIdList);
                                          foreach (var catId in catIdList)
                                          {
                                              var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == catId).OrderBy(p => p.Id).ToListAsync();
                                              foreach (var poem in poems)
                                              {
                                                  var verses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poem.Id).OrderBy(v => v.VOrder).ToListAsync();
                                                  foreach (var verse in verses)
                                                  {
                                                      if (false == await context.TajikVerses.Where(t => t.Id == verse.Id).AnyAsync())
                                                      {
                                                          GanjoorTajikVerse tajikVerse = new GanjoorTajikVerse()
                                                          {
                                                              Id = verse.Id,
                                                              PoemId = verse.PoemId,
                                                              VOrder = verse.VOrder,
                                                              TajikText = TajikTransilerator.Transilerate(verse.Text, formData, out error),
                                                          };
                                                          if(!string.IsNullOrEmpty(error))
                                                          {
                                                              await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"{error} - {verse.Text}");
                                                              return;
                                                          }
                                                          context.Add(tajikVerse);
                                                      }
                                                  }

                                                  if (false == await context.TajikPoems.Where(p => p.Id == poem.Id).AnyAsync())
                                                  {
                                                      GanjoorTajikPoem tajikPoem = new GanjoorTajikPoem()
                                                      {
                                                          Id = poem.Id,
                                                          CatId = poem.CatId,
                                                          TajikTitle = TajikTransilerator.Transilerate(poem.Title, formData, out error),
                                                      };
                                                      if (!string.IsNullOrEmpty(error))
                                                      {
                                                          await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"{error} - {poem.Title}");
                                                          return;
                                                      }

                                                      context.Add(tajikPoem);
                                                  }

                                                  await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname + $" - cat: {catId} - poem: {poem.Id}");
                                              }

                                              if (false == await context.TajikCats.Where(c => c.Id == catId).AnyAsync())
                                              {
                                                  var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == catId).SingleAsync();
                                                  string error2 = "" ;
                                                  GanjoorTajikCat tajikCat = new GanjoorTajikCat()
                                                  {
                                                      Id = catId,
                                                      TajikTitle = TajikTransilerator.Transilerate(cat.Title, formData, out error),
                                                      TajikDescription = TajikTransilerator.Transilerate(cat.Description, formData, out error2),
                                                  };
                                                  if (!string.IsNullOrEmpty(error))
                                                  {
                                                      await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"{error} - {cat.Title}");
                                                      return;
                                                  }

                                                  if (!string.IsNullOrEmpty(error2))
                                                  {
                                                      await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"{error2} - {cat.Description}");
                                                      return;
                                                  }

                                                  context.Add(tajikCat);
                                                  await context.SaveChangesAsync();
                                              }
                                          }

                                          if (false == await context.TajikPoets.Where(p => p.Id == poet.Id).AnyAsync())
                                          {
                                              string error2 = "";
                                              GanjoorTajikPoet tajikPoet = new GanjoorTajikPoet()
                                              {
                                                  Id = poet.Id,
                                                  TajikNickname = TajikTransilerator.Transilerate(poet.Nickname, formData, out error),
                                                  TajikDescription = TajikTransilerator.Transilerate(poet.Description, formData, out error2),
                                              };
                                              if (!string.IsNullOrEmpty(error))
                                              {
                                                  await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"{error} - {poet.Nickname}");
                                                  return;
                                              }

                                              if (!string.IsNullOrEmpty(error2))
                                              {
                                                  await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"{error2} - {poet.Description}");
                                                  return;
                                              }
                                              context.Add(tajikPoet);
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


        /// <summary>
        /// one time fix for transilerations
        /// </summary>
        public void FixTransilerations()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                          (
                          async token =>
                          {
                              using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                              {
                                  LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                  var job = (await jobProgressServiceEF.NewJob($"FixTransilerations", "Query data")).Result;
                                  try
                                  {
                                      
                                      var tajikPoets = await context.TajikPoets.Where(p => p.Id > 0).ToListAsync();
                                      foreach (var tajikPoet in tajikPoets)
                                      {
                                          var poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == tajikPoet.Id).SingleAsync();
                                          tajikPoet.BirthYearInLHijri = poet.BirthYearInLHijri;
                                          context.Update(tajikPoet);
                                          await context.SaveChangesAsync();
                                      }
                                      await jobProgressServiceEF.UpdateJob(job.Id, 1, "cats");

                                      var tajikCats = await context.TajikCats.Where(c => c.Id > 0).ToListAsync();
                                      foreach (var tajikCat in tajikCats)
                                      {
                                          var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == tajikCat.Id).SingleAsync();
                                          tajikCat.PoetId = cat.PoetId;
                                          context.Update(tajikCat);
                                          await context.SaveChangesAsync();
                                      }

                                      await jobProgressServiceEF.UpdateJob(job.Id, 2, "poems");

                                      var tajikPoems = await context.TajikPoems.ToListAsync();
                                      foreach(var tajikPoem in tajikPoems)
                                      {
                                          var poem = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == tajikPoem.Id).SingleAsync();
                                          tajikPoem.FullUrl = poem.FullUrl;

                                          string fullTitle = tajikPoem.TajikTitle;

                                          int catId = tajikPoem.CatId;
                                          while(catId != 0)
                                          {
                                              var tajikCat = await context.TajikCats.AsNoTracking().Where(c => c.Id == catId).SingleAsync();
                                              var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == tajikCat.Id).SingleAsync();
                                              
                                              fullTitle = tajikCat.TajikTitle + " - " + fullTitle;
                                              catId = cat.ParentId ?? 0;
                                          }

                                          tajikPoem.FullTitle = fullTitle;
                                          context.Update(tajikPoem);
                                          await context.SaveChangesAsync();
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
        private async Task<string> PrepareTajikPoetHtmlTextAsync(RMuseumDbContext context, GanjoorTajikPoet poet)
        {

            string[] lines = poet.TajikDescription.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            string html = "";
            foreach (var line in lines)
            {
                html += $"<p>{line}</p>";
            }
            var poetCat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poet.Id && c.ParentId == null).SingleAsync();
            var subCats = await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == poetCat.Id).OrderBy(p => p.Id).ToListAsync();
            foreach (var subCat in subCats)
            {
                var tajikCat = await context.TajikCats.AsNoTracking().Where(t => t.Id == subCat.Id).SingleAsync();
                html += $"<p><a href=\"{subCat.FullUrl}\">{LanguageUtils.CleanTextForTransileration(tajikCat.TajikTitle)}</a></p>";
            }

            var catPoems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == poetCat.Id).OrderBy(p => p.Id).ToListAsync();
            var tajikPoems = await context.TajikPoems.AsNoTracking().Where(p => p.CatId == poetCat.Id).OrderBy(p => p.Id).ToListAsync();
            foreach (var catPoem in catPoems)
            {
                var tajikPoem = tajikPoems.Where(p => p.Id == catPoem.Id).SingleOrDefault();
                if(tajikPoem != null)
                {
                    html += $"<p><a href=\"{catPoem.FullUrl}\">{LanguageUtils.CleanTextForTransileration(tajikPoem.TajikTitle)}</a></p>";
                }
                
            }
            return html;
        }

        private async Task<string> PrepareTajikCatHtmlTextAsync(RMuseumDbContext context, GanjoorTajikCat cat)
        {
            if(cat.TajikDescription == null)
            {
                cat.TajikDescription = "";
            }
            string[] lines = cat.TajikDescription.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            string html = "";
            foreach (var line in lines)
            {
                html += $"<p>{line}</p>";
            }
            var subCats = await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == cat.Id).OrderBy(p => p.Id).ToListAsync();
            foreach (var subCat in subCats)
            {
                var tajikCat = await context.TajikCats.AsNoTracking().Where(t => t.Id == subCat.Id).SingleAsync();
                html += $"<p><a href=\"{subCat.FullUrl}\">{LanguageUtils.CleanTextForTransileration(tajikCat.TajikTitle)}</a></p>";
            }

            var catPoems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == cat.Id).OrderBy(p => p.Id).ToListAsync();
            var tajikPoems = await context.TajikPoems.AsNoTracking().Where(p => p.CatId == cat.Id).OrderBy(p => p.Id).ToListAsync();
            foreach (var catPoem in catPoems)
            {
                var tajikPoem = tajikPoems.Where(p => p.Id == catPoem.Id).SingleOrDefault();
                if(tajikPoem != null)
                {
                    html += $"<p><a href=\"{catPoem.FullUrl}\">{LanguageUtils.CleanTextForTransileration(tajikPoem.TajikTitle)}</a></p>";
                }
            }
            return html;
        }
    }


}
