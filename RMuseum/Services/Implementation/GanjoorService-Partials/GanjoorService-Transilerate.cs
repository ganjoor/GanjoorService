using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;
using RMuseum.DbContext;
using System.Collections.Generic;
using RSecurityBackend.Services.Implementation;
using RMuseum.Utils;
using RMuseum.Models.Ganjoor;
using NAudio.Gui;

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
                                                          context.Add(tajikVerse);
                                                      }
                                                  }

                                                  if (false == await context.TajikPoems.Where(p => p.Id == poem.Id).AnyAsync())
                                                  {
                                                      GanjoorTajikPoem tajikPoem = new GanjoorTajikPoem()
                                                      {
                                                          Id = poem.Id,
                                                          CatId = poem.CatId,
                                                          TajikTitle = TajikTransilerator.Transilerate(poem.Title, formData),
                                                      };
                                                      context.Add(tajikPoem);
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
                                                  context.Add(tajikCat);
                                                  await context.SaveChangesAsync();
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
                                      
                                      var poets = await context.TajikPoets.ToListAsync();
                                      foreach (var poet in poets)
                                      {
                                          poet.TajikNickname = LanguageUtils.CleanTextForTransileration(poet.TajikNickname);
                                          poet.TajikDescription = LanguageUtils.CleanTextForTransileration(poet.TajikDescription);
                                          context.Update(poet);
                                          await context.SaveChangesAsync();
                                      }

                                      var cats = await context.TajikCats.ToListAsync();
                                      foreach (var cat in cats)
                                      {
                                          cat.TajikTitle = LanguageUtils.CleanTextForTransileration(cat.TajikTitle);
                                          cat.TajikDescription = LanguageUtils.CleanTextForTransileration(cat.TajikTitle);
                                          context.Update(cat);
                                          await context.SaveChangesAsync();
                                      }

                                      var poems = await context.TajikPoems.ToListAsync();
                                      foreach(var poem in poems)
                                      {
                                          poem.TajikTitle = LanguageUtils.CleanTextForTransileration(poem.TajikTitle);
                                          context.Update(poem);
                                          await context.SaveChangesAsync();
                                      }

                                      var verses = await context.TajikVerses.ToListAsync();
                                      foreach (var verse in verses)
                                      {
                                          verse.TajikText = LanguageUtils.CleanTextForTransileration(verse.TajikText);
                                          context.Update(verse);
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

    }


}
