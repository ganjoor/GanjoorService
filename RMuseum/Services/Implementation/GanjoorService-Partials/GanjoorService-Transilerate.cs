using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;
using RMuseum.DbContext;
using System.Collections.Generic;
using RSecurityBackend.Services.Implementation;
using RMuseum.Utils;

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
                                      var poets = await context.GanjoorPoets.Where(p => p.Published).OrderBy(p => p.Id).ToListAsync();
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
                                              var poems = await context.GanjoorPoems.Where(p => p.CatId == catId).OrderBy(p => p.Id).ToListAsync();
                                              foreach (var poem in poems)
                                              {
                                                  var verses = await context.GanjoorVerses.Where(v => v.PoemId == poem.Id && string.IsNullOrEmpty(v.Tajik)).OrderBy(v => v.VOrder).ToListAsync();
                                                  if(verses.Any())
                                                  {
                                                      foreach (var verse in verses)
                                                      {
                                                          verse.Tajik = TajikTransilerator.Transilerate(verse.Text);
                                                      }
                                                      context.UpdateRange(verses);
                                                  }

                                                  if (string.IsNullOrEmpty(poem.TajikTitle))
                                                  {
                                                      poem.TajikTitle = TajikTransilerator.Transilerate(poem.Title);
                                                      context.Update(poem);
                                                  }

                                                  await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, poet.Nickname + $" - cat: {catId} - poem: {poem.Id}");
                                              }

                                              var cat = await context.GanjoorCategories.Where(c => c.Id == catId && string.IsNullOrEmpty(c.TajikTitle)).SingleOrDefaultAsync();
                                              if (cat != null)
                                              {
                                                  cat.TajikTitle = TajikTransilerator.Transilerate(cat.Title);
                                                  cat.TajikDescription = TajikTransilerator.Transilerate(cat.Description);
                                                  context.Update(cat);
                                                  await context.SaveChangesAsync();
                                              }
                                          }

                                          if (string.IsNullOrEmpty(poet.TajikNickName))
                                          {
                                              poet.TajikNickName = TajikTransilerator.Transilerate(poet.Nickname);
                                              poet.TajikDescription = TajikTransilerator.Transilerate(poet.Description);
                                              context.Update(poet);
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

    }
}