using System;
using System.Collections.Generic;
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
    /// contributor-facing form for suggesting an edit to an already-approved GanjoorRelatedPerson.
    /// Any logged-in user may submit here (same bar as suggesting a poem correction); nothing is
    /// changed until a moderator approves it via Admin/ReviewPersonEdits. Reachable both from a
    /// person's PersonWindow.open() modal and from the poem-correction moderation page
    /// (ReviewEdits.cshtml, when a correction links to an existing person).
    /// </summary>
    public class SuggestPersonEditModel : LoginPartialEnabledPageModel
    {
        public SuggestPersonEditModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public string LastError { get; set; }

        public string LastResult { get; set; }

        public GanjoorRelatedPerson Person { get; set; }

        [BindProperty]
        public GanjoorPersonEditSuggestion Suggestion { get; set; }

        public List<GanjoorGeoLocation> Locations { get; set; }

        private async Task ReadLocationsAsync()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/locations");
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return;
            }

            Locations = new List<GanjoorGeoLocation>
            {
                new GanjoorGeoLocation() { Id = 0, Latitude = 0, Longitude = 0, Name = "" }
            };
            Locations.AddRange(JsonConvert.DeserializeObject<GanjoorGeoLocation[]>(await response.Content.ReadAsStringAsync()));
        }

        private async Task<bool> PreparePersonAsync(int id)
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
                LastError = "شخصیتی با این کد پیدا نشد.";
                return false;
            }

            return true;
        }

        private void FillSuggestionFromCurrentPerson()
        {
            // the form is pre-filled with the person's CURRENT values, so submitting without
            // changing anything is a no-op suggestion (harmless, just not useful) rather than
            // accidentally blanking fields the contributor didn't mean to touch
            Suggestion = new GanjoorPersonEditSuggestion()
            {
                PersonId = Person.Id,
                SuggestedName = Person.Name,
                SuggestedDescription = Person.Description,
                SuggestedWikiUrl = Person.WikiUrl,
                SuggestedBirthYearInLHijri = Person.BirthYearInLHijri,
                SuggestedDeathYearInLHijri = Person.DeathYearInLHijri,
                SuggestedValidBirthDate = Person.ValidBirthDate,
                SuggestedValidDeathDate = Person.ValidDeathDate,
                SuggestedBirthLocationId = Person.BirthLocationId,
                SuggestedDeathLocationId = Person.DeathLocationId,
                SuggestedFamilyTreeCaption = Person.FamilyTreeCaption,
            };
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            InitializeCommonPageState();

            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path + Request.QueryString)}");
            }

            await ReadLocationsAsync();

            if (!await PreparePersonAsync(id))
            {
                return Page();
            }

            FillSuggestionFromCurrentPerson();

            return Page();
        }

        public async Task<IActionResult> OnPostSuggestEditAsync(int id)
        {
            InitializeCommonPageState();

            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path + Request.QueryString)}");
            }

            LastResult = "";

            await ReadLocationsAsync();

            if (!await PreparePersonAsync(id))
            {
                return Page();
            }

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                HttpResponseMessage response = await secureClient.PostAsync(
                    $"{APIRoot.Url}/api/people/{id}/editsuggestion",
                    new StringContent(JsonConvert.SerializeObject(Suggestion), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    LastResult = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    FillSuggestionFromCurrentPerson();
                    return Page();
                }

                LastResult = $"پیشنهاد ویرایش شما ثبت شد و پس از بررسی توسط مدیران اعمال خواهد شد. <a role=\"button\" href=\"javascript:void(0)\" onclick=\"PersonWindow.open({id})\" class=\"actionlink\">مشاهدهٔ اطلاعات این شخصیت</a>";

                FillSuggestionFromCurrentPerson();

                return Page();
            }
        }
    }
}
