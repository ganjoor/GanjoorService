using DNTPersianUtils.Core;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Auth.ViewModel;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    public class LoginPartialEnabledPageModel : GanjoorPageModelBase
    {
        /// <summary>
        /// configuration file reader (appsettings.json). Was previously duplicated as a private field
        /// on almost every page model that derives from this class (Index, Contribs, Quotes, FAQ,
        /// Books, Hashieha, Search, Simi all had their own copy).
        /// </summary>
        protected readonly IConfiguration Configuration;

        /// <summary>
        /// Reads a boolean feature flag from configuration, defaulting to <paramref name="defaultValue"/>
        /// if the key is missing or not a valid bool. Was previously a per-page try/catch block
        /// (and in IndexModel's case, was the ONLY one of these that had the try/catch at all -
        /// every other page's `bool.Parse(Configuration["MaintenanceMode"])` below would throw on a
        /// missing/malformed config key instead of defaulting to false).
        /// </summary>
        protected bool GetConfigFlag(string key, bool defaultValue = false)
        {
            return bool.TryParse(Configuration[key], out var value) ? value : defaultValue;
        }

        /// <summary>
        /// aggressive cache flag. Every page model that had this property defined it identically -
        /// same try/catch-wrapped bool.Parse now handled once by GetConfigFlag.
        /// </summary>
        protected bool AggressiveCacheEnabled => GetConfigFlag("AggressiveCacheEnabled");

        /// <summary>
        /// Returns a 503 result if the site is in maintenance mode, or null otherwise. Was previously
        /// `if (bool.Parse(Configuration["MaintenanceMode"])) { return StatusCode(503); }` duplicated
        /// verbatim at the top of OnGetAsync in 8 different page models, all without try/catch (so a
        /// missing "MaintenanceMode" config key would throw a FormatException/ArgumentNullException
        /// instead of just... not being in maintenance mode).
        /// </summary>
        protected IActionResult TryGetMaintenanceModeResult()
        {
            return GetConfigFlag("MaintenanceMode") ? StatusCode(503) : null;
        }

        /// <summary>
        /// Sets the two pieces of per-request state that were duplicated character-for-character
        /// across every page model in this hierarchy: whether the current visitor is logged in, and
        /// the tracking script to render (with "loggedon" stripped out for anonymous visitors).
        /// Call once near the top of each page's OnGetAsync instead of repeating both lines.
        /// </summary>
        protected void InitializeCommonPageState()
        {
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);
            ViewData["TrackingScript"] = Configuration["TrackingScript"] != null && string.IsNullOrEmpty(Request.Cookies["Token"])
                ? Configuration["TrackingScript"].Replace("loggedon", "")
                : Configuration["TrackingScript"];
        }

        /// <summary>
        /// is logged on
        /// </summary>
        public bool LoggedIn { get; set; }

        [BindProperty]
        public LoginViewModel LoginViewModel { get; set; }

        /// <summary>
        /// Corresponding Ganjoor Page
        /// </summary>
        public GanjoorPageCompleteViewModel GanjoorPage { get; set; }

        public string NextUrl { get; set; }

        public string NextTitle { get; set; }

        public string PreviousUrl { get; set; }

        public string PreviousTitle { get; set; }

        public bool CanTranslate { get; set; }

        public List<GanjoorPoemSection> SectionsWithRelated { get; set; }

        public List<GanjoorPoemSection> SectionsWithMetreAndRhymes { get; set; }

        
        public PoemGeoDateTag[] CategoryPoemGeoDateTags { get; set; }

        public bool CategoryHasRecitations { get; set; }

        public _CategoryWordsCountPartialModel CategoryWordsCounts { get; set; }

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
            foreach (var cookieName in new string[] { "UserId", "SessionId", "Token", "Username", "Name", "NickName", "CanEdit", "KeepHistory", "CanTranslate" })
            {
                if (Request.Cookies[cookieName] != null)
                {
                    Response.Cookies.Append(cookieName, "", cookieOption);
                }
            }


            return Redirect(Request.Path);
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

            if (!response.IsSuccessStatusCode)
            {
                return Redirect($"/login?redirect={Request.Path}&error={JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync())}");
            }

            LoggedOnUserModelEx loggedOnUser = JsonConvert.DeserializeObject<LoggedOnUserModelEx>(await response.Content.ReadAsStringAsync());

            var cookieOption = new CookieOptions()
            {
                Expires = DateTime.Now.AddDays(365),
            };

            Response.Cookies.Append("UserId", loggedOnUser.User.Id.ToString(), cookieOption);
            Response.Cookies.Append("SessionId", loggedOnUser.SessionId.ToString(), cookieOption);
            Response.Cookies.Append("Token", loggedOnUser.Token, cookieOption);
            Response.Cookies.Append("Username", loggedOnUser.User.Username, cookieOption);
            Response.Cookies.Append("Name", $"{loggedOnUser.User.FirstName} {loggedOnUser.User.SurName}", cookieOption);
            Response.Cookies.Append("NickName", $"{loggedOnUser.User.NickName}", cookieOption);
            Response.Cookies.Append("KeepHistory", $"{loggedOnUser.KeepHistory}", cookieOption);

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

            bool canTranlate = false;
            if (ganjoorEntity != null)
            {
                var op = ganjoorEntity.Operations.Where(o => o.ShortName == RMuseumSecurableItem.Translations).SingleOrDefault();
                if (op != null)
                {
                    canTranlate = op.Status;
                }
            }
            Response.Cookies.Append("CanTranslate", canTranlate.ToString(), cookieOption);


            return Redirect(Request.Path);
        }

        public Task<IActionResult> OnGetCheckIfHasNotificationsAsync()
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.GetAsync($"{APIRoot.Url}/api/notifications/unread/count");
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                var res = JsonConvert.DeserializeObject<int>(await response.Content.ReadAsStringAsync());
                return new OkObjectResult(res == 0 ? "" : res.ToString().ToPersianNumbers());
            });
        }

        public LoginPartialEnabledPageModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient)
        {
            Configuration = configuration;
        }
    }
}
