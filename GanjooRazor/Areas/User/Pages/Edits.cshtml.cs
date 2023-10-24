using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using GanjooRazor.Utils;
using GSpotifyProxy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Auth.ViewModel;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class EditsModel : PageModel
    {
        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// pagination links
        /// </summary>
        public List<NameIdUrlImage> PaginationLinks { get; set; }

        /// <summary>
        /// can edit
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// all users edits
        /// </summary>
        public bool AllUsersEdits { get; set; }

        /// <summary>
        /// filteredUserId
        /// </summary>
        public Guid? filteredUserId { get; set; }

        public GanjoorUserPublicProfile Profile { get; set; }

        /// <summary>
        /// Corrections
        /// </summary>
        public List<GanjoorPoemCorrectionViewModel> Corrections { get; set; }

        public GanjoorLanguage[] Languages { get; set; }

        private async Task ReadLanguagesAsync(HttpClient secureClient)
        {
            HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/translations/languages");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return;
            }

            Languages = JsonConvert.DeserializeObject<GanjoorLanguage[]>(await response.Content.ReadAsStringAsync());
        }
        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");
            CanEdit = Request.Cookies["CanEdit"] == "True";
            AllUsersEdits = false;
            LastError = "";
            using (HttpClient secureClient = new HttpClient())
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    {
                        int pageNumber = 1;
                        if (!string.IsNullOrEmpty(Request.Query["page"]))
                        {
                            pageNumber = int.Parse(Request.Query["page"]);
                        }
                        string url = $"{APIRoot.Url}/api/ganjoor/corrections/mine?PageNumber={pageNumber}&PageSize=20";
                        if (CanEdit)
                        {
                            AllUsersEdits = Request.Query["AllUsers"] == "1";
                            if (AllUsersEdits)
                            {
                                url = $"{APIRoot.Url}/api/ganjoor/corrections/all?PageNumber={pageNumber}&PageSize=20";
                            }
                            filteredUserId = string.IsNullOrEmpty(Request.Query["UserId"]) ? null : Guid.Parse(Request.Query["UserId"]);
                            if (filteredUserId != null)
                            {
                                url += $"&userId={filteredUserId}";
                                var responseUserProfile = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/user/profile/{filteredUserId}");
                                if (!responseUserProfile.IsSuccessStatusCode)
                                {
                                    LastError = JsonConvert.DeserializeObject<string>(await responseUserProfile.Content.ReadAsStringAsync());
                                    return Page();
                                }

                                Profile = JsonConvert.DeserializeObject<GanjoorUserPublicProfile>(await responseUserProfile.Content.ReadAsStringAsync());
                            }
                        }
                        var response = await secureClient.GetAsync(url);
                        if (!response.IsSuccessStatusCode)
                        {
                            LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                            return Page();
                        }

                        Corrections = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCorrectionViewModel>>();

                        string paginnationMetadata = response.Headers.GetValues("paging-headers").FirstOrDefault();
                        if (!string.IsNullOrEmpty(paginnationMetadata))
                        {
                            PaginationMetadata paginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata);
                            PaginationLinks = new List<NameIdUrlImage>();
                            if (paginationMetadata.totalPages > 1)
                            {
                                if (paginationMetadata.currentPage > 3)
                                {
                                    PaginationLinks.Add
                                        (
                                        new NameIdUrlImage()
                                        {
                                            Name = "صفحهٔ اول",
                                            Url = AllUsersEdits ? "/User/Edits/?page=1&AllUsers=1" : "/User/Edits/?page=1"
                                        }
                                        );
                                }
                                for (int i = (paginationMetadata.currentPage - 2); i <= (paginationMetadata.currentPage + 2); i++)
                                {
                                    if (i >= 1 && i <= paginationMetadata.totalPages)
                                    {
                                        if (i == paginationMetadata.currentPage)
                                        {

                                            PaginationLinks.Add
                                               (
                                               new NameIdUrlImage()
                                               {
                                                   Name = i.ToPersianNumbers(),
                                               }
                                               );
                                        }
                                        else
                                        {

                                            PaginationLinks.Add
                                                (
                                                new NameIdUrlImage()
                                                {
                                                    Name = i.ToPersianNumbers(),
                                                    Url = AllUsersEdits ? $"/User/Edits/?page={i}&AllUsers=1" : $"/User/Edits/?page={i}"
                                                }
                                                );
                                        }
                                    }
                                }
                                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                                {

                                    PaginationLinks.Add
                                        (
                                        new NameIdUrlImage()
                                        {
                                            Name = "... ",
                                        }
                                        );

                                    PaginationLinks.Add
                                       (
                                       new NameIdUrlImage()
                                       {
                                           Name = "صفحهٔ آخر",
                                           Url = AllUsersEdits ? $"/User/Edits/?page={paginationMetadata.totalPages}&AllUsers=1" : $"/User/Edits/?page={paginationMetadata.totalPages}"
                                       }
                                       );
                                }
                            }
                        }
                        await ReadLanguagesAsync(secureClient);
                    }
                }
                else
                {
                    LastError = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            return Page();
        }

        public async Task<IActionResult> OnPostRollBackCorrectionAsync(int correctionId)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var correctionResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/correction/{correctionId}");
                    if (!correctionResponse.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await correctionResponse.Content.ReadAsStringAsync()));
                    }

                    var currentCorrection = JsonConvert.DeserializeObject<GanjoorPoemCorrectionViewModel>(await correctionResponse.Content.ReadAsStringAsync());
                    GanjoorPoemCorrectionViewModel correction = new GanjoorPoemCorrectionViewModel();
                    correction.PoemId = currentCorrection.PoemId;
                    if (currentCorrection.Title != null && currentCorrection.Result == CorrectionReviewResult.Approved)
                    {
                        correction.OriginalTitle = currentCorrection.Title;
                        correction.Title = currentCorrection.OriginalTitle;
                    }

                    List<GanjoorVerseVOrderText> vOrderTexts = new List<GanjoorVerseVOrderText>();
                    if (currentCorrection.VerseOrderText != null)
                    {

                        foreach (var verseOrderText in currentCorrection.VerseOrderText)
                        {
                            if (
                                verseOrderText.Result == CorrectionReviewResult.Approved
                                ||
                                verseOrderText.VersePositionResult == CorrectionReviewResult.Approved
                                ||
                                verseOrderText.LanguageReviewResult == CorrectionReviewResult.Approved
                                ||
                                verseOrderText.SummaryReviewResult == CorrectionReviewResult.Approved
                                )
                            {
                                vOrderTexts.Add(
                               new GanjoorVerseVOrderText()
                               {
                                   VORder = verseOrderText.VORder,
                                   OriginalText = verseOrderText.Text,
                                   Text = verseOrderText.OriginalText,
                                   VersePosition = verseOrderText.VersePositionResult == CorrectionReviewResult.Approved && verseOrderText.VersePosition != null ? verseOrderText.OriginalVersePosition : null,
                                   OriginalVersePosition = verseOrderText.VersePositionResult == CorrectionReviewResult.Approved && verseOrderText.VersePosition != null ? verseOrderText.VersePosition : null,
                                   LanguageId = verseOrderText.LanguageReviewResult == CorrectionReviewResult.Approved && verseOrderText.LanguageId != null ? verseOrderText.OriginalLanguageId : null,
                                   OriginalLanguageId = verseOrderText.LanguageReviewResult == CorrectionReviewResult.Approved && verseOrderText.LanguageId != null ? verseOrderText.LanguageId : null,
                                   CoupletIndex = verseOrderText.CoupletIndex,
                                   CoupletSummary = verseOrderText.SummaryReviewResult == CorrectionReviewResult.Approved && verseOrderText.CoupletSummary != null ? verseOrderText.OriginalCoupletSummary : null,
                                   OriginalCoupletSummary = verseOrderText.SummaryReviewResult == CorrectionReviewResult.Approved && verseOrderText.CoupletSummary != null ? verseOrderText.CoupletSummary : null,

                               });
                            }

                        }

                    }
                    correction.VerseOrderText = vOrderTexts.ToArray();
                    correction.Rhythm = currentCorrection.RhythmResult == CorrectionReviewResult.Approved && currentCorrection.Rhythm != null ? currentCorrection.OriginalRhythm : null;
                    correction.OriginalRhythm = currentCorrection.RhythmResult == CorrectionReviewResult.Approved && currentCorrection.Rhythm != null ? currentCorrection.Rhythm : null;

                    correction.Rhythm2 = currentCorrection.Rhythm2Result == CorrectionReviewResult.Approved && currentCorrection.Rhythm2 != null ? currentCorrection.OriginalRhythm2 : null;
                    correction.OriginalRhythm2 = currentCorrection.Rhythm2Result == CorrectionReviewResult.Approved && currentCorrection.Rhythm2 != null ? currentCorrection.Rhythm2 : null;

                    correction.Rhythm3 = currentCorrection.Rhythm3Result == CorrectionReviewResult.Approved && currentCorrection.Rhythm3 != null ? currentCorrection.OriginalRhythm3 : null;
                    correction.OriginalRhythm3 = currentCorrection.Rhythm3Result == CorrectionReviewResult.Approved && currentCorrection.Rhythm3 != null ? currentCorrection.Rhythm3 : null;

                    correction.Rhythm4 = currentCorrection.Rhythm4Result == CorrectionReviewResult.Approved && currentCorrection.Rhythm4 != null ? currentCorrection.OriginalRhythm4 : null;
                    correction.OriginalRhythm4 = currentCorrection.Rhythm4Result == CorrectionReviewResult.Approved && currentCorrection.Rhythm4 != null ? currentCorrection.Rhythm4 : null;


                    HttpResponseMessage response = await secureClient.PostAsync(
                        $"{APIRoot.Url}/api/ganjoor/poem/correction",
                        new StringContent(JsonConvert.SerializeObject(correction),
                        Encoding.UTF8,
                        "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }


                    return new OkObjectResult(true);
                }
            }
            return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
        }
    }
}
