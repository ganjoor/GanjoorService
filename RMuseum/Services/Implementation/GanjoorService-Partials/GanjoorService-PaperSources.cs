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
using Newtonsoft.Json;
using RMuseum.Models.PDFLibrary;
using System.Net.Http;
using System.Net;
using System.Reflection.PortableExecutable;
using RMuseum.Models.Auth.ViewModel;
using RSecurityBackend.Models.Auth.ViewModels;
using System.Text;
using System.Net.Http.Headers;


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
        /// discover poet naskban paper sources
        /// </summary>
        /// <param name="poetid"></param>
        /// <param name="naskbanUserName"></param>
        /// <param name="naskbanPassword"></param>
        public void DiscoverPoetNaskbanPaperSources(int poetid, string naskbanUserName, string naskbanPassword)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                   var job = (await jobProgressServiceEF.NewJob("DiscoverCategoryPaperSources", "Query data")).Result;
                                   try
                                   {
                                       var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poetid && c.ParentId == null).SingleAsync();
                                       List<int> catIdList = new List<int>
                                               {
                                                   cat.Id
                                               };
                                       await _populateCategoryChildren(context, cat.Id, catIdList);

                                       LoggedOnUserModelEx loggedOnUser;
                                       using (HttpClient client = new HttpClient())
                                       {
                                           LoginViewModel loginViewModel = new LoginViewModel()
                                           {
                                               Username = naskbanUserName,
                                               Password = naskbanPassword,
                                               ClientAppName = "Ganjoor API",
                                               Language = "fa-IR"
                                           };
                                           var loginResponse = await client.PostAsync("https://api.naskban.ir/api/users/login", new StringContent(JsonConvert.SerializeObject(loginViewModel), Encoding.UTF8, "application/json"));

                                           if (loginResponse.StatusCode != HttpStatusCode.OK)
                                           {
                                               await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "login error: " + JsonConvert.DeserializeObject<string>(await loginResponse.Content.ReadAsStringAsync()));
                                               return;
                                           }
                                           loggedOnUser = JsonConvert.DeserializeObject<LoggedOnUserModelEx>(await loginResponse.Content.ReadAsStringAsync());
                                       }

                                       using (HttpClient secureClient = new HttpClient())
                                       {
                                           secureClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loggedOnUser.Token);
                                           var unsyncedResponse = await secureClient.GetAsync("https://api.naskban.ir/api/pdf/ganjoor/matching?notStarted=false&notFinished=false");
                                           if (!unsyncedResponse.IsSuccessStatusCode)
                                           {
                                               await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "unsync error: " + JsonConvert.DeserializeObject<string>(await unsyncedResponse.Content.ReadAsStringAsync()));
                                               return;
                                           }
                                           foreach (int subCatId in catIdList)
                                           {
                                               await jobProgressServiceEF.UpdateJob(job.Id, subCatId);
                                               var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == subCatId).ToListAsync();
                                               foreach (var poem in poems)
                                               {
                                                   var sources = await context.PinterestLinks.Where(l => l.GanjoorPostId == poem.Id && l.NaskbanLinkId != null).ToListAsync();
                                                   foreach (var matching in sources)
                                                   {
                                                       if (false == await context.GanjoorPaperSources.Where(p => p.NaskbanBookId == matching.PDFBookId && p.GanjoorCatId == cat.Id).AnyAsync())
                                                       {
                                                           HttpResponseMessage responseBook = await secureClient.GetAsync($"https://api.naskban.ir/api/pdf/{matching.PDFBookId}?includePages=false&includeBookText=false&includePageText=false");
                                                           if (responseBook.StatusCode != HttpStatusCode.OK)
                                                           {
                                                               await jobProgressServiceEF.UpdateJob(job.Id, 1000, "", false, $"book fetch error bookid = {matching.PDFBookId} matching id = {matching.Id} - " + JsonConvert.DeserializeObject<string>(await responseBook.Content.ReadAsStringAsync()));
                                                               return;
                                                           }
                                                           responseBook.EnsureSuccessStatusCode();

                                                           var book = JsonConvert.DeserializeObject<PDFBook>(await responseBook.Content.ReadAsStringAsync());

                                                           GanjoorPaperSource paperSource = new GanjoorPaperSource()
                                                           {
                                                               GanjoorPoetId = cat.PoetId,
                                                               GanjoorCatId = cat.Id,
                                                               GanjoorCatFullTitle = cat.Title,
                                                               GanjoorCatFullUrl = cat.FullUrl,
                                                               BookType = LinkType.Naskban,
                                                               BookFullUrl = $"https://naskban.ir/{matching.PDFBookId}",
                                                               NaskbanBookId = matching.PDFBookId,
                                                               BookFullTitle = book.Title,
                                                               CoverThumbnailImageUrl = book.ExtenalCoverImageUrl,
                                                               Description = "",
                                                               IsTextOriginalSource = matching.IsTextOriginalSource,
                                                               MatchPercent = matching.MatchPercent,
                                                               HumanReviewed = matching.HumanReviewed,
                                                               OrderIndicator = 0,
                                                           };
                                                           context.GanjoorPaperSources.Add(paperSource);
                                                           await context.SaveChangesAsync();
                                                       }
                                                   }
                                               }
                                           }
                                       }
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, succeeded: true);
                                   }
                                   catch (Exception exp)
                                   {
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());

                                   }

                               }

                           }
                           );
        }

        /// <summary>
        /// import paper sources from museum
        /// </summary>
        /// <param name="poetid"></param>
        public void ImportPaperSourcesFromMuseum(int poetid)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                   var job = (await jobProgressServiceEF.NewJob("ImportPaperSourcesFromMuseum", "Query data")).Result;
                                   await _ImportPaperSourcesFromMuseumAsync(context, jobProgressServiceEF, job, poetid);

                               }
                           });
        }

        private async Task _ImportPaperSourcesFromMuseumAsync(RMuseumDbContext context, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job, int poetid)
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
                        if (poem == null) continue;
                        var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == poem.CatId).SingleOrDefaultAsync();
                        if (cat == null) continue;
                        if(poetid !=0)
                        {
                            if(cat.PoetId != poetid) continue;
                        }
                        if (poets.ContainsKey(cat.PoetId))
                        {
                            poets[cat.PoetId]++;
                        }
                        else
                        {
                            poets[cat.PoetId] = 1;
                        }
                    }

                    if (poets.Count > 0)
                    {
                        var mainPoet = poets.First();
                        foreach (var poet in poets)
                        {
                            if (poet.Value > mainPoet.Value)
                            {
                                mainPoet = poet;
                            }
                        }

                        if (mainPoet.Value > 3)
                        {
                            var poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == mainPoet.Key).SingleAsync();
                            var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poet.Id & c.ParentId == null).SingleAsync();
                            if (false == await context.GanjoorPaperSources.Where(p => p.BookFullUrl == $"https://museum.ganjoor.net/items/{artifact.FriendlyUrl}" && p.GanjoorCatId == cat.Id).AnyAsync())
                            {
                                GanjoorPaperSource paperSource = new GanjoorPaperSource()
                                {
                                    GanjoorPoetId = poet.Id,
                                    GanjoorCatId = cat.Id,
                                    GanjoorCatFullTitle = poet.Nickname,
                                    GanjoorCatFullUrl = cat.FullUrl,
                                    BookType = LinkType.Museum,
                                    BookFullUrl = $"https://museum.ganjoor.net/items/{artifact.FriendlyUrl}",
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
