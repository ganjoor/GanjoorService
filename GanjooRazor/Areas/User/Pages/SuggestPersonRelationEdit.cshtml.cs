using System;
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
    /// contributor-facing form for suggesting a change to, or removal of, an existing edge between
    /// two people - the kinship-edge/affiliation-edge counterpart of SuggestPersonEdit.cshtml. Covers
    /// both kinds: pass relationId for a GanjoorPersonRelation (family), or affiliationId for a
    /// GanjoorPersonAffiliation (non-family) - exactly one of the two is expected, matching which ✎
    /// link on PersonWindow the contributor came from. Any logged-in user may submit here; nothing is
    /// changed until a moderator approves it via Admin/ReviewPersonRelationEdits.
    /// </summary>
    public class SuggestPersonRelationEditModel : LoginPartialEnabledPageModel
    {
        public SuggestPersonRelationEditModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public string LastError { get; set; }

        public string LastResult { get; set; }

        /// <summary>
        /// "family" when editing a GanjoorPersonRelation (Relation is populated), "affiliation" when
        /// editing a GanjoorPersonAffiliation (Affiliation is populated) - decides which of the two
        /// the view renders and which Suggestion.Existing*Id gets set on post
        /// </summary>
        public PersonRelationSuggestionKind Kind { get; set; }

        public GanjoorPersonRelation Relation { get; set; }

        public GanjoorPersonAffiliation Affiliation { get; set; }

        /// <summary>
        /// whichever side (Relation.Person1/Person2 or Affiliation.Person1/Person2) is populated -
        /// used by the view for the page heading/links without needing to branch on Kind there too
        /// </summary>
        public int Person1Id => Kind == PersonRelationSuggestionKind.Affiliation ? Affiliation.Person1Id : Relation.Person1Id;
        public string Person1Name => Kind == PersonRelationSuggestionKind.Affiliation ? (Affiliation.Person1?.Name ?? Affiliation.Person1Id.ToString()) : (Relation.Person1?.Name ?? Relation.Person1Id.ToString());
        public int Person2Id => Kind == PersonRelationSuggestionKind.Affiliation ? Affiliation.Person2Id : Relation.Person2Id;
        public string Person2Name => Kind == PersonRelationSuggestionKind.Affiliation ? (Affiliation.Person2?.Name ?? Affiliation.Person2Id.ToString()) : (Relation.Person2?.Name ?? Relation.Person2Id.ToString());

        [BindProperty]
        public GanjoorPersonRelationEditSuggestion Suggestion { get; set; }

        [BindProperty]
        public string Action { get; set; } // "Modify" or "Remove", from the posted radio buttons

        private async Task<bool> PrepareAsync(int? relationId, int? affiliationId)
        {
            if (affiliationId == null && relationId == null)
            {
                LastError = "شناسهٔ نسبت یا وابستگی مشخص نشده است.";
                return false;
            }

            if (affiliationId != null)
            {
                Kind = PersonRelationSuggestionKind.Affiliation;

                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/affiliations/{affiliationId.Value}");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = await ReadErrorMessageAsync(response);
                    return false;
                }

                Affiliation = JsonConvert.DeserializeObject<GanjoorPersonAffiliation>(await response.Content.ReadAsStringAsync());
                if (Affiliation == null)
                {
                    LastError = "وابستگی‌ای با این کد پیدا نشد.";
                    return false;
                }

                return true;
            }
            else
            {
                Kind = PersonRelationSuggestionKind.Family;

                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/relations/{relationId.Value}");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = await ReadErrorMessageAsync(response);
                    return false;
                }

                Relation = JsonConvert.DeserializeObject<GanjoorPersonRelation>(await response.Content.ReadAsStringAsync());
                if (Relation == null)
                {
                    LastError = "نسبتی با این کد پیدا نشد.";
                    return false;
                }

                return true;
            }
        }

        private void FillSuggestionFromCurrent()
        {
            // pre-filled with the edge's CURRENT values, same convention as SuggestPersonEdit -
            // submitting a plain "Modify" without changing anything is a harmless no-op
            if (Kind == PersonRelationSuggestionKind.Affiliation)
            {
                Suggestion = new GanjoorPersonRelationEditSuggestion()
                {
                    Action = PersonRelationSuggestionAction.Modify,
                    Kind = PersonRelationSuggestionKind.Affiliation,
                    ExistingAffiliationId = Affiliation.Id,
                    Person1Id = Affiliation.Person1Id,
                    Person2Id = Affiliation.Person2Id,
                    SuggestedAffiliationType = Affiliation.AffiliationType,
                    SuggestedNote = Affiliation.Note,
                };
            }
            else
            {
                Suggestion = new GanjoorPersonRelationEditSuggestion()
                {
                    Action = PersonRelationSuggestionAction.Modify,
                    Kind = PersonRelationSuggestionKind.Family,
                    ExistingRelationId = Relation.Id,
                    Person1Id = Relation.Person1Id,
                    Person2Id = Relation.Person2Id,
                    SuggestedRelationType = Relation.RelationType,
                    SuggestedDegreeHint = Relation.DegreeHint,
                    SuggestedNote = Relation.Note,
                };
            }
        }

        public async Task<IActionResult> OnGetAsync(int? relationId, int? affiliationId)
        {
            InitializeCommonPageState();

            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path + Request.QueryString)}");
            }

            if (!await PrepareAsync(relationId, affiliationId))
            {
                return Page();
            }

            FillSuggestionFromCurrent();

            return Page();
        }

        public async Task<IActionResult> OnPostSuggestEditAsync(int? relationId, int? affiliationId)
        {
            InitializeCommonPageState();

            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path + Request.QueryString)}");
            }

            LastResult = "";

            if (!await PrepareAsync(relationId, affiliationId))
            {
                return Page();
            }

            Suggestion.Kind = Kind;
            Suggestion.Action = Action == "Remove" ? PersonRelationSuggestionAction.Remove : PersonRelationSuggestionAction.Modify;
            if (Kind == PersonRelationSuggestionKind.Affiliation)
            {
                Suggestion.ExistingAffiliationId = Affiliation.Id;
                Suggestion.ExistingRelationId = null;
            }
            else
            {
                Suggestion.ExistingRelationId = Relation.Id;
                Suggestion.ExistingAffiliationId = null;
            }

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                HttpResponseMessage response = await secureClient.PostAsync(
                    $"{APIRoot.Url}/api/people/relationeditsuggestion",
                    new StringContent(JsonConvert.SerializeObject(Suggestion), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    LastResult = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    FillSuggestionFromCurrent();
                    return Page();
                }

                LastResult = $"پیشنهاد شما ثبت شد و پس از بررسی توسط مدیران اعمال خواهد شد. <a role=\"button\" href=\"javascript:void(0)\" onclick=\"PersonWindow.open({Person1Id})\" class=\"actionlink\">مشاهدهٔ اطلاعات این نامبرده</a>";

                FillSuggestionFromCurrent();

                return Page();
            }
        }
    }
}
