using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Pages;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Areas.User.Pages
{
    /// <summary>
    /// contributor-facing form for suggesting that a person is a duplicate of another one (merge) or
    /// is really two people with the same name (split). Nothing changes until a moderator approves it
    /// via Admin/ReviewPersonEdits.
    /// </summary>
    public class SuggestPersonMergeSplitModel : LoginPartialEnabledPageModel
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="configuration"></param>
        public SuggestPersonMergeSplitModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        /// <summary>
        /// error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// result of the last submission
        /// </summary>
        public string LastResult { get; set; }

        /// <summary>
        /// the person
        /// </summary>
        public GanjoorRelatedPerson Person { get; set; }

        /// <summary>
        /// all people (for choosing the merge target)
        /// </summary>
        public GanjoorRelatedPerson[] People { get; set; }

        /// <summary>
        /// the couplets this person is tagged in, in book order - the start/end choices of a split
        /// </summary>
        public List<PoemGeoDateTag> Tags { get; set; } = new List<PoemGeoDateTag>();

        /// <summary>
        /// merge target typed as "name (id)"
        /// </summary>
        [BindProperty]
        public string MergeTarget { get; set; }

        /// <summary>
        /// reason
        /// </summary>
        [BindProperty]
        public string Reason { get; set; }

        /// <summary>
        /// split start, "poemId|coupletIndex"
        /// </summary>
        [BindProperty]
        public string SplitFrom { get; set; }

        /// <summary>
        /// split end, "poemId|coupletIndex" or empty for the end of the book
        /// </summary>
        [BindProperty]
        public string SplitTo { get; set; }

        /// <summary>
        /// name of the new person
        /// </summary>
        [BindProperty]
        public string SplitNewName { get; set; }

        /// <summary>
        /// description of the new person
        /// </summary>
        [BindProperty]
        public string SplitNewDescription { get; set; }

        private async Task<bool> PrepareAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/{id}");
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return false;
            }
            Person = JsonConvert.DeserializeObject<GanjoorRelatedPerson>(await response.Content.ReadAsStringAsync());
            if (Person == null)
            {
                LastError = "نامبرده‌ای با این کد پیدا نشد.";
                return false;
            }

            var peopleResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/people");
            People = peopleResponse.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<GanjoorRelatedPerson[]>(await peopleResponse.Content.ReadAsStringAsync())
                : new GanjoorRelatedPerson[0];

            var poemsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/{id}/poems");
            if (poemsResponse.IsSuccessStatusCode)
            {
                var tags = JsonConvert.DeserializeObject<PoemGeoDateTag[]>(await poemsResponse.Content.ReadAsStringAsync());
                Tags = tags.Where(t => t.Poem != null).OrderBy(t => t.PoemId).ThenBy(t => t.CoupletIndex).ToList();
            }
            return true;
        }

        /// <summary>
        /// get
        /// </summary>
        public async Task<IActionResult> OnGetAsync(int id)
        {
            InitializeCommonPageState();
            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path + Request.QueryString)}");
            }
            await PrepareAsync(id);
            return Page();
        }

        private static bool TryParsePoint(string text, out int poemId, out int couplet)
        {
            poemId = 0;
            couplet = 0;
            var parts = (text ?? "").Split('|');
            return parts.Length == 2 && int.TryParse(parts[0], out poemId) && int.TryParse(parts[1], out couplet);
        }

        private async Task<IActionResult> SubmitAsync(int id, GanjoorPersonEditSuggestion suggestion)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);
                HttpResponseMessage response = await secureClient.PostAsync(
                    $"{APIRoot.Url}/api/people/{id}/editsuggestion",
                    new StringContent(JsonConvert.SerializeObject(suggestion), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    LastResult = System.Net.WebUtility.HtmlEncode(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    return Page();
                }
                LastResult = "پیشنهاد شما ثبت شد و پس از بررسی توسط مدیران اعمال خواهد شد.";
                Reason = MergeTarget = SplitFrom = SplitTo = SplitNewName = SplitNewDescription = null;
                ModelState.Clear();
                return Page();
            }
        }

        /// <summary>
        /// submit a merge suggestion
        /// </summary>
        public async Task<IActionResult> OnPostMergeAsync(int id)
        {
            InitializeCommonPageState();
            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path + Request.QueryString)}");
            }
            LastResult = "";
            if (!await PrepareAsync(id))
                return Page();

            var m = System.Text.RegularExpressions.Regex.Match(MergeTarget ?? "", @"\((\d+)\)\s*$");
            if (!m.Success && !int.TryParse((MergeTarget ?? "").Trim(), out _))
            {
                LastResult = "شخصیت دیگر را از فهرست پیشنهادی انتخاب کنید.";
                return Page();
            }
            int targetId = m.Success ? int.Parse(m.Groups[1].Value) : int.Parse(MergeTarget.Trim());
            return await SubmitAsync(id, new GanjoorPersonEditSuggestion()
            {
                PersonId = id,
                Operation = PersonSuggestionOperation.Merge,
                MergeIntoPersonId = targetId,
                SuggestionNote = Reason,
            });
        }

        /// <summary>
        /// submit a split suggestion
        /// </summary>
        public async Task<IActionResult> OnPostSplitAsync(int id)
        {
            InitializeCommonPageState();
            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path + Request.QueryString)}");
            }
            LastResult = "";
            if (!await PrepareAsync(id))
                return Page();

            if (!TryParsePoint(SplitFrom, out int fromPoem, out int fromCouplet))
            {
                LastResult = "بیت آغاز بخش دوم را انتخاب کنید.";
                return Page();
            }
            var suggestion = new GanjoorPersonEditSuggestion()
            {
                PersonId = id,
                Operation = PersonSuggestionOperation.Split,
                SplitFromPoemId = fromPoem,
                SplitFromCoupletIndex = fromCouplet,
                SplitNewName = SplitNewName,
                SplitNewDescription = SplitNewDescription,
                SuggestionNote = Reason,
            };
            if (!string.IsNullOrWhiteSpace(SplitTo))
            {
                if (!TryParsePoint(SplitTo, out int toPoem, out int toCouplet))
                {
                    LastResult = "بیت پایان نامعتبر است.";
                    return Page();
                }
                suggestion.SplitToPoemId = toPoem;
                suggestion.SplitToCoupletIndex = toCouplet;
            }
            return await SubmitAsync(id, suggestion);
        }
    }
}
