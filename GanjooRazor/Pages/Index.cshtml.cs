using DNTPersianUtils.Core;
using GanjooRazor.Utils;
using KontorService.Models.Reporting.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
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
        /// Message shown whenever an action requiring a session couldn't prepare an authenticated
        /// client (expired/missing cookies). Was previously duplicated as a literal string in ~10 places.
        /// </summary>
        private const string NotLoggedInMessage = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";

        /// <summary>
        /// Persian stop words excluded from category word-count listings when remStopWords is
        /// requested. Previously rebuilt as a local array literal on every call to
        /// _GetCategoryWordCountsAsync; now a single static HashSet (O(1) Contains instead of O(n),
        /// and allocated once instead of per-request).
        /// </summary>
        private static readonly HashSet<string> _persianStopWords = new HashSet<string>
        {
            "و", "از", "که", "به", "در", "را", "ز", "است", "می", "این", "چون", "بود", "ای", "تا", "چو",
            "هر", "با", "چه", "شد", "بی", "خود", "گفت", "نیست", "نه", "گر", "کند", "اگر", "کرد", "باشد",
            "هم", "روی", "شود", "یک", "دو", "وی", "اندر", "پیش", "آمد", "دارد", "کن", "یا", "همی", "آید",
            "کرده", "نمی", "کز", "هست", "ام", "کی", "بهر", "فی", "چنین", "پای", "ها", "اند", "ی", "گردد",
            "داد", "چنان", "کنم", "نبود", "گشت", "دیگر", "باید", "دگر", "چند", "همچو", "شده", "بد", "زان",
            "پی", "مگر", "آنکه", "رفت", "کنی", "برد", "بدان", "ست", "ازین", "دید", "وز", "گوید", "کجا",
            "دهد", "گه", "درین", "آخر", "دارم", "خواهد", "نیز", "های", "چرا", "راست", "کان", "رو", "نباشد",
            "بر", "من", "آن", "تو", "او", "ما", "شما", "مرا", "ار", "داری", "بیا", "همه", "گو", "مکن", "زد",
            "گفتم",
        };

        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration Configuration;

        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// aggressive cache
        /// </summary>
        public bool AggressiveCacheEnabled => GetConfigFlag("AggressiveCacheEnabled");

        public bool OfflineMode => GetConfigFlag("OfflineMode");

        public bool ReadOnlyMode => GetConfigFlag("ReadOnlyMode");

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

        #region Shared helpers
        // These helpers replace patterns that used to be copy-pasted throughout this file:
        //  - GetConfigFlag: the try/bool.Parse-with-fallback block duplicated for every feature flag
        //  - ReadErrorMessageAsync / CaptureErrorIfFailedAsync: "deserialize the API's error string"
        //    duplicated ~35 times
        //  - WithSecureClientAsync: the using/PrepareClient/else-BadRequest block duplicated ~10 times
        // (PartialViewResult construction is handled by PageModel's own inherited Partial(viewName,
        // model) method - the original code was hand-rolling a PartialViewResult/ViewDataDictionary
        // block 14 times instead of using it.)

        /// <summary>
        /// Reads a boolean feature flag from configuration, defaulting to <paramref name="defaultValue"/>
        /// if the key is missing or not a valid bool, instead of each flag having its own try/catch.
        /// </summary>
        private bool GetConfigFlag(string key, bool defaultValue = false)
        {
            return bool.TryParse(Configuration[key], out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Reads the API's JSON-encoded error string out of a failed response body.
        /// </summary>
        private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
        {
            return JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// If the response failed, stores the API's error message in <see cref="LastError"/> and
        /// returns true so the caller can short-circuit (the established pattern here is
        /// `if (await CaptureErrorIfFailedAsync(response)) return Page();`).
        /// </summary>
        private async Task<bool> CaptureErrorIfFailedAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return false;
            }
            LastError = await ReadErrorMessageAsync(response);
            return true;
        }

        /// <summary>
        /// Runs <paramref name="operation"/> against an HttpClient authenticated from the current
        /// session cookies. If the session can't be prepared (missing/expired cookies), returns
        /// <paramref name="unauthorizedResult"/> (defaulting to a 400 with <see cref="NotLoggedInMessage"/>)
        /// instead of every handler re-implementing the same using/if/else block.
        /// </summary>
        private async Task<IActionResult> WithSecureClientAsync(
            Func<HttpClient, Task<IActionResult>> operation,
            IActionResult unauthorizedResult = null)
        {
            using var secureClient = new HttpClient();
            if (!await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
            {
                return unauthorizedResult ?? new BadRequestObjectResult(NotLoggedInMessage);
            }
            return await operation(secureClient);
        }

        #endregion

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

        public Task<IActionResult> OnPostReply(string replyCommentText, int refPoemId, int refCommentId)
        {
            return OnPostComment(replyCommentText, refPoemId, refCommentId, -1);
        }

        /// <summary>
        /// comment
        /// </summary>
        /// <param name="comment"></param>
        /// <param name="poemId"></param>
        /// <param name="inReplytoId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        public Task<IActionResult> OnPostComment(string comment, int poemId, int inReplytoId, int coupletIndex)
        {
            var unauthorized = Partial("_CommentPartial", new _CommentPartialModel()
            {
                Comment = null,
                Error = NotLoggedInMessage,
                InReplyTo = null
            });

            return WithSecureClientAsync(async secureClient =>
            {
                var stringContent = new StringContent(
                    JsonConvert.SerializeObject(new GanjoorCommentPostViewModel()
                    {
                        HtmlComment = comment,
                        InReplyToId = inReplytoId == 0 ? null : inReplytoId,
                        PoemId = poemId,
                        CoupletIndex = coupletIndex == -1 ? null : coupletIndex
                    }),
                    Encoding.UTF8, "application/json");

                var response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/comment", stringContent);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var resComment = JsonConvert.DeserializeObject<GanjoorCommentSummaryViewModel>(await response.Content.ReadAsStringAsync());
                    resComment.MyComment = true;

                    return Partial("_CommentPartial", new _CommentPartialModel()
                    {
                        Comment = resComment,
                        Error = "",
                        InReplyTo = inReplytoId == 0 ? null : new GanjoorCommentSummaryViewModel(),
                        LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]),
                        PoemId = poemId,
                    });
                }

                return Partial("_CommentPartial", new _CommentPartialModel()
                {
                    Comment = null,
                    Error = await ReadErrorMessageAsync(response),
                    InReplyTo = null,
                    LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]),
                    PoemId = poemId,
                });
            }, unauthorized);
        }

        /// <summary>
        /// delete my comment
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<IActionResult> OnDeleteMyComment(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/comment?id={id}");
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new JsonResult(true);
            });
        }

        /// <summary>
        /// edit my comment
        /// </summary>
        /// <param name="id"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        public Task<IActionResult> OnPutMyComment(int id, string comment)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/comment/{id}", new StringContent(JsonConvert.SerializeObject(comment), Encoding.UTF8, "application/json"));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new JsonResult(true);
            });
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
        public string CanonicalUrl => $"{Configuration["SiteUrl"]}{GanjoorPage.FullUrl}";

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
            const string cacheKey = "/api/ganjoor/poets";
            if (!_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel> poets))
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
                    if (await CaptureErrorIfFailedAsync(response))
                    {
                        return false;
                    }
                    poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();
                    if (AggressiveCacheEnabled)
                    {
                        _memoryCache.Set(cacheKey, poets, TimeSpan.FromHours(1));
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
                    return BadRequest(await ReadErrorMessageAsync(poetResponse));
                }
                poet = JObject.Parse(await poetResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poet, TimeSpan.FromHours(1));
                }
            }
            return new OkObjectResult(poet);
        }

        private async Task<bool> _PreparePoetGroups()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/centuries");
                if (await CaptureErrorIfFailedAsync(response))
                {
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
            if (GetConfigFlag("MaintenanceMode"))
            {
                return StatusCode(503);
            }

            var legacyRedirect = TryHandleLegacyUrlRedirect();
            if (legacyRedirect != null)
            {
                return legacyRedirect;
            }

            InitializeRequestState();

            if (!string.IsNullOrEmpty(Request.Query["p"]))
            {
                return await RedirectByPageIdAsync(Request.Query["p"]);
            }

            await preparePoets();

            var breadCrumbList = new GoogleBreadCrumbList();
            Banner = null;

            if (!IsHomePage)
            {
                var pageLoadResult = await LoadGanjoorPageAsync();
                if (pageLoadResult != null)
                {
                    return pageLoadResult;
                }

                var pageDataResult = await LoadPageTypeSpecificDataAsync();
                if (pageDataResult != null)
                {
                    return pageDataResult;
                }
            }

            await BuildTitleAndBreadCrumbsAsync(breadCrumbList);
            ViewData["BrearCrumpList"] = breadCrumbList.ToString();

            if (IsCatPage || IsPoetPage)
            {
                var geoTagsResult = await LoadCategoryGeoTagsAsync();
                if (geoTagsResult != null)
                {
                    return geoTagsResult;
                }
            }

            return Page();
        }

        /// <summary>
        /// A couple of legacy URL shapes get redirected to their modern equivalents.
        /// Returns null if the current request doesn't match either pattern.
        /// </summary>
        private IActionResult TryHandleLegacyUrlRedirect()
        {
            var path = Request.Path.ToString();

            if (path.IndexOf("index.php") != -1)
            {
                return Redirect($"{path.Replace("index.php", "search")}{Request.QueryString}");
            }

            if (path.IndexOf("vazn") != -1 && Request.QueryString.ToString().IndexOf("v") != -1)
            {
                return Redirect($"{path.Replace("vazn", "simi")}{Request.QueryString}");
            }

            return null;
        }

        /// <summary>
        /// Resets/derives all the per-request flags and view-state that used to be set inline at the
        /// top of OnGetAsync (login state, page-type flags, active tab, tracking script, etc).
        /// </summary>
        private void InitializeRequestState()
        {
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
            ViewData["TrackingScript"] = Configuration["TrackingScript"] != null && string.IsNullOrEmpty(Request.Cookies["Token"])
                ? Configuration["TrackingScript"].Replace("loggedon", "")
                : Configuration["TrackingScript"];
            ActiveTab = Request.Query["tab"];

            if (ShowAllRecitaions && string.IsNullOrEmpty(ActiveTab))
            {
                ActiveTab = "recitations";
            }
            else if (ActiveTab == "recitations" || ActiveTab == "commentaries")
            {
                ShowAllRecitaions = true;
            }
        }

        /// <summary>
        /// Handles the legacy "?p=&lt;id&gt;" query-string form by resolving it to the page's real
        /// URL and redirecting there.
        /// </summary>
        private async Task<IActionResult> RedirectByPageIdAsync(string pageId)
        {
            var pageUrlResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={pageId}");
            if (await CaptureErrorIfFailedAsync(pageUrlResponse))
            {
                return Page();
            }
            var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
            return Redirect(pageUrl);
        }

        /// <summary>
        /// Fetches the GanjoorPage for the current URL and applies the page-type-specific preparation
        /// that depends only on the page payload itself (excerpts, related sections, next/previous
        /// links). Returns null to let OnGetAsync continue, or a terminal IActionResult
        /// (redirect/404/error page) to short-circuit.
        /// </summary>
        private async Task<IActionResult> LoadGanjoorPageAsync()
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

                LastError = await ReadErrorMessageAsync(pageQuery);
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

            return null;
        }

        /// <summary>
        /// The extra API calls that only apply to specific page types: recitation availability for
        /// poet/cat pages, poet photo for poet pages, and the random banner for poem pages.
        /// </summary>
        private async Task<IActionResult> LoadPageTypeSpecificDataAsync()
        {
            if (IsPoetPage || IsCatPage)
            {
                var catHasAnyRecitationQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/audio/catany/{GanjoorPage.PoetOrCat.Cat.Id}");
                if (await CaptureErrorIfFailedAsync(catHasAnyRecitationQuery))
                {
                    return Page();
                }
                CategoryHasRecitations = JsonConvert.DeserializeObject<bool>(await catHasAnyRecitationQuery.Content.ReadAsStringAsync());
            }

            if (IsPoetPage)
            {
                var responsePhotos = await _httpClient.GetAsync($"{APIRoot.Url}/api/poetphotos/poet/{GanjoorPage.PoetOrCat.Poet.Id}");
                if (await CaptureErrorIfFailedAsync(responsePhotos))
                {
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
                if (GetConfigFlag("BannersEnabled"))
                {
                    var bannerQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/banners/random");
                    if (await CaptureErrorIfFailedAsync(bannerQuery))
                    {
                        return Page();
                    }
                    var bannerResponse = await bannerQuery.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(bannerResponse))
                    {
                        Banner = JObject.Parse(bannerResponse).ToObject<GanjoorSiteBannerViewModel>();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Sets ViewData["Title"] and populates the breadcrumb list. This used to be one ~90-line
        /// if/elseif chain inline in OnGetAsync; split into one method per page type below.
        /// </summary>
        private async Task BuildTitleAndBreadCrumbsAsync(GoogleBreadCrumbList breadCrumbList)
        {
            if (IsHomePage)
            {
                ViewData["Title"] = "گنجور";
                await _PreparePoetGroups();
            }
            else if (IsPoetPage)
            {
                BuildPoetPageTitleAndBreadCrumbs(breadCrumbList);
            }
            else if (IsCatPage)
            {
                BuildCatPageTitleAndBreadCrumbs(breadCrumbList);
            }
            else if (IsPoemPage)
            {
                BuildPoemPageTitleAndBreadCrumbs(breadCrumbList);
            }
            else
            {
                BuildGenericPageTitleAndBreadCrumbs(breadCrumbList);
            }
        }

        private void BuildPoetPageTitleAndBreadCrumbs(GoogleBreadCrumbList breadCrumbList)
        {
            ViewData["Title"] = $"گنجور » {GanjoorPage.PoetOrCat.Poet.Nickname}";
            breadCrumbList.AddItem(GanjoorPage.PoetOrCat.Poet.Nickname, GanjoorPage.PoetOrCat.Cat.FullUrl, $"{APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}");
        }

        private void BuildCatPageTitleAndBreadCrumbs(GoogleBreadCrumbList breadCrumbList)
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

        private void BuildPoemPageTitleAndBreadCrumbs(GoogleBreadCrumbList breadCrumbList)
        {
            ViewData["Title"] = $"گنجور » {GanjoorPage.Poem.FullTitle}";
            bool poetCat = true;
            foreach (var gran in GanjoorPage.Poem.Category.Cat.Ancestors)
            {
                breadCrumbList.AddItem(gran.Title, gran.FullUrl, poetCat ? $"{APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}" : "https://i.ganjoor.net/cat.png");
                poetCat = false;
            }
            breadCrumbList.AddItem(GanjoorPage.PoetOrCat.Cat.Title, GanjoorPage.PoetOrCat.Cat.FullUrl, "https://i.ganjoor.net/cat.png");

            var aiMuseumImage = GanjoorPage.Poem.Images.FirstOrDefault(i => i.TargetPageUrl.StartsWith("https://museum.ganjoor.net/items/ai"));
            if (aiMuseumImage != null)
            {
                breadCrumbList.AddItem(GanjoorPage.Poem.Title, GanjoorPage.Poem.FullUrl, aiMuseumImage.ThumbnailImageUrl.Replace("/thumb/", "/orig/"));
            }
            else if (GanjoorPage.Poem.Images.Any())
            {
                breadCrumbList.AddItem(GanjoorPage.Poem.Title, GanjoorPage.Poem.FullUrl, GanjoorPage.Poem.Images.First().ThumbnailImageUrl.Replace("/thumb/", "/orig/"));
            }
            else
            {
                breadCrumbList.AddItem(GanjoorPage.Poem.Title, GanjoorPage.Poem.FullUrl, "https://i.ganjoor.net/poem.png");
            }
        }

        private void BuildGenericPageTitleAndBreadCrumbs(GoogleBreadCrumbList breadCrumbList)
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
                {
                    foreach (var gran in GanjoorPage.PoetOrCat.Cat.Ancestors)
                    {
                        breadCrumbList.AddItem(gran.Title, gran.FullUrl, poetCat ? $"{APIRoot.InternetUrl + GanjoorPage.PoetOrCat.Poet.ImageUrl}" : "https://i.ganjoor.net/cat.png");
                        poetCat = false;
                        fullTitle += $"{gran.Title} » ";
                    }
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

        private async Task<IActionResult> LoadCategoryGeoTagsAsync()
        {
            var tagsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat/{GanjoorPage.PoetOrCat.Cat.Id}/geotag");
            if (await CaptureErrorIfFailedAsync(tagsResponse))
            {
                return Page();
            }
            CategoryPoemGeoDateTags = JsonConvert.DeserializeObject<PoemGeoDateTag[]>(await tagsResponse.Content.ReadAsStringAsync());
            return null;
        }

        public async Task<IActionResult> OnGetBNumPartialAsync(int poemId, int coupletIndex)
        {
            var responseCoupletComments = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}/comments?coupletIndex={coupletIndex}");
            if (!responseCoupletComments.IsSuccessStatusCode)
            {
                return BadRequest(await ReadErrorMessageAsync(responseCoupletComments));
            }
            var comments = JArray.Parse(await responseCoupletComments.Content.ReadAsStringAsync()).ToObject<List<GanjoorCommentSummaryViewModel>>();

            var responseCoupletNumbers = await _httpClient.GetAsync($"{APIRoot.Url}/api/numberings/couplet/{poemId}/{coupletIndex}");
            if (!responseCoupletNumbers.IsSuccessStatusCode)
            {
                return BadRequest(await ReadErrorMessageAsync(responseCoupletNumbers));
            }
            var numbers = JArray.Parse(await responseCoupletNumbers.Content.ReadAsStringAsync()).ToObject<List<GanjoorCoupletNumberViewModel>>();

            var responseCoupletSections = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/couplet/{poemId}/{coupletIndex}/sections");
            if (!responseCoupletSections.IsSuccessStatusCode)
            {
                return BadRequest(await ReadErrorMessageAsync(responseCoupletSections));
            }
            var responseVerses = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}/verses?coupletIndex={coupletIndex}");
            if (!responseVerses.IsSuccessStatusCode)
            {
                return BadRequest(await ReadErrorMessageAsync(responseVerses));
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
                                    return BadRequest(await ReadErrorMessageAsync(responseIsBookmarked));
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

            return Partial("_BNumPartial", new _BNumPartialModel()
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
            });
        }

        public Task<IActionResult> OnPostSwitchBookmarkAsync(int poemId, int coupletIndex)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/bookmark/switch/ret/{poemId}/{coupletIndex}", null);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return new OkObjectResult(res);
            });
        }

        public Task<IActionResult> OnGetIsCoupletBookmarkedAsync(int poemId, int coupletIndex)
        {
            // NOTE: preserved as-is - unlike most handlers here, this one silently returns "false"
            // instead of a 400 when the session can't be prepared (no logged-in user just means
            // "nothing is bookmarked", which is arguably correct, but it's inconsistent with e.g.
            // OnGetUserUpvotedRecitationsAsync below). Worth a deliberate decision, not a silent fix.
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/bookmark/{poemId}/{coupletIndex}");
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                var res = JsonConvert.DeserializeObject<bool>(await response.Content.ReadAsStringAsync());
                return new OkObjectResult(res);
            }, new OkObjectResult(false));
        }

        public Task<IActionResult> OnGetPoemBookmarksAsync(int poemId)
        {
            // Same note as OnGetIsCoupletBookmarkedAsync above: falls back to OkObjectResult(false)
            // rather than an error when not logged in (preserved from the original behavior).
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/bookmark/{poemId}");
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                var res = JsonConvert.DeserializeObject<GanjoorUserBookmarkViewModel[]>(await response.Content.ReadAsStringAsync());
                return new OkObjectResult(res);
            }, new OkObjectResult(false));
        }

        /// <summary>
        /// unused now, to be removed
        /// </summary>
        /// <param name="poemId"></param>
        /// <returns></returns>
        public Task<IActionResult> OnGetUserUpvotedRecitationsAsync(int poemId)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}/recitations/upvotes");
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                var res = JsonConvert.DeserializeObject<int[]>(await response.Content.ReadAsStringAsync());
                return new OkObjectResult(res);
            });
        }

        public Task<IActionResult> OnPostSwitchRecitationUpVoteAsync(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/audio/vote/switch/{id}", null);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                var res = JsonConvert.DeserializeObject<bool>(await response.Content.ReadAsStringAsync());
                return new OkObjectResult(res);
            });
        }

        public Task<IActionResult> OnPostAddToMyHistoryAsync(int poemId)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PostAsync($"{APIRoot.Url}/api/tracking", new StringContent(JsonConvert.SerializeObject(poemId), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                var res = JsonConvert.DeserializeObject<GanjoorUserPrePoemVisitViewModel>(await response.Content.ReadAsStringAsync());
                if (res.KeepTrack == false)
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
                return new OkObjectResult(res.KeepTrack && res.LastVisit != null ? $"از این صفحه آخرین بار {res.LastVisit.ToFriendlyPersianDateTextify()} و در مجموع {res.TotalVisits.ToPersianNumbers()} بار بازدید کرده‌ام." : "");
            });
        }

        public Task<IActionResult> OnPutBookmarkNote(Guid id, string note)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/bookmark/{id}", new StringContent(JsonConvert.SerializeObject(note), Encoding.UTF8, "application/json"));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new JsonResult(true);
            });
        }

        public Task<IActionResult> OnDeleteMistakeAsync(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/audio/errors/approved/{id}");
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new JsonResult(true);
            });
        }

        public Task<IActionResult> OnPutMistakeAsync(int id, string reasonText)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var stringContent = new StringContent(
                   JsonConvert.SerializeObject(new RecitationErrorReportViewModel()
                   {
                       Id = id,
                       ReasonText = reasonText,
                       NumberOfLinesAffected = 1,
                       CoupletIndex = -1,
                   }),
                   Encoding.UTF8, "application/json");

                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/audio/errors/report/edit", stringContent);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new JsonResult(true);
            });
        }

        public async Task<IActionResult> OnGetMoreQuotedPoemsForRelatedPoemPartialAsync(int poemId, int relatedPoemId, string poetImageUrl, string poetNickName, bool canEdit)
        {
            string url = $"{APIRoot.Url}/api/ganjoor/poem/{poemId}/quoteds/{relatedPoemId}?published=true";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return BadRequest(await ReadErrorMessageAsync(response));

            var quoteds = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorQuotedPoemViewModel>>();
            if (!quoteds.Any())
            {
                return BadRequest("مورد دیگری یافت نشد.");
            }

            if (quoteds.Any(q => q.ChosenForMainList))
            {
                var mainList = quoteds.Where(q => q.ChosenForMainList).First();
                quoteds.Remove(mainList);
            }

            return Partial("_MultipleQuotedPoemsPartial", new _MultipleQuotedPoemsPartialModel()
            {
                GanjoorQuotedPoems = quoteds.ToArray(),
                PoetImageUrl = poetImageUrl,
                PoetNickName = poetNickName,
                CanEdit = canEdit
            });
        }

        public async Task<IActionResult> OnGetMoreQuotedPoemsPartialAsync(int poemId, int skip, string poetImageUrl, string poetNickName, bool canEdit)
        {
            string url = $"{APIRoot.Url}/api/ganjoor/poem/{poemId}/quoteds?skip={skip}&itemsCount=1000";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return BadRequest(await ReadErrorMessageAsync(response));

            var quoteds = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorQuotedPoemViewModel>>();

            return Partial("_MultipleQuotedPoemsPartial", new _MultipleQuotedPoemsPartialModel()
            {
                GanjoorQuotedPoems = quoteds.ToArray(),
                PoetImageUrl = poetImageUrl,
                PoetNickName = poetNickName,
                CanEdit = canEdit
            });
        }

        public string PoemBlockClass => GanjoorPage != null && GanjoorPage.Poem != null && GanjoorPage.Poem.ClaimedByMultiplePoets ? "poem ribbon-parent" : "poem";

        public Task<IActionResult> OnPostMarkAsTextOriginalAsync(int bookId, int categoryId)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/naskban/textoriginal/{bookId}/{categoryId}/true", null);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            });
        }

        public Task<IActionResult> OnDeleteRelatedImageLinkAsync(PoemRelatedImageType relatedImageType, Guid linkId, bool removeItemLink)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.DeleteAsync(
                    relatedImageType == PoemRelatedImageType.MuseumLink
                        ? $"{APIRoot.Url}/api/artifacts/ganjoor?linkId={linkId}&removeItemLink={removeItemLink}"
                        : $"{APIRoot.Url}/api/artifacts/pinterest?linkId={linkId}");
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            });
        }

        public async Task<IActionResult> OnGetCategoryRecitationsAsync(int catId)
        {
            var catTop1RecitationsQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/audio/cattop1/{catId}?includePoemText=false");
            if (!catTop1RecitationsQuery.IsSuccessStatusCode)
            {
                return BadRequest(await ReadErrorMessageAsync(catTop1RecitationsQuery));
            }
            var categoryTop1Recitations = JsonConvert.DeserializeObject<PublicRecitationViewModel[]>(await catTop1RecitationsQuery.Content.ReadAsStringAsync());

            return Partial("_AudioPlayerPartial", new _AudioPlayerPartialModel()
            {
                LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]),
                Recitations = categoryTop1Recitations,
                ShowAllRecitaions = true,
                CategoryMode = true,
            });
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
                wordCounts = wordCounts.Where(w => !_persianStopWords.Contains(w.Word)).Take(100).ToArray();
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

        public async Task<IActionResult> OnGetCategoryWordCountsAsync(int catId, int poetId, bool remStopWords = false)
        {
            var res = await _GetCategoryWordCountsAsync(catId, poetId, remStopWords);
            if (res == null)
            {
                return BadRequest("خطا در دسترسی به شمارش واژگان");
            }

            return Partial("_CategoryWordsCountPartial", res);
        }

        public async Task<IActionResult> OnGetSearchCategoryWordCountsAsync(int catId, int poetId, string term, int totalWordCount)
        {
            if (!string.IsNullOrEmpty(term))
            {
                term = term.Trim();
            }
            var wordCountsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/wordcounts/{catId}?PageNumber=1&PageSize=100&term={term}");
            if (!wordCountsResponse.IsSuccessStatusCode)
            {
                return BadRequest(await ReadErrorMessageAsync(wordCountsResponse));
            }
            var wordCounts = JsonConvert.DeserializeObject<CategoryWordCount[]>(await wordCountsResponse.Content.ReadAsStringAsync());

            return Partial("_CategoryWordsCountTablePartial", new _CategoryWordsCountTablePartialModel()
            {
                CatId = catId,
                PoetId = poetId,
                WordCounts = wordCounts,
                TotalWordCount = totalWordCount,
            });
        }

        public async Task<IActionResult> OnGetPoemWordCountsAsync(int poemId)
        {
            var poemQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}");
            if (!poemQuery.IsSuccessStatusCode)
            {
                return BadRequest(await ReadErrorMessageAsync(poemQuery));
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

            return Partial("_CategoryWordsCountTablePartial", new _CategoryWordsCountTablePartialModel()
            {
                CatId = -1,
                PoetId = -1,
                TotalWordCount = counts.Where(c => c.RowNmbrInCat > 0).Sum(c => c.Count),
                WordCounts = counts.ToArray()
            });
        }

        public async Task<IActionResult> OnGetTopVisitsAsync(string url)
        {
            url = $"https://ganjoor.net/{url}/";
            var apiUrl = $"https://track.kntr.ir/api/reporting/toppages/1/ganjoor.net?parentUrl={WebUtility.UrlEncode(url)}&count=20";
            var topVisitsResponse = await _httpClient.GetAsync(apiUrl);

            if (!topVisitsResponse.IsSuccessStatusCode)
            {
                return Partial("_TopVisitsPartial", new _TopVisitsPartialModel()
                {
                    Visits = null
                });
            }
            var topVisits = JsonConvert.DeserializeObject<PageVisitsViewModel[]>(await topVisitsResponse.Content.ReadAsStringAsync());

            return Partial("_TopVisitsPartial", new _TopVisitsPartialModel()
            {
                Visits = topVisits
            });
        }

        public async Task<IActionResult> OnGetSevenDaysVisitsAsync(string url)
        {
            url = $"https://ganjoor.net/{url}/";
            var apiUrl = $"https://track.kntr.ir/api/reporting/dailypagevisits/1/ganjoor.net/for/{WebUtility.UrlEncode(url)}?start={DateTime.Now.Date.AddDays(-6).ToString("yyyy-MM-dd")}";
            var s7ndaysVisitsResponse = await _httpClient.GetAsync(apiUrl);

            if (!s7ndaysVisitsResponse.IsSuccessStatusCode)
            {
                return Partial("_7DaysVisitsPartial", new _7DaysVisitsPartialModel()
                {
                    SevenDaysVisits = null
                });
            }
            var s7ndaysVisits = JsonConvert.DeserializeObject<DateRangeVisitsViewModel[]>(await s7ndaysVisitsResponse.Content.ReadAsStringAsync());

            return Partial("_7DaysVisitsPartial", new _7DaysVisitsPartialModel()
            {
                SevenDaysVisits = s7ndaysVisits
            });
        }
    }
}
