using Microsoft.EntityFrameworkCore;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using RMuseum.Models.GanjoorIntegration;
using RMuseum.DbContext;
using RSecurityBackend.Services.Implementation;
using RSecurityBackend.Models.Generic.Db;
using System.Collections.Generic;


namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// category paper sources
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPaperSource[]>> GetCategoryPaperSourcesAsync(int categoryId)
        {
            try
            {
                return new RServiceResult<GanjoorPaperSource[]>
                    (
                    await _context.GanjoorPaperSources.AsNoTracking().Where(p => p.GanjoorCatId == categoryId).OrderByDescending(c => c.IsTextOriginalSource).OrderBy(c => c.OrderIndicator).ThenBy(c => c.Id).ToArrayAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPaperSource[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// import paper sources from museum
        /// </summary>
        public void ImportPaperSourcesFromMuseum()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                   var job = (await jobProgressServiceEF.NewJob("ImportPaperSourcesFromMuseum", "Query data")).Result;
                                   await _ImportPaperSourcesFromMuseumAsync(context, jobProgressServiceEF, job);
                                   
                               }
                           });
        }

        private async Task _ImportPaperSourcesFromMuseumAsync(RMuseumDbContext context, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job)
        {
            try
            {
                var artifacts = await context.Artifacts.AsNoTracking().Include(c => c.CoverImage).ToListAsync();
                int index = 0;
                foreach (var artifact in artifacts)
                {
                    await jobProgressServiceEF.UpdateJob(job.Id, index++, $"{index} از {artifacts.Count} - {artifact.Name}");
                    var links = await context.GanjoorLinks.AsNoTracking().Where(l => l.ArtifactId == artifact.Id).ToListAsync();

                    Dictionary<int, int> poets = new Dictionary<int, int>();
                    foreach (var link in links)
                    {
                        var poem = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == link.GanjoorPostId).SingleOrDefaultAsync();
                        if(poem == null) continue;
                        var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == poem.CatId).SingleOrDefaultAsync();
                        if(cat == null) continue;
                        if(poets.ContainsKey(cat.PoetId))
                        {
                            poets[cat.PoetId]++;
                        }
                        else
                        {
                            poets[cat.PoetId] = 1;
                        }
                    }

                    if(poets.Count > 0)
                    {
                        var mainPoet = poets.First();
                        foreach (var poet in poets)
                        {
                            if(poet.Value > mainPoet.Value)
                            {
                                mainPoet = poet;
                            }
                        }

                        if(mainPoet.Value > 3)
                        {
                            var poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == mainPoet.Key).SingleAsync();
                            var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poet.Id & c.ParentId == null).SingleAsync();
                            GanjoorPaperSource paperSource = new GanjoorPaperSource()
                            {
                                GanjoorPoetId = poet.Id,
                                GanjoorCatId = cat.Id,
                                GanjoorCatFullTitle = poet.Nickname,
                                GanjoorCatFullUrl = cat.FullUrl,
                                BookType = LinkType.Museum,
                                BookFullUrl = $"https://museum.ir/items/{artifact.FriendlyUrl}",
                                NaskbanBookId = 0,
                                BookFullTitle = artifact.Name,
                                CoverThumbnailImageUrl = artifact.CoverImage.ExternalNormalSizeImageUrl.Replace("/norm/", "/thumb/").Replace("/orig/", "/thumb/"),
                                Description = "",
                                IsTextOriginalSource = await context.GanjoorLinks.AsNoTracking().Where(l => l.ArtifactId == artifact.Id && l.IsTextOriginalSource == true).AnyAsync(),
                                MatchPercent = 100,
                                HumanReviewed = true,
                                OrderIndicator = 1,
                            };
                            context.GanjoorPaperSources.Add(paperSource);
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
    }
}
