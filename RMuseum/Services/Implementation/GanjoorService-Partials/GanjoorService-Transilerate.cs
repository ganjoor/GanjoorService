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
using RMuseum.Migrations;

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
                                      var poets = await context.GanjoorPoets.AsNoTracking().Where(p => p.Published).OrderBy(p => p.Id).ToListAsync();
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
                                                  bool error = false;
                                                  foreach (var verse in verses)
                                                  {
                                                      if (false == await context.TajikVerses.Where(t => t.Id == verse.Id).AnyAsync())
                                                      {
                                                          GanjoorTajikVerse tajikVerse = new GanjoorTajikVerse()
                                                          {
                                                              Id = verse.Id,
                                                              PoemId = verse.PoemId,
                                                              VOrder = verse.VOrder,
                                                              TajikText = TajikTransilerator.Transilerate(verse.Text, formData),
                                                          };
                                                          if (!string.IsNullOrEmpty(verse.Text) && string.IsNullOrEmpty(tajikVerse.TajikText))
                                                          {
                                                              error = true;
                                                              break;
                                                          }
                                                          context.Add(tajikVerse);
                                                          verse.Text = tajikVerse.TajikText;
                                                      }
                                                  }

                                                  if (!error && false == await context.TajikPoems.Where(p => p.Id == poem.Id).AnyAsync())
                                                  {
                                                      GanjoorTajikPoem tajikPoem = new GanjoorTajikPoem()
                                                      {
                                                          Id = poem.Id,
                                                          CatId = poem.CatId,
                                                          TajikTitle = TajikTransilerator.Transilerate(poem.Title, formData),
                                                          TajikPlainText = PreparePlainText(verses)
                                                      };
                                                      if (string.IsNullOrEmpty(tajikPoem.TajikTitle))
                                                          continue;
                                                      context.Add(tajikPoem);

                                                      GanjoorTajikPage page = new GanjoorTajikPage()
                                                      {
                                                          Id = tajikPoem.Id,
                                                          TajikHtmlText = PrepareHtmlText(verses)
                                                      };
                                                      context.Add(page);

                                                  }

                                                  await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname + $" - cat: {catId} - poem: {poem.Id}");
                                              }

                                              if (false == await context.TajikCats.Where(c => c.Id == catId).AnyAsync())
                                              {
                                                  var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == catId).SingleAsync();
                                                  GanjoorTajikCat tajikCat = new GanjoorTajikCat()
                                                  {
                                                      Id = catId,
                                                      TajikTitle = TajikTransilerator.Transilerate(cat.Title, formData),
                                                      TajikDescription = TajikTransilerator.Transilerate(cat.Description, formData),
                                                  };
                                                  if (string.IsNullOrEmpty(tajikCat.TajikTitle)) continue;
                                                  context.Add(tajikCat);
                                                  await context.SaveChangesAsync();

                                                  var catPage = await context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == cat.Id).SingleOrDefaultAsync();
                                                  if (catPage != null)
                                                  {
                                                      GanjoorTajikPage page = new GanjoorTajikPage()
                                                      {
                                                          Id = catPage.Id,
                                                          TajikHtmlText = await PrepareTajikCatHtmlTextAsync(context, tajikCat),
                                                      };
                                                      context.Add(page);
                                                      await context.SaveChangesAsync();
                                                  }
                                              }
                                          }

                                          if (false == await context.TajikPoets.Where(p => p.Id == poet.Id).AnyAsync())
                                          {
                                              GanjoorTajikPoet tajikPoet = new GanjoorTajikPoet()
                                              {
                                                  Id = poet.Id,
                                                  TajikNickname = TajikTransilerator.Transilerate(poet.Nickname, formData),
                                                  TajikDescription = TajikTransilerator.Transilerate(poet.Description, formData),
                                              };
                                              if (string.IsNullOrEmpty(tajikPoet.TajikNickname)) continue;
                                              context.Add(tajikPoet);
                                              await context.SaveChangesAsync();

                                              var poetPage = await context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.PoetPage && p.PoetId == poet.Id).SingleAsync();
                                              GanjoorTajikPage page = new GanjoorTajikPage()
                                              {
                                                  Id = poetPage.Id,
                                                  TajikHtmlText = await PrepareTajikPoetHtmlTextAsync(context, tajikPoet),
                                              };
                                              context.Add(page);
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
                var tajikCat = await context.TajikCats.AsNoTracking().Where(t => t.Id == subCat.Id).SingleOrDefaultAsync();
                if(tajikCat == null)
                {
                    html += $"<p><a href=\"{subCat.FullUrl}\">{LanguageUtils.CleanTextForTransileration(tajikCat.TajikTitle)}</a></p>";
                }
            }

            var catPoems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == poetCat.Id).OrderBy(p => p.Id).ToListAsync();
            var tajikPoems = await context.TajikPoems.AsNoTracking().Where(p => p.CatId == poetCat.Id).OrderBy(p => p.Id).ToListAsync();
            foreach (var catPoem in catPoems)
            {
                var tajikPoem = tajikPoems.Where(p => p.Id == catPoem.Id).Single();
                html += $"<p><a href=\"{catPoem.FullUrl}\">{LanguageUtils.CleanTextForTransileration(tajikPoem.TajikTitle)}</a></p>";
            }
            return html;
        }

        private async Task<string> PrepareTajikCatHtmlTextAsync(RMuseumDbContext context, GanjoorTajikCat cat)
        {

            string[] lines = cat.TajikDescription.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            string html = "";
            foreach (var line in lines)
            {
                html += $"<p>{line}</p>";
            }
            var subCats = await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == cat.Id).OrderBy(p => p.Id).ToListAsync();
            foreach (var subCat in subCats)
            {
                var tajikCat = await context.TajikCats.AsNoTracking().Where(t => t.Id == subCat.Id).SingleOrDefaultAsync();
                if (tajikCat != null)
                {
                    html += $"<p><a href=\"{subCat.FullUrl}\">{LanguageUtils.CleanTextForTransileration(tajikCat.TajikTitle)}</a></p>";
                }
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
