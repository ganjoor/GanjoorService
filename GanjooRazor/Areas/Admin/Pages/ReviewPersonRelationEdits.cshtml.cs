using System;
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
    /// moderator review queue for pending GanjoorPersonRelationEditSuggestion entries - counterpart
    /// of ReviewPersonEdits.cshtml, but for kinship edges (Add/Modify/Remove) rather than a person's
    /// own fields.
    /// </summary>
    public class ReviewPersonRelationEditsModel : PageModel
    {
        /// <summary>
        /// the pending suggestion currently shown
        /// </summary>
        public GanjoorPersonRelationEditSuggestion Suggestion { get; set; }

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

        public IActionResult OnPost()
        {
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            if (Request.Form["next"].Count == 1)
            {
                return Redirect($"/Admin/ReviewPersonRelationEdits/?skip={Skip + 1}");
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
                    var nextResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/people/relationeditsuggestions/next?skip={Skip}");
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

                    Suggestion = JsonConvert.DeserializeObject<GanjoorPersonRelationEditSuggestion>(await nextResponse.Content.ReadAsStringAsync());

                    return Page();
                }
                else
                {
                    return Redirect("/");
                }
            }
        }

        public async Task<IActionResult> OnPostModerateAsync([FromBody] PersonRelationEditSuggestionModerationRequest pms)
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

                    var moderationResponse = await secureClient.PostAsync($"{APIRoot.Url}/api/people/relationeditsuggestions/{pms.Id}/moderate",
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
    /// JSON body shape posted by ReviewPersonRelationEdits.cshtml's moderatePersonRelationEditSuggestion()
    /// ajax call - kept local to this page since it's only ever used to bind that one handler's
    /// [FromBody] parameter
    /// </summary>
    public class PersonRelationEditSuggestionModerationRequest
    {
        public int Id { get; set; }
        public string Result { get; set; }
        public string ReviewNote { get; set; }
    }
}
