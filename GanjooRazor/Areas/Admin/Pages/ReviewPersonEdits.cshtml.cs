using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;

namespace GanjooRazor.Areas.Admin.Pages
{
    /// <summary>
    /// moderator review queue for pending GanjoorPersonEditSuggestion entries - counterpart of
    /// ReviewEdits.cshtml for poem corrections, but much simpler: one suggestion, one overall
    /// Result, no per-field branching.
    /// </summary>
    public class ReviewPersonEditsModel : PageModel
    {
        /// <summary>
        /// the pending suggestion currently shown
        /// </summary>
        public GanjoorPersonEditSuggestion Suggestion { get; set; }

        /// <summary>
        /// fatal error
        /// </summary>
        public string FatalError { get; set; }

        /// <summary>
        /// skip
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// total count
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// full location catalog, used only to resolve the CURRENT person's BirthLocationId/
        /// DeathLocationId into names for the before/after diff (the suggestion's own Suggested*
        /// locations already arrive populated via the backend's Include)
        /// </summary>
        public List<GanjoorGeoLocation> Locations { get; set; }

        public string LocationName(int? id)
        {
            if (id == null || Locations == null)
                return "";
            var loc = Locations.Where(l => l.Id == id).FirstOrDefault();
            return loc == null ? "" : loc.Name;
        }

        public string ImportanceLabel(PersonImportance importance)
        {
            switch (importance)
            {
                case PersonImportance.Important:
                    return "مهم";
                case PersonImportance.VeryImportant:
                    return "بسیار مهم";
                default:
                    return "معمولی";
            }
        }

        public IActionResult OnPost()
        {
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            if (Request.Form["next"].Count == 1)
            {
                return Redirect($"/Admin/ReviewPersonEdits/?skip={Skip + 1}");
            }
            return Page();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            FatalError = "";
            TotalCount = 0;
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var locationsResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/locations");
                    if (!locationsResponse.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await locationsResponse.Content.ReadAsStringAsync());
                        return Page();
                    }
                    Locations = JsonConvert.DeserializeObject<List<GanjoorGeoLocation>>(await locationsResponse.Content.ReadAsStringAsync());

                    var nextResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/people/editsuggestions/next?skip={Skip}");
                    if (!nextResponse.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await nextResponse.Content.ReadAsStringAsync());
                        return Page();
                    }

                    string paginationMetadata = nextResponse.Headers.GetValues("paging-headers").FirstOrDefault();
                    if (!string.IsNullOrEmpty(paginationMetadata))
                    {
                        TotalCount = JsonConvert.DeserializeObject<PaginationMetadata>(paginationMetadata).totalCount;
                    }

                    Suggestion = JsonConvert.DeserializeObject<GanjoorPersonEditSuggestion>(await nextResponse.Content.ReadAsStringAsync());

                    return Page();
                }
                else
                {
                    return Redirect("/");
                }
            }
        }

        public async Task<IActionResult> OnPostModerateAsync([FromBody] PersonEditSuggestionModerationRequest pms)
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return new BadRequestObjectResult("لطفاً از گنجور خارج و مجدداً به آن وارد شوید.");

            if (pms == null || string.IsNullOrEmpty(pms.Result))
            {
                return new BadRequestObjectResult("لطفاً تکلیف بررسی این پیشنهاد را مشخص کنید.");
            }

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var moderation = new PersonEditSuggestionModerationViewModel()
                    {
                        Result = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.Result),
                        ReviewNote = pms.ReviewNote,
                    };

                    var moderationResponse = await secureClient.PostAsync($"{APIRoot.Url}/api/people/editsuggestions/{pms.Id}/moderate",
                        new StringContent(JsonConvert.SerializeObject(moderation), Encoding.UTF8, "application/json"));

                    if (!moderationResponse.IsSuccessStatusCode)
                    {
                        string err = await moderationResponse.Content.ReadAsStringAsync();
                        if (string.IsNullOrEmpty(err))
                        {
                            if (!string.IsNullOrEmpty(moderationResponse.ReasonPhrase))
                            {
                                err = moderationResponse.ReasonPhrase;
                            }
                            else
                            {
                                err = $"Error Code: {moderationResponse.StatusCode}";
                            }
                        }
                        else
                        {
                            err = JsonConvert.DeserializeObject<string>(err);
                        }
                        return new BadRequestObjectResult(err);
                    }

                    return new OkObjectResult(true);
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجدداً به آن وارد شوید.");
                }
            }
        }
    }

    /// <summary>
    /// JSON body shape posted by ReviewPersonEdits.cshtml's moderatePersonEditSuggestion() ajax call -
    /// kept local to this page since it's only ever used to bind that one handler's [FromBody] parameter
    /// </summary>
    public class PersonEditSuggestionModerationRequest
    {
        public int Id { get; set; }
        public string Result { get; set; }
        public string ReviewNote { get; set; }
    }
}
