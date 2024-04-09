using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RMuseum.Models.Auth.ViewModel;
using RMuseum.Models.GanjoorIntegration;
using RSecurityBackend.Models.Auth.ViewModels;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using RMuseum.DbContext;
using RSecurityBackend.Services.Implementation;
using RSecurityBackend.Models.Generic.Db;
using RMuseum.Models.PDFLibrary;
using DNTPersianUtils.Core;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// synchronize naskban links
        /// </summary>
        /// <param name="ganjoorUserId"></param>
        /// <param name="naskbanUserName"></param>
        /// <param name="naskbanPassword"></param>
        public void SynchronizeNaskbanLinks(Guid ganjoorUserId, string naskbanUserName, string naskbanPassword)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                   var job = (await jobProgressServiceEF.NewJob("SynchronizeNaskbanLinks", "Query data")).Result;
                                   var res = await _SynchronizeNaskbanLinksAsync(context, ganjoorUserId, naskbanUserName, naskbanPassword);
                                   if (!string.IsNullOrEmpty(res.ExceptionString))
                                   {
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, res.ExceptionString);
                                   }
                                   else
                                   {
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                   }
                               }
                           });
        }

        /// <summary>
        /// synchronize naskban links
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ganjoorUserId"></param>
        /// <param name="naskbanUserName"></param>
        /// <param name="naskbanPassword"></param>
        /// <returns>number of synched items</returns>
        private async Task<RServiceResult<int>> _SynchronizeNaskbanLinksAsync(RMuseumDbContext context, Guid ganjoorUserId, string naskbanUserName, string naskbanPassword)
        {
            try
            {
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
                        return new RServiceResult<int>(0, "login error: " + JsonConvert.DeserializeObject<string>(await loginResponse.Content.ReadAsStringAsync()));
                    }
                    loggedOnUser = JsonConvert.DeserializeObject<LoggedOnUserModelEx>(await loginResponse.Content.ReadAsStringAsync());
                }
                

                using (HttpClient secureClient = new HttpClient())
                {
                    secureClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loggedOnUser.Token);
                    var unsyncedResponse = await secureClient.GetAsync("https://api.naskban.ir/api/pdf/ganjoor/unsynched");
                    if (!unsyncedResponse.IsSuccessStatusCode)
                    {
                        return new RServiceResult<int>(0, "unsync error: " + JsonConvert.DeserializeObject<string>(await unsyncedResponse.Content.ReadAsStringAsync()));
                    }
                    var unsynchronizeds = JsonConvert.DeserializeObject<PDFGanjoorLink[]>(await unsyncedResponse.Content.ReadAsStringAsync());
                    foreach (var unsynchronized in unsynchronizeds)
                    {
                        bool isTextOriginalSource =
                            unsynchronized.IsTextOriginalSource
                            &&
                            await context.GanjoorLinks.Where(l => l.GanjoorPostId == unsynchronized.GanjoorPostId && l.IsTextOriginalSource).AnyAsync() == false
                            &&
                            await context.PinterestLinks.Where(l => l.GanjoorPostId == unsynchronized.GanjoorPostId && l.IsTextOriginalSource).AnyAsync() == false
                            ;
                        if (false == await context.PinterestLinks.Where(p => p.NaskbanLinkId == unsynchronized.Id).AnyAsync())
                        {
                            PinterestLink link = new PinterestLink()
                            {
                                GanjoorPostId = unsynchronized.GanjoorPostId,
                                GanjoorTitle = unsynchronized.GanjoorTitle,
                                GanjoorUrl = unsynchronized.GanjoorUrl,
                                AltText = unsynchronized.PDFPageTitle,
                                LinkType = LinkType.Naskban,
                                PinterestUrl = $"https://naskban.ir/{unsynchronized.PDFBookId}/{unsynchronized.PageNumber}",
                                PinterestImageUrl = unsynchronized.ExternalThumbnailImageUrl,
                                ReviewResult = ReviewResult.Approved,
                                SuggestionDate = DateTime.Now,
                                SuggestedById = ganjoorUserId,
                                Synchronized = true,
                                ReviewerId = ganjoorUserId,
                                IsTextOriginalSource = isTextOriginalSource,
                                PDFBookId = unsynchronized.PDFBookId,
                                PageNumber = unsynchronized.PageNumber,
                                NaskbanLinkId = unsynchronized.Id
                            };
                            context.PinterestLinks.Add(link);
                            await context.SaveChangesAsync();
                        }
                        await secureClient.PutAsync($"https://api.naskban.ir/api/pdf/ganjoor/sync/{unsynchronized.Id}", null);
                    }

                    var logoutUrl = $"https://api.naskban.ir/api/users/delsession?userId={loggedOnUser.User.Id}&sessionId={loggedOnUser.SessionId}";
                    await secureClient.DeleteAsync(logoutUrl);
                    return new RServiceResult<int>(unsynchronizeds.Length);
                }
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }

        /// <summary>
        /// delete poem related naskban images by url
        /// </summary>
        /// <param name="naskbanUrl"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeletePoemRelatedNaskbanImagesByNaskbanUrlAsync(string naskbanUrl)
        {
            try
            {
                var images = await _context.PinterestLinks.Where(l => l.PinterestUrl == naskbanUrl && l.LinkType == LinkType.Naskban).ToListAsync();
                if (images.Count > 0)
                {
                    _context.RemoveRange(images);
                    await _context.SaveChangesAsync();
                }
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// justify naskban links
        /// </summary>
        /// <param name="naskbanUserName"></param>
        /// <param name="naskbanPassword"></param>
        public void JustifyNaskbanPageNumbers(string naskbanUserName, string naskbanPassword)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                   var job = (await jobProgressServiceEF.NewJob("JustifyNaskbanPageNumbers", "Query data")).Result;
                                   var res = await _JustifyNaskbanPageNumbersAsync(context, naskbanUserName, naskbanPassword, jobProgressServiceEF, job);
                                   if (!string.IsNullOrEmpty(res.ExceptionString))
                                   {
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, res.ExceptionString);
                                   }
                                   else
                                   {
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                   }
                               }
                           });
        }

        private async Task<RServiceResult<bool>> _JustifyNaskbanPageNumbersAsync(RMuseumDbContext context, string naskbanUserName, string naskbanPassword, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job)
        {
            try
            {
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
                        return new RServiceResult<int>(0, "login error: " + JsonConvert.DeserializeObject<string>(await loginResponse.Content.ReadAsStringAsync()));
                    }
                    loggedOnUser = JsonConvert.DeserializeObject<LoggedOnUserModelEx>(await loginResponse.Content.ReadAsStringAsync());
                }


                using (HttpClient secureClient = new HttpClient())
                {
                    secureClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loggedOnUser.Token);
                   

                    int firstPoemId = 0;
                    var optionName = "LastJustifiedNaskbanLinkPoemId";
                    RGenericOption lastJustifiedNaskbanLinkPoemIdGenericOption = await (from o in context.Options.AsNoTracking()
                                                           where o.Name == optionName && o.RAppUserId == null
                                                           select o).SingleOrDefaultAsync();
                    if(lastJustifiedNaskbanLinkPoemIdGenericOption != null)
                    {
                        firstPoemId = int.Parse(lastJustifiedNaskbanLinkPoemIdGenericOption.Value);
                    }

                    var naskbanLinks = await context.PinterestLinks.AsNoTracking().Where(l => l.LinkType == LinkType.Naskban && l.GanjoorPostId > firstPoemId).OrderBy(l => l.GanjoorPostId).ToListAsync();

                    if(firstPoemId == 0 && naskbanLinks.Any() )
                    {
                        firstPoemId = naskbanLinks.First().GanjoorPostId;
                    }
                    int progress = 0;
                    foreach ( var naskbanLink in naskbanLinks )
                    {
                        progress++;
                        if(naskbanLink.GanjoorPostId != firstPoemId )
                        {
                            var option = await context.Options.Where(o => o.Id == lastJustifiedNaskbanLinkPoemIdGenericOption.Id).SingleAsync();
                            option.Value = firstPoemId.ToString();
                            context.Update(option);
                            await jobProgressServiceEF.UpdateJob(job.Id, progress, $"{progress} of {naskbanLinks.Count}");
                            firstPoemId = naskbanLink.GanjoorPostId;
                        }
                        var naskbanPageResponse = await secureClient.GetAsync($"https://api.naskban.ir/api/pdf/{naskbanLink.PDFBookId}/page/{naskbanLink.PageNumber}");
                        if (!naskbanPageResponse.IsSuccessStatusCode)
                        {
                            return new RServiceResult<bool>(false, "naskbanPageResponse error: " + JsonConvert.DeserializeObject<string>(await naskbanPageResponse.Content.ReadAsStringAsync()));
                        }
                        naskbanPageResponse.EnsureSuccessStatusCode();
                        var currentPage = JsonConvert.DeserializeObject<PDFPage>(await naskbanPageResponse.Content.ReadAsStringAsync());

                        HttpResponseMessage responseBook = await secureClient.GetAsync($"https://api.naskban.ir/api/pdf/{naskbanLink.PDFBookId}?includePages=false&includeBookText=false&includePageText=false");
                        if (responseBook.StatusCode != HttpStatusCode.OK)
                        {
                            return new RServiceResult<bool>(false, "book fetch error: " + JsonConvert.DeserializeObject<string>(await responseBook.Content.ReadAsStringAsync()));
                        }
                        responseBook.EnsureSuccessStatusCode();

                        PDFBook book = JsonConvert.DeserializeObject<PDFBook>(await responseBook.Content.ReadAsStringAsync());
                        string bookPage = book.Title;
                        if (!string.IsNullOrEmpty(book.AuthorsLine))
                        {
                            bookPage = $"{book.Title} - {book.AuthorsLine}";
                        }

                        var modifyNaskbanLink = await context.PinterestLinks.Where(l => l.Id == naskbanLink.Id).SingleAsync();
                        modifyNaskbanLink.AltText = $"{bookPage} - صفحهٔ {naskbanLink.PageNumber.ToPersianNumbers()}";

                        var verses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == naskbanLink.GanjoorPostId && v.VersePosition != VersePosition.Comment).OrderBy(v => v.VOrder).ToListAsync();
                        if (!verses.Any()) continue;
                        string verseText = verses.First().Text;
                        if(verses.Count > 1)
                        {
                            verseText += $" {verses[1].Text}";
                        }

                        string[] poemWords = verseText.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (poemWords.Length == 0) continue;

                        string pageText = currentPage.PageText;

                        int found = 0;
                        foreach (var poemWord in poemWords)
                        {
                            if (pageText.Contains(poemWord))
                            {
                                found++;
                            }
                        }
                        var percentMainPage = found * 100 / poemWords.Length;

                        if(percentMainPage < 70)
                        {
                            naskbanPageResponse = await secureClient.GetAsync($"https://api.naskban.ir/api/pdf/{naskbanLink.PDFBookId}/page/{naskbanLink.PageNumber + 1}");
                            if (!naskbanPageResponse.IsSuccessStatusCode)
                            {
                                continue;
                            }
                            naskbanPageResponse.EnsureSuccessStatusCode();

                            var nextPage = JsonConvert.DeserializeObject<PDFPage>(await naskbanPageResponse.Content.ReadAsStringAsync());
                            pageText = nextPage.PageText;

                            found = 0;
                            foreach (var poemWord in poemWords)
                            {
                                if (pageText.Contains(poemWord))
                                {
                                    found++;
                                }
                            }
                            var percentNextPage = found * 100 / poemWords.Length;

                            if(percentNextPage >= 70)
                            {
                                modifyNaskbanLink.PageNumber = nextPage.PageNumber;
                                modifyNaskbanLink.PinterestImageUrl = nextPage.ExtenalThumbnailImageUrl;
                                modifyNaskbanLink.PinterestUrl = $"https://naskban.ir/{nextPage.PDFBookId}/{nextPage.PageNumber}";
                                modifyNaskbanLink.AltText = $"{bookPage} - صفحهٔ {nextPage.PageNumber.ToPersianNumbers()}";
                            }
                            else
                            if(naskbanLink.PageNumber > 1)
                            {
                                naskbanPageResponse = await secureClient.GetAsync($"https://api.naskban.ir/api/pdf/{naskbanLink.PDFBookId}/page/{naskbanLink.PageNumber - 1}");
                                if (!naskbanPageResponse.IsSuccessStatusCode)
                                {
                                    continue;
                                }
                                naskbanPageResponse.EnsureSuccessStatusCode();

                                var prevPage = JsonConvert.DeserializeObject<PDFPage>(await naskbanPageResponse.Content.ReadAsStringAsync());
                                pageText = prevPage.PageText;

                                found = 0;
                                foreach (var poemWord in poemWords)
                                {
                                    if (pageText.Contains(poemWord))
                                    {
                                        found++;
                                    }
                                }
                                var percentPrevPage = found * 100 / poemWords.Length;

                                if (percentPrevPage >= 70)
                                {
                                    modifyNaskbanLink.PageNumber = prevPage.PageNumber;
                                    modifyNaskbanLink.PinterestImageUrl = prevPage.ExtenalThumbnailImageUrl;
                                    modifyNaskbanLink.PinterestUrl = $"https://naskban.ir/{prevPage.PDFBookId}/{prevPage.PageNumber}";
                                    modifyNaskbanLink.AltText = $"{bookPage} - صفحهٔ {prevPage.PageNumber.ToPersianNumbers()}";
                                }
                            }
                        }

                        context.Update(modifyNaskbanLink);
                        await context.SaveChangesAsync();

                    }

                    var logoutUrl = $"https://api.naskban.ir/api/users/delsession?userId={loggedOnUser.User.Id}&sessionId={loggedOnUser.SessionId}";
                    await secureClient.DeleteAsync(logoutUrl);
                    return new RServiceResult<bool>(true);
                }
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
    }
}