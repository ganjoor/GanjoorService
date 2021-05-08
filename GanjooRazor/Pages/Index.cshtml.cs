using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public partial class IndexModel : PageModel
    {
        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// IMemoryCache
        /// </summary>
        protected readonly IMemoryCache _memoryCache;

        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="memoryCache"></param>
        public IndexModel(IConfiguration configuration, IMemoryCache memoryCache, HttpClient httpClient)
        {
            _configuration = configuration;
            _memoryCache = memoryCache;
            _httpClient = httpClient;
        }

        [BindProperty]
        public LoginViewModel LoginViewModel { get; set; }

        /// <summary>
        /// last error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// banner
        /// </summary>
        public GanjoorSiteBannerViewModel Banner { get; set; }

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

        public _CommentPartialModel GetCommentModel(GanjoorCommentSummaryViewModel comment)
        {
            return new _CommentPartialModel()
            {
                Comment = comment,
                Error = "",
                InReplyTo = null
            };
        }

        public async Task<ActionResult> OnPostReply(string replyCommentText, int refPoemId, int refCommentId)
        {
            return await OnPostComment(replyCommentText, refPoemId, refCommentId);
        }

        /// <summary>
        /// comment
        /// </summary>
        /// <param name="comment"></param>
        /// <param name="poemId"></param>
        /// <param name="inReplytoId"></param>
        /// <returns></returns>
        public async Task<ActionResult> OnPostComment(string comment, int poemId, int inReplytoId)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {

                    var stringContent = new StringContent(
                        JsonConvert.SerializeObject
                        (
                            new GanjoorCommentPostViewModel()
                            {
                                HtmlComment = comment,
                                InReplyToId = inReplytoId == 0 ? null : inReplytoId,
                                PoemId = poemId
                            }
                        ),
                        Encoding.UTF8, "application/json");
                    var response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/comment", stringContent);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        GanjoorCommentSummaryViewModel resComment = JsonConvert.DeserializeObject<GanjoorCommentSummaryViewModel>(await response.Content.ReadAsStringAsync());
                        resComment.MyComment = true;

                        return new PartialViewResult()
                        {
                            ViewName = "_CommentPartial",
                            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                            {
                                Model = new _CommentPartialModel()
                                {
                                    Comment = resComment,
                                    Error = "",
                                    InReplyTo = inReplytoId == 0 ? null : new GanjoorCommentSummaryViewModel()
                                }
                            }
                        };
                    }
                    else
                    {
                        return new PartialViewResult()
                        {
                            ViewName = "_CommentPartial",
                            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                            {
                                Model = new _CommentPartialModel()
                                {
                                    Comment = null,
                                    Error = await response.Content.ReadAsStringAsync(),
                                    InReplyTo = null
                                }
                            }
                        };
                    }
                }
                else
                {
                    return new PartialViewResult()
                    {
                        ViewName = "_CommentPartial",
                        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                        {
                            Model = new _CommentPartialModel()
                            {
                                Comment = null,
                                Error = "لطفا از گنجور خارج و مجددا به آن وارد شوید.",
                                InReplyTo = null
                            }
                        }
                    };
                }
            }
        }

        /// <summary>
        /// delete my comment
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnDeleteMyComment(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response  = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/comment?id={id}");

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return Redirect($"/login?redirect={Request.Path}&error={await response.Content.ReadAsStringAsync()}");
                    }

                }
            }
            return new JsonResult(true);
        }

        /// <summary>
        /// edit my comment
        /// </summary>
        /// <param name="id"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPutMyComment(int id, string comment)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/comment/{id}", new StringContent(JsonConvert.SerializeObject(comment), Encoding.UTF8, "application/json"));
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return Redirect($"/login?redirect={Request.Path}&error={await response.Content.ReadAsStringAsync()}");
                    }
                }
            }
            return new JsonResult(true);
        }

        /// <summary>
        /// is home page
        /// </summary>
        public bool IsHomePage { get; set; }
        /// <summary>
        /// Poets
        /// </summary>
        public List<GanjoorPoetViewModel> Poets { get; set; }

        /// <summary>
        /// Corresponding Ganojoor Page
        /// </summary>
        public GanjoorPageCompleteViewModel GanjoorPage { get; set; }

        /// <summary>
        /// is poet page
        /// </summary>
        public bool IsPoetPage { get; set; }

        /// <summary>
        /// is category page
        /// </summary>
        public bool IsCatPage { get; set; }

        /// <summary>
        /// is poem page
        /// </summary>
        public bool IsPoemPage { get; set; }

        /// <summary>
        /// is logged on
        /// </summary>
        public bool LoggedIn { get; set; }

        /// <summary>
        /// can edit
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// pinterest url
        /// </summary>
        public string PinterestUrl { get; set; }

        /// <summary>
        /// prepare poem except
        /// </summary>
        /// <param name="poem"></param>
        private void _preparePoemExcerpt(GanjoorPoemSummaryViewModel poem)
        {
            if (poem == null)
            {
                return;
            }
            if (poem.Excerpt.Length > 100)
            {
                poem.Excerpt = poem.Excerpt.Substring(0, 50);
                int n = poem.Excerpt.LastIndexOf(' ');
                if (n >= 0)
                {
                    poem.Excerpt = poem.Excerpt.Substring(0, n) + " ...";
                }
                else
                {
                    poem.Excerpt += "...";
                }
            }
        }

        /// <summary>
        /// specify which comments belong to current user
        /// </summary>
        private void _markMyComments()
        {
            if (GanjoorPage == null)
            {
                return;
            }
            if (GanjoorPage.Poem == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                return;
            }
            if (!Guid.TryParse(Request.Cookies["UserId"], out Guid userId))
            {
                return;
            }
            if (userId == Guid.Empty)
            {
                return;
            }
            foreach (GanjoorCommentSummaryViewModel comment in GanjoorPage.Poem.Comments)
            {
                comment.MyComment = comment.UserId == userId;
                _markMyReplies(comment, userId);
            }
        }

        private void _markMyReplies(GanjoorCommentSummaryViewModel parent, Guid userId)
        {
            foreach(var reply in parent.Replies)
            {
                reply.MyComment = reply.UserId == userId;
                _markMyReplies(reply, userId);
            }
        }

        /// <summary>
        /// get audio description
        /// </summary>
        /// <param name="recitation"></param>
        /// <param name="contributionLink"></param>
        /// <returns></returns>
        public string getAudioDesc(PublicRecitationViewModel recitation, bool contributionLink = false)
        {
            string audiodesc = "به خوانش ";
            if (!string.IsNullOrEmpty(recitation.AudioArtistUrl))
            {
                audiodesc += $"<a href='{recitation.AudioArtistUrl}'>{recitation.AudioArtist}</a>";
            }
            else
            {
                audiodesc += $"{recitation.AudioArtist}";
            }

            if (!string.IsNullOrEmpty(recitation.AudioSrc))
            {
                if (!string.IsNullOrEmpty(recitation.AudioSrcUrl))
                {
                    audiodesc += $" <a href='{recitation.AudioSrcUrl}'>{recitation.AudioSrc}</a>";
                }
                else
                {
                    audiodesc += $" {recitation.AudioSrc}";
                }
            }

            audiodesc += $" <small><a href='/AudioClip/?a={recitation.Id}' onclick='wpopen(this.href); return false' class='comments-link' title='دریافت'>(دریافت)</a></small>";

            if (contributionLink)
            {
                audiodesc += "<br /> <small>می‌خواهید شما بخوانید؟ <a href='http://ava.ganjoor.net/about/'>اینجا</a> را ببینید.</small>";
            }

            return audiodesc;
        }

        private async Task preparePoets(bool includeBio)
        {
            var cacheKey = $"/api/ganjoor/poets?includeBio={includeBio}";
            if(!_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel> poets))
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets?includeBio={includeBio}");
                response.EnsureSuccessStatusCode();
                poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();

                _memoryCache.Set(cacheKey, poets);
            }

            Poets = poets;
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetAsync()
        {
            LastError = "";
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);
            CanEdit = Request.Cookies["CanEdit"] == "True";
            IsPoetPage = false;
            IsCatPage = false;
            IsPoemPage = false;
            IsHomePage = Request.Path == "/";
            PinterestUrl = Request.Query["pinterest_url"];
            ViewData["GoogleAnalyticsCode"] = _configuration["GoogleAnalyticsCode"];
            GoogleBreadCrumbList breadCrumbList = new GoogleBreadCrumbList();
            Banner = null;

            if (!string.IsNullOrEmpty(Request.Query["p"]))
            {
                var pageUrlResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={Request.Query["p"]}");
                pageUrlResponse.EnsureSuccessStatusCode();
                var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                return Redirect(pageUrl);
            }

            await preparePoets(false);

            if (!IsHomePage)
            {
                var pageQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={Request.Path}");
                if (!pageQuery.IsSuccessStatusCode)
                {
                    if (pageQuery.StatusCode == HttpStatusCode.NotFound)
                    {
                        return NotFound();
                    }
                }
                pageQuery.EnsureSuccessStatusCode();
                GanjoorPage = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();
                GanjoorPage.HtmlText = GanjoorPage.HtmlText.Replace("https://ganjoor.net/", "/").Replace("http://ganjoor.net/", "/");
                switch (GanjoorPage.GanjoorPageType)
                {
                    case GanjoorPageType.PoemPage:
                        _markMyComments();
                        _preparePoemExcerpt(GanjoorPage.Poem.Next);
                        _preparePoemExcerpt(GanjoorPage.Poem.Previous);
                        GanjoorPage.PoetOrCat = GanjoorPage.Poem.Category;
                        IsPoemPage = true;
                        break;
                    case GanjoorPageType.PoetPage:
                        IsPoetPage = true;
                        break;
                    case GanjoorPageType.CatPage:
                        IsCatPage = true;
                        break;
                }

                if (IsPoemPage)
                {
                    var bannerQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/site/banner");
                    bannerQuery.EnsureSuccessStatusCode();
                    string bannerResponse = await bannerQuery.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(bannerResponse))
                    {
                        Banner = JObject.Parse(bannerResponse).ToObject<GanjoorSiteBannerViewModel>();
                    }

                }
            }

            if (IsHomePage)
            {
                ViewData["Title"] = "گنجور";
            }
            else
            if (IsPoetPage)
            {
                ViewData["Title"] = $"گنجور &raquo; {GanjoorPage.PoetOrCat.Poet.Name}";
                breadCrumbList.AddItem(GanjoorPage.PoetOrCat.Poet.Name, GanjoorPage.PoetOrCat.Cat.FullUrl, $"{GanjooRazor.APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}");
            }
            else
            if (IsCatPage)
            {
                string title = $"گنجور &raquo; ";
                bool poetCat = true;
                foreach (var gran in GanjoorPage.PoetOrCat.Cat.Ancestors)
                {
                    title += $"{gran.Title} &raquo; ";
                    breadCrumbList.AddItem(gran.Title, gran.FullUrl, poetCat ? $"{GanjooRazor.APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}" : "https://i.ganjoor.net/cat.png");
                    poetCat = false;
                }
                breadCrumbList.AddItem(GanjoorPage.PoetOrCat.Cat.Title, GanjoorPage.PoetOrCat.Cat.FullUrl, "https://i.ganjoor.net/cat.png");
                title += GanjoorPage.PoetOrCat.Cat.Title;
                ViewData["Title"] = title;
            }
            else
            if (IsPoemPage)
            {
                ViewData["Title"] = $"گنجور &raquo; {GanjoorPage.Poem.FullTitle}";
                bool poetCat = true;
                foreach (var gran in GanjoorPage.Poem.Category.Cat.Ancestors)
                {
                    breadCrumbList.AddItem(gran.Title, gran.FullUrl, poetCat ? $"{GanjooRazor.APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}" : "https://i.ganjoor.net/cat.png");
                    poetCat = false;
                }
                breadCrumbList.AddItem(GanjoorPage.PoetOrCat.Cat.Title, GanjoorPage.PoetOrCat.Cat.FullUrl, "https://i.ganjoor.net/cat.png");
                breadCrumbList.AddItem(GanjoorPage.Poem.Title, GanjoorPage.Poem.FullUrl, "https://i.ganjoor.net/poem.png");


            }
            else
            {
                if (GanjoorPage.PoetOrCat != null)
                {
                    bool poetCat = true;
                    string fullTitle = "گنجور &raquo; ";
                    if (GanjoorPage.PoetOrCat.Cat.Ancestors.Count == 0)
                    {
                        fullTitle += $"{GanjoorPage.PoetOrCat.Poet.Name} &raquo; ";
                    }
                    else
                        foreach (var gran in GanjoorPage.PoetOrCat.Cat.Ancestors)
                        {
                            breadCrumbList.AddItem(gran.Title, gran.FullUrl, poetCat ? $"{GanjooRazor.APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}" : "https://i.ganjoor.net/cat.png");
                            poetCat = false;
                            fullTitle += $"{gran.Title} &raquo; ";
                        }
                    ViewData["Title"] = $"{fullTitle}{GanjoorPage.Title}";
                    breadCrumbList.AddItem(GanjoorPage.PoetOrCat.Poet.Name, GanjoorPage.PoetOrCat.Cat.FullUrl, $"{GanjooRazor.APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}");
                }
                else
                {
                    ViewData["Title"] = $"گنجور &raquo; {GanjoorPage.FullTitle}";


                    switch(GanjoorPage.UrlSlug)
                    {
                        case "hashieha":
                            await _GenerateHashiehaHtmlText();
                            break;
                        case "vazn":
                            await _GenerateVaznHtmlText();
                            break;
                        case "simi":
                            await _GenerateSimiHtmlText();
                            break;
                    }
                }
                breadCrumbList.AddItem(GanjoorPage.Title, GanjoorPage.FullUrl, "https://i.ganjoor.net/cat.png");
            }
            

            ViewData["BrearCrumpList"] = breadCrumbList.ToString();

            
            return Page();
        }
    }
}