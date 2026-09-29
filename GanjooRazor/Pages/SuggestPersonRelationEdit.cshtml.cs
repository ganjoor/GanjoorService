using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// contributor-facing form for suggesting a change to, or removal of, an existing
    /// GanjoorPersonRelation - the kinship-edge counterpart of SuggestPersonEdit.cshtml. Any
    /// logged-in user may submit here; nothing is changed until a moderator approves it via
    /// Admin/ReviewPersonRelationEdits. Reachable from a person's own public page (Person.cshtml).
    /// </summary>
    public class SuggestPersonRelationEditModel : LoginPartialEnabledPageModel
    {
        public SuggestPersonRelationEditModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public string LastError { get; set; }

        public string LastResult { get; set; }

        public GanjoorPersonRelation Relation { get; set; }

        [BindProperty]
        public GanjoorPersonRelationEditSuggestion Suggestion { get; set; }

        [BindProperty]
        public string Action { get; set; } // "Modify" or "Remove", from the posted radio buttons

        private async Task<bool> PrepareRelationAsync(int relationId)
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/relations/{relationId}");
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

        private void FillSuggestionFromCurrentRelation()
        {
            // pre-filled with the relation's CURRENT values, same convention as SuggestPersonEdit -
            // submitting a plain "Modify" without changing anything is a harmless no-op
            Suggestion = new GanjoorPersonRelationEditSuggestion()
            {
                Action = PersonRelationSuggestionAction.Modify,
                ExistingRelationId = Relation.Id,
                Person1Id = Relation.Person1Id,
                Person2Id = Relation.Person2Id,
                SuggestedRelationType = Relation.RelationType,
                SuggestedDegreeHint = Relation.DegreeHint,
                SuggestedNote = Relation.Note,
            };
        }

        public async Task<IActionResult> OnGetAsync(int relationId)
        {
            InitializeCommonPageState();

            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path)}");
            }

            if (!await PrepareRelationAsync(relationId))
            {
                return Page();
            }

            FillSuggestionFromCurrentRelation();

            return Page();
        }

        public async Task<IActionResult> OnPostSuggestEditAsync(int relationId)
        {
            InitializeCommonPageState();

            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path)}");
            }

            LastResult = "";

            if (!await PrepareRelationAsync(relationId))
            {
                return Page();
            }

            Suggestion.ExistingRelationId = Relation.Id;
            Suggestion.Action = Action == "Remove" ? PersonRelationSuggestionAction.Remove : PersonRelationSuggestionAction.Modify;

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                HttpResponseMessage response = await secureClient.PostAsync(
                    $"{APIRoot.Url}/api/people/relationeditsuggestion",
                    new StringContent(JsonConvert.SerializeObject(Suggestion), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    LastResult = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    FillSuggestionFromCurrentRelation();
                    return Page();
                }

                LastResult = $"پیشنهاد شما ثبت شد و پس از بررسی توسط مدیران اعمال خواهد شد. <a role=\"button\" href=\"/person/{Relation.Person1Id}\" class=\"actionlink\">بازگشت به صفحهٔ شخصیت</a>";

                FillSuggestionFromCurrentRelation();

                return Page();
            }
        }
    }
}
