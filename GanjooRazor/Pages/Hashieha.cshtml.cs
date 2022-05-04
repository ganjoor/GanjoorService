using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Auth.ViewModel;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services.Implementation;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;


namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class HashiehaModel : LoginPartialEnabledPageModel
    {
        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration Configuration;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="memoryCache"></param>
        /// <param name="configuration"></param>
        public HashiehaModel(HttpClient httpClient, IMemoryCache memoryCache, IConfiguration configuration) : base(httpClient)
        {
            _memoryCache = memoryCache;
            Configuration
                = configuration;
        }

        /// <summary>
        /// aggressive cache
        /// </summary>
        public bool AggressiveCacheEnabled
        {
            get
            {
                try
                {
                    return bool.Parse(Configuration["AggressiveCacheEnabled"]);
                }
                catch
                {
                    return false;
                }
            }
        }

        public List<GanjoorPoetViewModel> Poets { get; set; }
        public int PoetId { get; set; }
        public GanjoorPoetCompleteViewModel Poet { get; set; }
        public string PagingToolsHtml { get; set; }
        public string LastError { get; set; }

        private async Task<bool> preparePoets()
        {
            var cacheKey = $"/api/ganjoor/poets";
            if (!_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel> poets))
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return false;
                }
                poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poets);
                }
            }

            Poets = poets;
            return true;
        }

        private async Task<bool> preparePoet()
        {
            var cacheKey = $"/api/ganjoor/poet/{PoetId}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                var poetResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{PoetId}");
                if (!poetResponse.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await poetResponse.Content.ReadAsStringAsync());
                    return false;
                }
                poet = JObject.Parse(await poetResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poet);
                }
            }

            Poet = poet;
            return true;
        }

        

        public List<GanjoorCommentFullViewModel> Comments { get; set; }

        public async Task<IActionResult> OnGetPoetInformationAsync(int id)
        {
            if (id == 0)
                return new OkObjectResult(null);
            var cacheKey = $"/api/ganjoor/poet/{id}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                var poetResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{id}");
                if (!poetResponse.IsSuccessStatusCode)
                {
                    return BadRequest(JsonConvert.DeserializeObject<string>(await poetResponse.Content.ReadAsStringAsync()));
                }
                poet = JObject.Parse(await poetResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poet);
                }
            }
            return new OkObjectResult(poet);
        }

        public GanjoorUserPublicProfile Profile { get; set; }

        public string Title { get; set; }

        public string HomeLink { get; set; }

        public bool CanAdministerUsers { get; set; }

        public string Query { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);

            PoetId = string.IsNullOrEmpty(Request.Query["a"]) ? 0 : int.Parse(Request.Query["a"]);

            ViewData["GoogleAnalyticsCode"] = Configuration["GoogleAnalyticsCode"];

            //todo: use html master layout or make it partial
            // 1. poets 
            if (false == (await preparePoets()))
                return Page();

            if (PoetId != 0)
            {
                if (false == (await preparePoet()))
                    return Page();
            }

            // 2. search comments

            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            Query = Request.Query["w"];

            var filterUserId = Request.Query["userid"];
            string url = $"{APIRoot.Url}/api/ganjoor/comments?PageNumber={pageNumber}&PageSize=20";
            Title = "حاشیه‌ها";
            HomeLink = "/hashieha";
            if (!string.IsNullOrEmpty(filterUserId))
            {
                var responseUserProfile = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/user/profile/{filterUserId}");
                if (!responseUserProfile.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await responseUserProfile.Content.ReadAsStringAsync());
                    return Page();
                }

                Profile = JsonConvert.DeserializeObject<GanjoorUserPublicProfile>(await responseUserProfile.Content.ReadAsStringAsync());


                ViewData["Title"] = $"گنجور » حاشیه‌های {Profile.NickName}";

                Title = $"حاشیه‌های {Profile.NickName}";
                HomeLink = $"/hashieha?userid={filterUserId}";

                if (!string.IsNullOrEmpty(Request.Cookies["Token"]))
                {
                    using (HttpClient secureClient = new HttpClient())
                    {
                        secureClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["Token"]);
                        var res = await secureClient.GetAsync($"{APIRoot.Url}/api/users/securableitems");
                        if (res.IsSuccessStatusCode)
                        {
                            SecurableItem[] secuarbleItems = JsonConvert.DeserializeObject<SecurableItem[]>(await res.Content.ReadAsStringAsync());
                            var userSecurableItem = secuarbleItems.Where(s => s.ShortName == SecurableItem.UserEntityShortName).FirstOrDefault();
                            if (userSecurableItem != null)
                            {
                                var administerOperation = userSecurableItem.Operations.Where(o => o.ShortName == SecurableItem.Administer).FirstOrDefault();
                                if (administerOperation != null)
                                {
                                    CanAdministerUsers = administerOperation.Status;
                                }
                            }
                        }
                    }
                }

                url += $"&filterUserId={filterUserId}";

            }

            if(!string.IsNullOrEmpty(Query))
            {
                Query = Query.ApplyCorrectYeKe().Trim();
                bool quotes = Query.IndexOf("\"") != -1;
                Query = LanguageUtils.MakeTextSearchable(Query); //replace zwnj with space
                if (quotes)
                    Query = $"\"{Query}\"";
                url += $"&term={Query}";
            }


            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }


            Comments = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCommentFullViewModel>>();

            string paginnationMetadata = response.Headers.GetValues("paging-headers").FirstOrDefault();
            PaginationMetadata paginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata);

            string htmlText = "";
            
            if (paginationMetadata.totalPages > 1)
            {
                if(pageNumber > 1)
                    ViewData["Title"] += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";


                htmlText = $"<div>{Environment.NewLine}";
                string queryFilterUserId = string.IsNullOrEmpty(filterUserId) ? "" : $"&amp;userid={filterUserId}";
                if (paginationMetadata.currentPage > 3)
                {
                    htmlText += $"<a href=\"/hashieha/?page=1{queryFilterUserId}\"><div class=\"circled-number\">۱</div></a> …";
                }
                for (int i = paginationMetadata.currentPage - 2; i <= (paginationMetadata.currentPage + 2); i++)
                {
                    if (i >= 1 && i <= paginationMetadata.totalPages)
                    {
                        if (i == paginationMetadata.currentPage)
                        {
                            htmlText += $"<div class=\"circled-number-diff\">{i.ToPersianNumbers()}</div>{Environment.NewLine}";
                        }
                        else
                        {
                            htmlText += $"<a href=\"/hashieha/?page={i}{queryFilterUserId}\"><div class=\"circled-number\">{i.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                        }
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… <a href=\"/hashieha/?page={paginationMetadata.totalPages}{queryFilterUserId}\"><div class=\"circled-number\">{paginationMetadata.totalPages.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                }
                htmlText += $"</div>{Environment.NewLine}";
            }

            ViewData["Title"] = $"گنجور » {Title}";
            PagingToolsHtml = htmlText;

            return Page();
        }
    }
}
