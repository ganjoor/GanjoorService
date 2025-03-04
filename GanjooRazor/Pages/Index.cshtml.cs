﻿using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.PDFLibrary;
using RMuseum.Services.Implementation;
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
    public partial class IndexModel : LoginPartialEnabledPageModel
    {
        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration Configuration;

        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        // <summary>
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

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="httpClient"></param>
        /// <param name="memoryCache"></param>
        public IndexModel(IConfiguration configuration,
            HttpClient httpClient,
            IMemoryCache memoryCache
            ) : base(httpClient)
        {
            Configuration = configuration;
            _memoryCache = memoryCache;
        }



        /// <summary>
        /// last error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// banner
        /// </summary>
        public GanjoorSiteBannerViewModel Banner { get; set; }



        public _CommentPartialModel GetCommentModel(GanjoorCommentSummaryViewModel comment, int poemId)
        {
            return new _CommentPartialModel()
            {
                Comment = comment,
                Error = "",
                InReplyTo = null,
                LoggedIn = LoggedIn,
                DivSuffix = "",
                PoemId = poemId,
            };
        }

        public _AudioPlayerPartialModel GetRecitationsModel(PublicRecitationViewModel[] recitations, bool minimumControls, RecitationType recitationType, bool isAdmin)
        {
            return new _AudioPlayerPartialModel()
            {
                LoggedIn = LoggedIn,
                Recitations = recitations.Where(a => a.RecitationType == recitationType).ToArray(),
                ShowAllRecitaions = minimumControls ? true : ShowAllRecitaions,
                CategoryMode = minimumControls,
                RecitationType = recitationType,
                IsAdmin = isAdmin,
            };
        }

        public _QuotedPoemPartialModel GetQuotedPoemModel(GanjoorQuotedPoemViewModel quotedPoem, GanjoorPageCompleteViewModel page, bool canEdit)
        {
            return new _QuotedPoemPartialModel()
            {
                GanjoorQuotedPoemViewModel = quotedPoem,
                PoetImageUrl = page.PoetOrCat.Poet.ImageUrl,
                PoetNickName = page.PoetOrCat.Poet.Nickname,
                CanEdit = canEdit,
            };
        }

        public async Task<ActionResult> OnPostReply(string replyCommentText, int refPoemId, int refCommentId)
        {
            return await OnPostComment(replyCommentText, refPoemId, refCommentId, -1);
        }



        /// <summary>
        /// comment
        /// </summary>
        /// <param name="comment"></param>
        /// <param name="poemId"></param>
        /// <param name="inReplytoId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        public async Task<ActionResult> OnPostComment(string comment, int poemId, int inReplytoId, int coupletIndex)
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
                                PoemId = poemId,
                                CoupletIndex = coupletIndex == -1 ? null : coupletIndex
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
                                    InReplyTo = inReplytoId == 0 ? null : new GanjoorCommentSummaryViewModel(),
                                    LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]),
                                    PoemId = poemId,
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
                                    Error = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()),
                                    InReplyTo = null,
                                    LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]),
                                    PoemId = poemId,
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
                                Error = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.",
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
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/comment?id={id}");

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
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
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
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
        /// canonical url
        /// </summary>
        public string CanonicalUrl
        {
            get
            {
                return $"{Configuration["SiteUrl"]}{GanjoorPage.FullUrl}";
            }
        }

        /// <summary>
        /// can edit
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// keep history
        /// </summary>
        public bool KeepHistory { get; set; }



        /// <summary>
        /// pinterest url
        /// </summary>
        public string PinterestUrl { get; set; }

        /// <summary>
        /// show all recitations
        /// </summary>
        public bool ShowAllRecitaions { get; set; }

        /// <summary>
        /// active tab
        /// </summary>
        public string ActiveTab { get; set; }

        /// <summary>
        /// prepare poem except
        /// </summary>
        /// <param name="poem"></param>
        private void _preparePoemExcerpt(GanjoorPoemSummaryViewModel poem)
        {
            if (poem == null || poem.Excerpt == null)
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
        private async Task _markMyCommentsAndBringUpMyRecitations()
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
                _markMyReplies(comment, userId, null);
            }

            if (GanjoorPage.Poem.Recitations.Length > 0)
            {
                using (HttpClient secureClient = new HttpClient())
                {
                    if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{GanjoorPage.Poem.Id}/recitations/upvotes");
                        if (response.IsSuccessStatusCode)
                        {
                            var upVotedIds = JsonConvert.DeserializeObject<int[]>(await response.Content.ReadAsStringAsync());
                            if (upVotedIds.Length > 0)
                            {
                                foreach (var recitation in GanjoorPage.Poem.Recitations)
                                {
                                    recitation.UpVotedByUser = upVotedIds.Contains(recitation.Id);
                                }
                                Array.Sort(GanjoorPage.Poem.Recitations, (a, b) => b.UpVotedByUser == a.UpVotedByUser ? a.AudioOrder.CompareTo(b.AudioOrder) : b.UpVotedByUser.CompareTo(a.UpVotedByUser));
                            }
                        }

                    }
                }
            }
        }

        private void _markMyReplies(GanjoorCommentSummaryViewModel parent, Guid userId, GanjoorUserBookmarkViewModel[] bookmarks)
        {
            foreach (var reply in parent.Replies)
            {
                reply.MyComment = reply.UserId == userId;
                if (bookmarks != null)
                    reply.IsBookmarked = bookmarks.Where(b => b.CoupletIndex == -reply.Id).Any();
                _markMyReplies(reply, userId, bookmarks);
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

        private async Task<bool> preparePoets()
        {
            var cacheKey = $"/api/ganjoor/poets";
            if (!_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel> poets))
            {
                try
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
                catch
                {
                    LastError = "خطا در دسترسی به وب سرویس گنجور";
                    return false;
                }

            }

            Poets = poets;
            return true;
        }

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

        private async Task<bool> _PreparePoetGroups()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/centuries");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return false;
                }
                PoetGroups = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCenturyViewModel>>();
                return true;
            }
            catch
            {
                LastError = "خطا در دسترسی به وب سرویس گنجور";
                return false;
            }

        }

        /// <summary>
        /// valid only for home page
        /// </summary>
        public List<GanjoorCenturyViewModel> PoetGroups { get; set; }

        /// <summary>
        /// html language
        /// </summary>
        public string HtmlLanguage { get; set; } = "fa-IR";
        private void _prepareNextPre()
        {
            switch (GanjoorPage.GanjoorPageType)
            {
                case GanjoorPageType.PoemPage:
                    {
                        if (GanjoorPage.Poem.Next != null)
                        {
                            NextUrl = GanjoorPage.PoetOrCat.Cat.FullUrl + "/" + GanjoorPage.Poem.Next.UrlSlug;
                            NextTitle = GanjoorPage.Poem.Next.Title + ": " + GanjoorPage.Poem.Next.Excerpt;
                        }
                        else
                        if (GanjoorPage.Poem.MixedModeOrder > 0
                            && GanjoorPage.Poem.Category.Cat.Children.Where(c => c.MixedModeOrder == 0 || c.MixedModeOrder > GanjoorPage.Poem.MixedModeOrder).Any())
                        {
                            var nextCat = GanjoorPage.Poem.Category.Cat.Children.Where(c => c.MixedModeOrder == 0 || c.MixedModeOrder > GanjoorPage.Poem.MixedModeOrder).OrderBy(c => c.MixedModeOrder).First();
                            NextUrl = nextCat.FullUrl;
                            NextTitle = nextCat.Title;
                        }
                        else
                        if (GanjoorPage.Poem.Category.Cat.Next != null)
                        {
                            NextUrl = GanjoorPage.Poem.Category.Cat.Next.FullUrl;
                            NextTitle = GanjoorPage.Poem.Category.Cat.Next.Title;
                        }

                        if (GanjoorPage.Poem.Previous != null)
                        {
                            PreviousUrl = GanjoorPage.PoetOrCat.Cat.FullUrl + "/" + GanjoorPage.Poem.Previous.UrlSlug;
                            PreviousTitle = GanjoorPage.Poem.Previous.Title + ": " + GanjoorPage.Poem.Previous.Excerpt;
                        }
                        else
                        if (GanjoorPage.Poem.MixedModeOrder > 0
                            && GanjoorPage.Poem.Category.Cat.Children.Where(c => c.MixedModeOrder != 0 && c.MixedModeOrder < GanjoorPage.Poem.MixedModeOrder).Any())
                        {
                            var prevCat = GanjoorPage.Poem.Category.Cat.Children.Where(c => c.MixedModeOrder != 0 && c.MixedModeOrder < GanjoorPage.Poem.MixedModeOrder).OrderByDescending(c => c.MixedModeOrder).First();
                            PreviousUrl = prevCat.FullUrl;
                            PreviousTitle = prevCat.Title;
                        }
                        else
                        if (GanjoorPage.Poem.Category.Cat.Previous != null)
                        {
                            PreviousUrl = GanjoorPage.Poem.Category.Cat.Previous.FullUrl;
                            PreviousTitle = GanjoorPage.Poem.Category.Cat.Previous.Title;
                        }
                    }
                    break;
                case GanjoorPageType.CatPage:
                    {
                        if (GanjoorPage.PoetOrCat.Cat.Next != null)
                        {
                            NextUrl = GanjoorPage.PoetOrCat.Cat.Next.FullUrl;
                            NextTitle = GanjoorPage.PoetOrCat.Cat.Next.Title;
                        }
                        if (GanjoorPage.PoetOrCat.Cat.Previous != null)
                        {
                            PreviousUrl = GanjoorPage.PoetOrCat.Cat.Previous.FullUrl;
                            PreviousTitle = GanjoorPage.PoetOrCat.Cat.Previous.Title;
                        }
                    }
                    break;
            }
        }

        public bool MutiPartPoemPage { get; set; }

        private void _prepareRelatedSecions()
        {
            SectionsWithRelated = new List<GanjoorPoemSection>();
            SectionsWithMetreAndRhymes = new List<GanjoorPoemSection>();
            MutiPartPoemPage = GanjoorPage.Poem.Verses.Where(v => v.VersePosition == VersePosition.Paragraph || v.VersePosition == VersePosition.Single).Any()
                ||
                GanjoorPage.Poem.Sections.Count(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First) != 1;
            if (
                !MutiPartPoemPage
                )
            {
                foreach (var section in GanjoorPage.Poem.Sections)
                {
                    if (section.Top6RelatedSections.Length > 0)
                    {
                        SectionsWithRelated.Add(section);
                    }
                    if (section.SectionType == PoemSectionType.WholePoem && section.GanjoorMetre != null && !string.IsNullOrEmpty(section.RhymeLetters))
                    {
                        SectionsWithMetreAndRhymes.Add(section);
                    }
                }
            }
        }

        public GanjoorPoetSuggestedPictureViewModel Photo { get; set; }

        /// <summary>
        /// Get
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }
            if (Request.Path.ToString().IndexOf("index.php") != -1)
            {
                return Redirect($"{Request.Path.ToString().Replace("index.php", "search")}{Request.QueryString}");
            }

            if (Request.Path.ToString().IndexOf("vazn") != -1 && Request.QueryString.ToString().IndexOf("v") != -1)
            {
                return Redirect($"{Request.Path.ToString().Replace("vazn", "simi")}{Request.QueryString}");
            }

            LastError = "";
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);
            CanEdit = Request.Cookies["CanEdit"] == "True";
            KeepHistory = Request.Cookies["KeepHistory"] == "True";
            CanTranslate = Request.Cookies["CanTranslate"] == "True";
            IsPoetPage = false;
            IsCatPage = false;
            IsPoemPage = false;
            IsHomePage = Request.Path == "/";
            PinterestUrl = Request.Query["pinterest_url"];
            ShowAllRecitaions = Request.Query["allaudio"] == "1";
            ViewData["GoogleAnalyticsCode"] = Configuration["GoogleAnalyticsCode"];
            ActiveTab = Request.Query["tab"];
            if (ShowAllRecitaions && string.IsNullOrEmpty(ActiveTab))
            {
                ActiveTab = "recitations";
            }
            else
                if (ActiveTab == "recitations" || ActiveTab == "commentaries")
            {
                ShowAllRecitaions = true;
            }

            GoogleBreadCrumbList breadCrumbList = new GoogleBreadCrumbList();
            Banner = null;

            if (!string.IsNullOrEmpty(Request.Query["p"]))
            {
                var pageUrlResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={Request.Query["p"]}");
                if (!pageUrlResponse.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                    return Page();
                }
                var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                return Redirect(pageUrl);
            }

            await preparePoets();

            if (!IsHomePage)
            {
                var pageQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={Request.Path}");
                if (!pageQuery.IsSuccessStatusCode)
                {
                    if (pageQuery.StatusCode == HttpStatusCode.NotFound)
                    {
                        var redirectQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/redirecturl?url={Request.Path}");
                        if (redirectQuery.IsSuccessStatusCode)
                        {
                            var redirectUrl = JsonConvert.DeserializeObject<string>(await redirectQuery.Content.ReadAsStringAsync());
                            return Redirect(redirectUrl);
                        }
                        return NotFound();
                    }
                }
                if (!pageQuery.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await pageQuery.Content.ReadAsStringAsync());
                    return Page();
                }
                GanjoorPage = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();
                if (!string.IsNullOrEmpty(GanjoorPage.HtmlText))
                {
                    GanjoorPage.HtmlText = GanjoorPage.HtmlText.Replace("https://ganjoor.net/", "/").Replace("http://ganjoor.net/", "/");
                }
                switch (GanjoorPage.GanjoorPageType)
                {
                    case GanjoorPageType.PoemPage:
                        await _markMyCommentsAndBringUpMyRecitations();
                        _preparePoemExcerpt(GanjoorPage.Poem.Next);
                        _preparePoemExcerpt(GanjoorPage.Poem.Previous);
                        GanjoorPage.PoetOrCat = GanjoorPage.Poem.Category;
                        _prepareNextPre();
                        _prepareRelatedSecions();
                        IsPoemPage = true;

                        break;
                    case GanjoorPageType.PoetPage:
                        IsPoetPage = true;
                        break;
                    case GanjoorPageType.CatPage:
                        _prepareNextPre();

                        IsCatPage = true;
                        break;
                }

                if (IsPoetPage || IsCatPage)
                {
                    var catHasAnyRecitationQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/audio/catany/{GanjoorPage.PoetOrCat.Cat.Id}");

                    if (!catHasAnyRecitationQuery.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await catHasAnyRecitationQuery.Content.ReadAsStringAsync());
                        return Page();
                    }
                    CategoryHasRecitations = JsonConvert.DeserializeObject<bool>(await catHasAnyRecitationQuery.Content.ReadAsStringAsync());
                }

                if (IsPoetPage)
                {
                    var responsePhotos = await _httpClient.GetAsync($"{APIRoot.Url}/api/poetphotos/poet/{GanjoorPage.PoetOrCat.Poet.Id}");
                    if (!responsePhotos.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await responsePhotos.Content.ReadAsStringAsync());
                        return Page();
                    }
                    var photos = JArray.Parse(await responsePhotos.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetSuggestedPictureViewModel>>();
                    if (photos.Any())
                    {
                        Photo = photos.First();
                    }
                }

                if (IsPoemPage)
                {
                    HtmlLanguage = string.IsNullOrEmpty(GanjoorPage.Poem.Language) ? "fa-IR" : GanjoorPage.Poem.Language;
                    if (bool.Parse(Configuration["BannersEnabled"]))
                    {
                        var bannerQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/banners/random");
                        if (!bannerQuery.IsSuccessStatusCode)
                        {
                            LastError = JsonConvert.DeserializeObject<string>(await bannerQuery.Content.ReadAsStringAsync());
                            return Page();
                        }
                        string bannerResponse = await bannerQuery.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(bannerResponse))
                        {
                            Banner = JObject.Parse(bannerResponse).ToObject<GanjoorSiteBannerViewModel>();
                        }
                    }
                }
            }

            if (IsHomePage)
            {
                ViewData["Title"] = "گنجور";
                await _PreparePoetGroups();
            }
            else
            if (IsPoetPage)
            {
                ViewData["Title"] = $"گنجور » {GanjoorPage.PoetOrCat.Poet.Nickname}";
                breadCrumbList.AddItem(GanjoorPage.PoetOrCat.Poet.Nickname, GanjoorPage.PoetOrCat.Cat.FullUrl, $"{APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}");
            }
            else
            if (IsCatPage)
            {
                string title = $"گنجور » ";
                bool poetCat = true;
                foreach (var gran in GanjoorPage.PoetOrCat.Cat.Ancestors)
                {
                    title += $"{gran.Title} » ";
                    breadCrumbList.AddItem(gran.Title, gran.FullUrl, poetCat ? $"{APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}" : "https://i.ganjoor.net/cat.png");
                    poetCat = false;
                }
                breadCrumbList.AddItem(GanjoorPage.PoetOrCat.Cat.Title, GanjoorPage.PoetOrCat.Cat.FullUrl, "https://i.ganjoor.net/cat.png");
                title += GanjoorPage.PoetOrCat.Cat.Title;
                ViewData["Title"] = title;
            }
            else
            if (IsPoemPage)
            {
                ViewData["Title"] = $"گنجور » {GanjoorPage.Poem.FullTitle}";
                bool poetCat = true;
                foreach (var gran in GanjoorPage.Poem.Category.Cat.Ancestors)
                {
                    breadCrumbList.AddItem(gran.Title, gran.FullUrl, poetCat ? $"{APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}" : "https://i.ganjoor.net/cat.png");
                    poetCat = false;
                }
                breadCrumbList.AddItem(GanjoorPage.PoetOrCat.Cat.Title, GanjoorPage.PoetOrCat.Cat.FullUrl, "https://i.ganjoor.net/cat.png");
                if (GanjoorPage.Poem.Images.Where(i => i.TargetPageUrl.StartsWith("https://museum.ganjoor.net/items/ai")).Any())
                {
                    breadCrumbList.AddItem(GanjoorPage.Poem.Title, GanjoorPage.Poem.FullUrl,
                        GanjoorPage.Poem.Images.Where(i => i.TargetPageUrl.StartsWith("https://museum.ganjoor.net/items/ai")).First().ThumbnailImageUrl.Replace("/thumb/", "/orig/"));
                }
                else
                if (GanjoorPage.Poem.Images.Any())
                {
                    breadCrumbList.AddItem(GanjoorPage.Poem.Title, GanjoorPage.Poem.FullUrl,
                        GanjoorPage.Poem.Images.First().ThumbnailImageUrl.Replace("/thumb/", "/orig/"));
                }
                else
                {
                    breadCrumbList.AddItem(GanjoorPage.Poem.Title, GanjoorPage.Poem.FullUrl, "https://i.ganjoor.net/poem.png");
                }

            }
            else
            {
                if (GanjoorPage.PoetOrCat != null)
                {
                    bool poetCat = true;
                    string fullTitle = "گنجور » ";
                    if (GanjoorPage.PoetOrCat.Cat.Ancestors.Count == 0)
                    {
                        fullTitle += $"{GanjoorPage.PoetOrCat.Poet.Nickname} » ";
                    }
                    else
                        foreach (var gran in GanjoorPage.PoetOrCat.Cat.Ancestors)
                        {
                            breadCrumbList.AddItem(gran.Title, gran.FullUrl, poetCat ? $"{APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}" : "https://i.ganjoor.net/cat.png");
                            poetCat = false;
                            fullTitle += $"{gran.Title} » ";
                        }
                    ViewData["Title"] = $"{fullTitle}{GanjoorPage.Title}";
                    breadCrumbList.AddItem(GanjoorPage.PoetOrCat.Poet.Nickname, GanjoorPage.PoetOrCat.Cat.FullUrl, $"{APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}");
                }
                else
                {
                    ViewData["Title"] = $"گنجور » {GanjoorPage.FullTitle}";
                }
                breadCrumbList.AddItem(GanjoorPage.Title, GanjoorPage.FullUrl, "https://i.ganjoor.net/cat.png");
            }


            ViewData["BrearCrumpList"] = breadCrumbList.ToString();

            if (IsCatPage || IsPoetPage)
            {
                var tagsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat/{GanjoorPage.PoetOrCat.Cat.Id}/geotag");
                if (!tagsResponse.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await tagsResponse.Content.ReadAsStringAsync());
                    return Page();
                }

                CategoryPoemGeoDateTags = JsonConvert.DeserializeObject<PoemGeoDateTag[]>(await tagsResponse.Content.ReadAsStringAsync());
            }


            return Page();
        }
        public async Task<ActionResult> OnGetBNumPartialAsync(int poemId, int coupletIndex)
        {
            var responseCoupletComments = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}/comments?coupletIndex={coupletIndex}");
            if (!responseCoupletComments.IsSuccessStatusCode)
            {
                return BadRequest(JsonConvert.DeserializeObject<string>(await responseCoupletComments.Content.ReadAsStringAsync()));
            }
            var comments = JArray.Parse(await responseCoupletComments.Content.ReadAsStringAsync()).ToObject<List<GanjoorCommentSummaryViewModel>>();

            var responseCoupletNumbers = await _httpClient.GetAsync($"{APIRoot.Url}/api/numberings/couplet/{poemId}/{coupletIndex}");
            if (!responseCoupletNumbers.IsSuccessStatusCode)
            {
                return BadRequest(JsonConvert.DeserializeObject<string>(await responseCoupletNumbers.Content.ReadAsStringAsync()));
            }
            var numbers = JArray.Parse(await responseCoupletNumbers.Content.ReadAsStringAsync()).ToObject<List<GanjoorCoupletNumberViewModel>>();

            var responseCoupletSections = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/couplet/{poemId}/{coupletIndex}/sections");
            if (!responseCoupletSections.IsSuccessStatusCode)
            {
                return BadRequest(JsonConvert.DeserializeObject<string>(await responseCoupletSections.Content.ReadAsStringAsync()));
            }
            var responseVerses = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}/verses?coupletIndex={coupletIndex}");
            if (!responseVerses.IsSuccessStatusCode)
            {
                return BadRequest(JsonConvert.DeserializeObject<string>(await responseVerses.Content.ReadAsStringAsync()));
            }
            var verses = JArray.Parse(await responseVerses.Content.ReadAsStringAsync()).ToObject<List<GanjoorVerseViewModel>>();
            var sections = JArray.Parse(await responseCoupletSections.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemSection>>();
            var sectionsWithMetreAndRhymes = new List<GanjoorPoemSection>();
            foreach (var section in sections)
            {
                if (section.GanjoorMetreId != null && !string.IsNullOrEmpty(section.RhymeLetters))
                {
                    if (!sectionsWithMetreAndRhymes.Any(s => s.GanjoorMetreId == section.GanjoorMetreId && s.RhymeLetters == section.RhymeLetters))
                    {
                        sectionsWithMetreAndRhymes.Add(section);
                    }
                }
            }

            bool isBookmarked = false;

            if (!string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                if (Guid.TryParse(Request.Cookies["UserId"], out Guid userId))
                    if (userId != Guid.Empty)
                    {
                        using (HttpClient secureClient = new HttpClient())
                        {
                            if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                            {
                                HttpResponseMessage responseIsBookmarked = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/bookmark/{poemId}");
                                if (!responseIsBookmarked.IsSuccessStatusCode)
                                {
                                    return BadRequest(JsonConvert.DeserializeObject<string>(await responseIsBookmarked.Content.ReadAsStringAsync()));
                                }

                                var bookmarks = JsonConvert.DeserializeObject<GanjoorUserBookmarkViewModel[]>(await responseIsBookmarked.Content.ReadAsStringAsync());
                                if (bookmarks.Where(b => b.CoupletIndex == coupletIndex).Any())
                                    isBookmarked = true;
                                foreach (GanjoorCommentSummaryViewModel comment in comments)
                                {
                                    comment.MyComment = comment.UserId == userId;
                                    comment.IsBookmarked = bookmarks.Where(b => b.CoupletIndex == -comment.Id).Any();
                                    _markMyReplies(comment, userId, bookmarks);
                                }
                            }
                        }
                    }
            }

            return new PartialViewResult()
            {
                ViewName = "_BNumPartial",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _BNumPartialModel()
                    {
                        PoemId = poemId,
                        CoupletIndex = coupletIndex,
                        LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]),
                        Comments = comments,
                        IsBookmarked = isBookmarked,
                        Numbers = numbers,
                        Sections = sections,
                        SectionsWithMetreAndRhymes = sectionsWithMetreAndRhymes,
                        Verses = verses,
                    }
                }
            };
        }

        public async Task<IActionResult> OnPostSwitchBookmarkAsync(int poemId, int coupletIndex)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync(
                        $"{APIRoot.Url}/api/ganjoor/bookmark/switch/ret/{poemId}/{coupletIndex}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return new OkObjectResult(res);
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }

        public async Task<IActionResult> OnGetIsCoupletBookmarkedAsync(int poemId, int coupletIndex)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/bookmark/{poemId}/{coupletIndex}");
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    var res = JsonConvert.DeserializeObject<bool>(await response.Content.ReadAsStringAsync());
                    return new OkObjectResult(res);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnGetPoemBookmarksAsync(int poemId)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/bookmark/{poemId}");
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    var res = JsonConvert.DeserializeObject<GanjoorUserBookmarkViewModel[]>(await response.Content.ReadAsStringAsync());
                    return new OkObjectResult(res);
                }
            }
            return new OkObjectResult(false);
        }

        /// <summary>
        /// unused now, to be removed
        /// </summary>
        /// <param name="poemId"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetUserUpvotedRecitationsAsync(int poemId)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}/recitations/upvotes");
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    var res = JsonConvert.DeserializeObject<int[]>(await response.Content.ReadAsStringAsync());
                    return new OkObjectResult(res);
                }
            }
            return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
        }

        public async Task<ActionResult> OnPostSwitchRecitationUpVoteAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync(
                        $"{APIRoot.Url}/api/audio/vote/switch/{id}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    var res = JsonConvert.DeserializeObject<bool>(await response.Content.ReadAsStringAsync());
                    return new OkObjectResult(res);
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }

        public async Task<ActionResult> OnPostAddToMyHistoryAsync(int poemId)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync(
                        $"{APIRoot.Url}/api/tracking", new StringContent(JsonConvert.SerializeObject(poemId), Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    var res = JsonConvert.DeserializeObject<bool>(await response.Content.ReadAsStringAsync());
                    if (res == false)
                    {
                        if (Request.Cookies["KeepHistory"] != null)
                        {
                            Response.Cookies.Delete("KeepHistory");
                        }
                        var cookieOption = new CookieOptions()
                        {
                            Expires = DateTime.Now.AddDays(365),
                        };
                        Response.Cookies.Append("KeepHistory", $"{false}", cookieOption);
                    }
                    return new OkObjectResult(res);
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }

        public async Task<IActionResult> OnPutBookmarkNote(Guid id, string note)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/bookmark/{id}", new StringContent(JsonConvert.SerializeObject(note), Encoding.UTF8, "application/json"));
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
            return new JsonResult(true);
        }

        public async Task<IActionResult> OnDeleteMistakeAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/audio/errors/approved/{id}");

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
            return new JsonResult(true);
        }

        public async Task<IActionResult> OnPutMistakeAsync(int id, string reasonText)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var stringContent = new StringContent(
                       JsonConvert.SerializeObject
                       (
                           new RecitationErrorReportViewModel()
                           {
                               Id = id,
                               ReasonText = reasonText,
                               NumberOfLinesAffected = 1,
                               CoupletIndex = -1,
                           }
                       ),
                       Encoding.UTF8, "application/json");

                    var response = await secureClient.PutAsync($"{APIRoot.Url}/api/audio/errors/report/edit", stringContent);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
            return new JsonResult(true);
        }

        public async Task<ActionResult> OnGetMoreQuotedPoemsForRelatedPoemPartialAsync(int poemId, int relatedPoemId, string poetImageUrl, string poetNickName, bool canEdit)
        {
            string url = $"{APIRoot.Url}/api/ganjoor/poem/{poemId}/quoteds/{relatedPoemId}?published=true";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));

            var quoteds = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorQuotedPoemViewModel>>();
            if (!quoteds.Any())
            {
                return new BadRequestObjectResult("مورد دیگری یافت نشد.");
            }

            if (quoteds.Any(q => q.ChosenForMainList))
            {
                var mainList = quoteds.Where(q => q.ChosenForMainList).First();
                quoteds.Remove(mainList);
            }


            return new PartialViewResult()
            {
                ViewName = "_MultipleQuotedPoemsPartial",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _MultipleQuotedPoemsPartialModel()
                    {
                        GanjoorQuotedPoems = quoteds.ToArray(),
                        PoetImageUrl = poetImageUrl,
                        PoetNickName = poetNickName,
                        CanEdit = canEdit
                    }
                }
            };
        }

        public async Task<ActionResult> OnGetMoreQuotedPoemsPartialAsync(int poemId, int skip, string poetImageUrl, string poetNickName, bool canEdit)
        {
            string url = $"{APIRoot.Url}/api/ganjoor/poem/{poemId}/quoteds?skip={skip}&itemsCount=1000";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));

            var quoteds = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorQuotedPoemViewModel>>();

            return new PartialViewResult()
            {
                ViewName = "_MultipleQuotedPoemsPartial",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _MultipleQuotedPoemsPartialModel()
                    {
                        GanjoorQuotedPoems = quoteds.ToArray(),
                        PoetImageUrl = poetImageUrl,
                        PoetNickName = poetNickName,
                        CanEdit = canEdit
                    }
                }
            };
        }

        public string PoemBlockClass
        {
            get
            {
                return GanjoorPage != null && GanjoorPage.Poem != null && GanjoorPage.Poem.ClaimedByMultiplePoets ? "poem ribbon-parent" : "poem";
            }
        }

        public async Task<ActionResult> OnPostMarkAsTextOriginalAsync(int bookId, int categoryId)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync(
                        $"{APIRoot.Url}/api/ganjoor/naskban/textoriginal/{bookId}/{categoryId}/true", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }

        public async Task<ActionResult> OnDeleteRelatedImageLinkAsync(PoemRelatedImageType relatedImageType, Guid linkId, bool removeItemLink)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.DeleteAsync(
                        relatedImageType == PoemRelatedImageType.MuseumLink ?
                        $"{APIRoot.Url}/api/artifacts/ganjoor?linkId={linkId}&removeItemLink={removeItemLink}"
                        :
                        $"{APIRoot.Url}/api/artifacts/pinterest?linkId={linkId}"
                        );
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }

        public async Task<ActionResult> OnGetCategoryRecitationsAsync(int catId)
        {
            var catTop1RecitationsQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/audio/cattop1/{catId}?includePoemText=false");

            if (!catTop1RecitationsQuery.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await catTop1RecitationsQuery.Content.ReadAsStringAsync()));
            }
            var categoryTop1Recitations = JsonConvert.DeserializeObject<PublicRecitationViewModel[]>(await catTop1RecitationsQuery.Content.ReadAsStringAsync());

            return new PartialViewResult()
            {
                ViewName = "_AudioPlayerPartial",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _AudioPlayerPartialModel()
                    {
                        LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]),
                        Recitations = categoryTop1Recitations,
                        ShowAllRecitaions = true,
                        CategoryMode = true,
                    }
                }
            };
        }


        private async Task<_CategoryWordsCountPartialModel> _GetCategoryWordCountsAsync(int catId, int poetId, bool remStopWords = false)
        {
            var wordSumsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/wordsums/{catId}");

            if (!wordSumsResponse.IsSuccessStatusCode)
            {
                return null;
            }
            var wordSums = JsonConvert.DeserializeObject<CategoryWordCountSummary>(await wordSumsResponse.Content.ReadAsStringAsync());
            if (wordSums.TotalWordCount == 0)
            {
                return null;
            }
            int pageSize = remStopWords ? 200 : 100;
            var wordCountsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/wordcounts/{catId}?PageNumber=1&PageSize={pageSize}");
            if (!wordCountsResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var wordCounts = JsonConvert.DeserializeObject<CategoryWordCount[]>(await wordCountsResponse.Content.ReadAsStringAsync());

            if (remStopWords)
            {
                string[] stopWords =
                    [
                    "و",
                    "از",
                    "که",
                    "به",
                    "در",
                    "را",
                    "ز",
                    "است",
                    "می",
                    "این",
                    "چون",
                    "بود",
                    "ای",
                    "تا",
                    "چو",
                    "هر",
                    "با",
                    "چه",
                    "شد",
                    "بی",
                    "خود",
                    "گفت",
                    "نیست",
                    "نه",
                    "گر",
                    "کند",
                    "اگر",
                    "کرد",
                    "باشد",
                    "هم",
                    "روی",
                    "شود",
                    "یک",
                    "دو",
                    "وی",
                    "اندر",
                    "پیش",
                    "آمد",
                    "دارد",
                    "کن",
                    "یا",
                    "همی",
                    "آید",
                    "کرده",
                    "نمی",
                    "کز",
                    "هست",
                    "ام",
                    "کی",
                    "بهر",
                    "فی",
                    "چنین",
                    "پای",
                    "ها",
                    "اند",
                    "ی",
                    "گردد",
                    "داد",
                    "چنان",
                    "کنم",
                    "نبود",
                    "گشت",
                    "دیگر",
                    "باید",
                    "دگر",
                    "چند",
                    "همچو",
                    "شده",
                    "بد",
                    "زان",
                    "پی",
                    "مگر",
                    "آنکه",
                    "رفت",
                    "کنی",
                    "برد",
                    "بدان",
                    "ست",
                    "ازین",
                    "دید",
                    "وز",
                    "گوید",
                    "کجا",
                    "دهد",
                    "گه",
                    "درین",
                    "آخر",
                    "دارم",
                    "خواهد",
                    "نیز",
                    "های",
                    "چرا",
                    "راست",
                    "کان",
                    "رو",
                    "نباشد",
                    "بر",
                    "من",
                    "آن",
                    "تو",
                    "او",
                    "ما",
                    "شما",
                    "مرا",
                    "ار",
                    "داری",
                    "بیا",
                    "همه",
                    "گو",
                    "مکن",
                    "زد",
                    "گفتم",
                    ];
                wordCounts = wordCounts.Where(w => !stopWords.Contains(w.Word)).Take(100).ToArray();
            }

            return new _CategoryWordsCountPartialModel()
            {
                CatId = catId,
                PoetId = poetId,
                WordCounts = wordCounts,
                UniqueWordCount = wordSums.UniqueWordCount,
                TotalWordCount = wordSums.TotalWordCount,
                RemStopWords = remStopWords,
            };
        }


        public async Task<ActionResult> OnGetCategoryWordCountsAsync(int catId, int poetId, bool remStopWords = false)
        {
            var res = await _GetCategoryWordCountsAsync(catId, poetId, remStopWords);
            if (res == null)
            {
                return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>("خطا در دسترسی به شمارش واژگان"));
            }

            return new PartialViewResult()
            {
                ViewName = "_CategoryWordsCountPartial",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = res
                }
            };
        }

        public async Task<ActionResult> OnGetSearchCategoryWordCountsAsync(int catId, int poetId, string term, int totalWordCount)
        {
            if (!string.IsNullOrEmpty(term))
            {
                term = term.Trim();
            }
            var wordCountsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/wordcounts/{catId}?PageNumber=1&PageSize=100&term={term}");

            if (!wordCountsResponse.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await wordCountsResponse.Content.ReadAsStringAsync()));
            }
            var wordCounts = JsonConvert.DeserializeObject<CategoryWordCount[]>(await wordCountsResponse.Content.ReadAsStringAsync());

            return new PartialViewResult()
            {
                ViewName = "_CategoryWordsCountTablePartial",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _CategoryWordsCountTablePartialModel()
                    {
                        CatId = catId,
                        PoetId = poetId,
                        WordCounts = wordCounts,
                        TotalWordCount = totalWordCount,
                    }
                }
            };
        }

        public async Task<ActionResult> OnGetPoemWordCountsAsync(int poemId)
        {
            var poemQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}");
            if (!poemQuery.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await poemQuery.Content.ReadAsStringAsync()));
            }
            var poem = JObject.Parse(await poemQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPoemCompleteViewModel>();

            List<CategoryWordCount> counts = new List<CategoryWordCount>();
            foreach (var verse in poem.Verses.Where(v => v.VersePosition != VersePosition.Comment))
            {
                string[] words = LanguageUtils.MakeTextSearchable(verse.Text).Split([' ', '‌']);
                foreach (var word in words)
                {
                    if (string.IsNullOrEmpty(word)) continue;
                    var wordCount = counts.Where(c => c.Word == word).SingleOrDefault();
                    if (wordCount != null)
                    {
                        wordCount.Count++;
                    }
                    else
                    {
                        counts.Add(new CategoryWordCount { CatId = 0, Word = word, Count = 1 });
                    }
                }
            }

            counts.Sort((a, b) => b.Count.CompareTo(a.Count));

            for (int i = 0; i < counts.Count; i++)
            {
                counts[i].RowNmbrInCat = i + 1;
            }

            counts.Insert(0, new CategoryWordCount()
            {
                Word = "* تعداد کل",
                Count = counts.Sum(c => c.Count),
                RowNmbrInCat = 0,
            });


            return new PartialViewResult()
            {
                ViewName = "_CategoryWordsCountTablePartial",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _CategoryWordsCountTablePartialModel()
                    {
                        CatId = -1,
                        PoetId = -1,
                        TotalWordCount = counts.Where(c => c.RowNmbrInCat > 0).Sum(c => c.Count),
                        WordCounts = counts.ToArray()
                    }
                }
            };
        }


    }
}