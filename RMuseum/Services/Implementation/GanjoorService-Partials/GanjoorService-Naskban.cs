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
        /// <returns>number of synched items</returns>
        public async Task<RServiceResult<int>> SynchronizeNaskbanLinksAsync(Guid ganjoorUserId, string naskbanUserName, string naskbanPassword)
        {
            try
            {
                LoginViewModel loginViewModel = new LoginViewModel()
                {
                    Username = naskbanUserName,
                    Password = naskbanPassword,
                    ClientAppName = "Ganjoor API",
                    Language = "fa-IR"
                };
                var loginResponse = await _httpClient.PostAsync("https://api.naskban.ir/api/users/login", new StringContent(JsonConvert.SerializeObject(loginViewModel), Encoding.UTF8, "application/json"));

                if (loginResponse.StatusCode != HttpStatusCode.OK)
                {
                    return new RServiceResult<int>(0, "login error: " + JsonConvert.DeserializeObject<string>(await loginResponse.Content.ReadAsStringAsync()));
                }
                LoggedOnUserModelEx loggedOnUser = JsonConvert.DeserializeObject<LoggedOnUserModelEx>(await loginResponse.Content.ReadAsStringAsync());

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
                            await _context.GanjoorLinks.Where(l => l.GanjoorPostId == unsynchronized.GanjoorPostId && l.IsTextOriginalSource).AnyAsync() == false
                            &&
                            await _context.PinterestLinks.Where(l => l.GanjoorPostId == unsynchronized.GanjoorPostId && l.IsTextOriginalSource).AnyAsync() == false
                            ;
                        if (false == await _context.PinterestLinks.Where(p => p.NaskbanLinkId == unsynchronized.Id).AnyAsync())
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
                            _context.PinterestLinks.Add(link);
                            await _context.SaveChangesAsync();
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
    }
}