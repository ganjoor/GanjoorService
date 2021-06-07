using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]

    public class SearchModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public SearchModel(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }

        public List<GanjoorPoetViewModel> Poets { get; set; }

        public string Query { get; set; }
        public int PoetId { get; set; }
        public int CatId { get; set; }
        public GanjoorPoetCompleteViewModel Poet { get; set; }
        public List<GanjoorPoemCompleteViewModel> Poems { get; set; }
        public string PagingToolsHtml { get; set; }

        /// <summary>
        /// is logged on
        /// </summary>
        public bool LoggedIn { get; set; }

        [BindProperty]
        public LoginViewModel LoginViewModel { get; set; }

        private async Task preparePoets()
        {
            var cacheKey = $"/api/ganjoor/poets";
            if (!_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel> poets))
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
                response.EnsureSuccessStatusCode();
                poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();
                _memoryCache.Set(cacheKey, poets);
            }

            Poets = poets;
        }

        private async Task preparePoet()
        {
            var cacheKey = $"/api/ganjoor/poet/{PoetId}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                var poetResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{PoetId}");
                poetResponse.EnsureSuccessStatusCode();
                poet = JObject.Parse(await poetResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                _memoryCache.Set(cacheKey, poet);
            }

            Poet = poet;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);

            Query = Request.Query["s"].ApplyCorrectYeKe().Trim();
            Query = Query.Replace('‌', ' '); //replace zwnj with space
            PoetId = string.IsNullOrEmpty(Request.Query["author"]) ? 0 : int.Parse(Request.Query["author"]);
            CatId = string.IsNullOrEmpty(Request.Query["cat"]) ? 0 : int.Parse(Request.Query["cat"]);

            //todo: use html master layout or make it partial
            // 1. poets 
            await preparePoets();

            var poetName = Poets.SingleOrDefault(p => p.Id == PoetId);
            if (poetName != null)
            {
                ViewData["Title"] = $"گنجور &raquo; نتایج جستجو برای {Query} &raquo; {poetName?.Name}";
            }
            else
            {
                if(!string.IsNullOrEmpty(Query))
                {
                    ViewData["Title"] = $"گنجور &raquo; نتایج جستجو برای {Query}";
                }
                else
                {
                    ViewData["Title"] = $"گنجور &raquo; جستجو";
                }
                
            }

            if (PoetId != 0)
            {
                await preparePoet();

            }

            // 2. search verses
            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            HttpResponseMessage searchQueryResponse = null;

            if (!string.IsNullOrEmpty(Query))
            {
                searchQueryResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poems/search?term={Query}&poetId={PoetId}&catId={CatId}&PageNumber={pageNumber}&PageSize=20");

                searchQueryResponse.EnsureSuccessStatusCode();

                Poems = JArray.Parse(await searchQueryResponse.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();
            }
            

            if(Poems != null && Poems.Count == 0)
            {
                Poems = null;
            }

            if(Poems != null)
            {
                // highlight searched word
                string[] queryParts = Query.IndexOf('"') == 0 && Query.LastIndexOf('"') == (Query.Length - 1) ?
                       new string[] { Query.Replace("\"", "") }
                       :
                       Query.Replace("\"", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var poem in Poems)
                {
                   

                    int firstIndex = poem.PlainText.Length;
                    for (int i = 0; i < queryParts.Length; i++)
                    {
                        if (poem.PlainText.IndexOf(queryParts[i]) < firstIndex)
                        {
                            if (firstIndex >= 0)
                            {
                                firstIndex = poem.PlainText.IndexOf(queryParts[i]);
                            }
                        }
                    }



                    if (firstIndex < 0)
                        firstIndex = 0;
                    _preparePoemExcerpt(poem, firstIndex);



                    for (int i = 0; i < queryParts.Length; i++)
                    {
                        string cssClass = i % 3 == 0 ? "hilite" : i % 3 == 1 ? "hilite2" : "hilite3";
                        string finalPlainText = "";
                        foreach(string line in poem.PlainText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                        {
                            finalPlainText += $"<p>{line}</p>";
                        }
                        poem.PlainText = Regex.Replace(finalPlainText, queryParts[i], $"<span class=\"{cssClass}\">{queryParts[i]}</span>", RegexOptions.IgnoreCase | RegexOptions.RightToLeft); ;
                    }


                }

                if(searchQueryResponse != null)
                {
                    string paginationMetadataJsonValue = searchQueryResponse.Headers.GetValues("paging-headers").FirstOrDefault();

                    if (!string.IsNullOrEmpty(paginationMetadataJsonValue))
                    {
                        PaginationMetadata paginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginationMetadataJsonValue);
                        PagingToolsHtml = GeneratePagingBarHtml(paginationMetadata, $"/search?s={Query}&author={PoetId}");
                    }
                }

               
            }

           

            return Page();
        }

        private void _preparePoemExcerpt(GanjoorPoemCompleteViewModel poem, int leastIndex)
        {
            if (poem == null)
            {
                return;
            }
            if (leastIndex > 10)
            {
                leastIndex -= 10;
            }
            poem.PlainText = "..." + poem.PlainText.Substring(leastIndex);

            if (poem.PlainText.Length > 300)
            {
                poem.PlainText = poem.PlainText.Substring(0, 250);
                int n = poem.PlainText.LastIndexOf(' ');
                if (n >= 0)
                {
                    poem.PlainText = poem.PlainText.Substring(0, n) + " ...";
                }
                else
                {
                    poem.PlainText += "...";
                }
            }
        }

        private string GeneratePagingBarHtml(PaginationMetadata paginationMetadata, string routeStartWithQueryStrings)
        {
            string htmlText = "<p style=\"text-align: center;\">";

            if (paginationMetadata != null && paginationMetadata.totalPages > 1)
            {
                if (paginationMetadata.currentPage > 3)
                {
                    htmlText += $"[<a href=\"{routeStartWithQueryStrings}&page=1\">صفحهٔ اول</a>] …";
                }
                for (int i = (paginationMetadata.currentPage - 2); i <= (paginationMetadata.currentPage + 2); i++)
                {
                    if (i >= 1 && i <= paginationMetadata.totalPages)
                    {
                        htmlText += " [";
                        if (i == paginationMetadata.currentPage)
                        {
                            htmlText += i.ToPersianNumbers();
                        }
                        else
                        {
                            htmlText += $"<a href=\"{routeStartWithQueryStrings}&page={i}\">{i.ToPersianNumbers()}</a>";
                        }
                        htmlText += "] ";
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… [<a href=\"{routeStartWithQueryStrings}&page={paginationMetadata.totalPages}\">صفحهٔ آخر</a>]";
                }
            }

            htmlText += $"</p>{Environment.NewLine}";
            return htmlText;
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostLoginAsync()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            LoginViewModel.ClientAppName = "GanjooRazor";
            LoginViewModel.Language = "fa-IR";

            var stringContent = new StringContent(JsonConvert.SerializeObject(LoginViewModel), Encoding.UTF8, "application/json");
            var loginUrl = $"{APIRoot.Url}/api/users/login";
            var response = await _httpClient.PostAsync(loginUrl, stringContent);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return Redirect($"/login?redirect={Request.Path}&error={await response.Content.ReadAsStringAsync()}");
            }

            LoggedOnUserModel loggedOnUser = JsonConvert.DeserializeObject<LoggedOnUserModel>(await response.Content.ReadAsStringAsync());

            var cookieOption = new CookieOptions()
            {
                Expires = DateTime.Now.AddDays(365),
            };

            Response.Cookies.Append("UserId", loggedOnUser.User.Id.ToString(), cookieOption);
            Response.Cookies.Append("SessionId", loggedOnUser.SessionId.ToString(), cookieOption);
            Response.Cookies.Append("Token", loggedOnUser.Token, cookieOption);
            Response.Cookies.Append("Username", loggedOnUser.User.Username, cookieOption);
            Response.Cookies.Append("Name", $"{loggedOnUser.User.FirstName} {loggedOnUser.User.SureName}", cookieOption);
            Response.Cookies.Append("NickName", $"{loggedOnUser.User.NickName}", cookieOption);

            bool canEditContent = false;
            var ganjoorEntity = loggedOnUser.SecurableItem.Where(s => s.ShortName == RMuseumSecurableItem.GanjoorEntityShortName).SingleOrDefault();
            if (ganjoorEntity != null)
            {
                var op = ganjoorEntity.Operations.Where(o => o.ShortName == SecurableItem.ModifyOperationShortName).SingleOrDefault();
                if (op != null)
                {
                    canEditContent = op.Status;
                }
            }

            Response.Cookies.Append("CanEdit", canEditContent.ToString(), cookieOption);


            return Redirect(Request.Path);
        }

        /// <summary>
        /// logout
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (!string.IsNullOrEmpty(Request.Cookies["SessionId"]) && !string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                using (HttpClient secureClient = new HttpClient())
                {
                    if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        var logoutUrl = $"{APIRoot.Url}/api/users/delsession?userId={Request.Cookies["UserId"]}&sessionId={Request.Cookies["SessionId"]}";
                        await secureClient.DeleteAsync(logoutUrl);
                    }
                }
            }


            var cookieOption = new CookieOptions()
            {
                Expires = DateTime.Now.AddDays(-1)
            };
            foreach (var cookieName in new string[] { "UserId", "SessionId", "Token", "Username", "Name", "NickName", "CanEdit" })
            {
                if (Request.Cookies[cookieName] != null)
                {
                    Response.Cookies.Append(cookieName, "", cookieOption);
                }
            }


            return Redirect(Request.Path);
        }
    }

}
