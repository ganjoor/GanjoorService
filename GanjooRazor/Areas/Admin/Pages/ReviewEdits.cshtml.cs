using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Models;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ReviewEditsModel : PageModel
    {
        /// <summary>
        /// correction
        /// </summary>
        public GanjoorPoemCorrectionViewModel Correction { get; set; }
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

        public PoemRelatedImage TextSourceImage { get; set; }

        public bool OnlyUserCorrections { get; set; }

        /// <summary>
        /// page
        /// </summary>
        public GanjoorPageCompleteViewModel PageInformation { get; set; }

        public GanjoorMetre GanjoorMetre1 { get; set; }

        public GanjoorMetre GanjoorMetre2 { get; set; }

        public string RhymeLetters { get; set; }

        public GanjoorLanguage[] Languages { get; set; }

        public bool ApproveVersePositionChanges { get; set; }

        /// <summary>
        /// couplets of the poem, used to show which couplet a suggested geo/date tag belongs to
        /// </summary>
        public Tuple<int, string>[] Couplets { get; set; }

        /// <summary>
        /// full location catalog, offered as an alternative to a user's new-location suggestion so the
        /// moderator can correct a mistyped/duplicate name by linking it to the existing entry instead
        /// </summary>
        public List<GanjoorGeoLocation> Locations { get; set; }

        /// <summary>
        /// groups verses into couplets - same logic as SuggestQuoted.cshtml.cs's/Editor.cshtml.cs's GetCouplets,
        /// duplicated here rather than shared, so this page doesn't take on a cross-file dependency on those
        /// </summary>
        /// <param name="verses"></param>
        /// <returns></returns>
        private Tuple<int, string>[] GetCouplets(GanjoorVerseViewModel[] verses)
        {
            int coupetIndex = -1;
            string coupletText = "";
            var couplets = new System.Collections.Generic.List<Tuple<int, string>>();
            int verseIndex = 0;
            while (verseIndex < verses.Length)
            {
                switch (verses[verseIndex].VersePosition)
                {
                    case VersePosition.Comment:
                        break;
                    case VersePosition.Paragraph:
                    case VersePosition.Single:
                        if (!string.IsNullOrEmpty(coupletText))
                        {
                            couplets.Add(new Tuple<int, string>(coupetIndex, coupletText));
                            coupletText = "";
                        }
                        coupetIndex++;
                        couplets.Add(new Tuple<int, string>(coupetIndex, verses[verseIndex].Text));
                        break;
                    case VersePosition.Right:
                    case VersePosition.CenteredVerse1:
                        if (!string.IsNullOrEmpty(coupletText))
                        {
                            couplets.Add(new Tuple<int, string>(coupetIndex, coupletText));
                        }
                        coupetIndex++;
                        coupletText = verses[verseIndex].Text;
                        break;
                    case VersePosition.Left:
                    case VersePosition.CenteredVerse2:
                        coupletText += $" - {verses[verseIndex].Text}";
                        break;
                }
                verseIndex++;
            }
            if (!string.IsNullOrEmpty(coupletText))
            {
                couplets.Add(new Tuple<int, string>(coupetIndex, coupletText));
            }
            return couplets.ToArray();
        }

        private async Task ReadLanguagesAsync(HttpClient secureClient)
        {
            HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/translations/languages");
            if (!response.IsSuccessStatusCode)
            {
                FatalError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return;
            }

            Languages = JsonConvert.DeserializeObject<GanjoorLanguage[]>(await response.Content.ReadAsStringAsync());
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            FatalError = "";
            TotalCount = 0;
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            OnlyUserCorrections = string.IsNullOrEmpty(Request.Query["onlyUserCorrections"]) ? true : bool.Parse(Request.Query["onlyUserCorrections"]);
            ApproveVersePositionChanges = string.IsNullOrEmpty(Request.Query["posok"]) ? false : bool.Parse(Request.Query["posok"]);
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var nextResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/correction/next?skip={Skip}&onlyUserCorrections={OnlyUserCorrections}");
                    if (!nextResponse.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await nextResponse.Content.ReadAsStringAsync());
                        return Page();
                    }

                    string paginnationMetadata = nextResponse.Headers.GetValues("paging-headers").FirstOrDefault();
                    if (!string.IsNullOrEmpty(paginnationMetadata))
                    {
                        TotalCount = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata).totalCount;
                    }

                    await ReadLanguagesAsync(secureClient);


                    Correction = JsonConvert.DeserializeObject<GanjoorPoemCorrectionViewModel>(await nextResponse.Content.ReadAsStringAsync());
                    if(Correction != null)
                    {
                        var pageUrlResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={Correction.PoemId}");
                        if (!pageUrlResponse.IsSuccessStatusCode)
                        {
                            FatalError = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                            return Page();
                        }
                        var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

                        var pageQuery = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
                        if (!pageQuery.IsSuccessStatusCode)
                        {
                            FatalError = JsonConvert.DeserializeObject<string>(await pageQuery.Content.ReadAsStringAsync());
                            return Page();
                        }
                        PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

                        Couplets = GetCouplets(PageInformation.Poem.Verses);

                        if (Correction.GeoDateTags != null && Correction.GeoDateTags.Any())
                        {
                            var responseLocations = await secureClient.GetAsync($"{APIRoot.Url}/api/locations");
                            if (!responseLocations.IsSuccessStatusCode)
                            {
                                FatalError = JsonConvert.DeserializeObject<string>(await responseLocations.Content.ReadAsStringAsync());
                                return Page();
                            }
                            Locations = new List<GanjoorGeoLocation>();
                            Locations.Add(new GanjoorGeoLocation() { Id = 0, Latitude = 0, Longitude = 0, Name = "" });
                            Locations.AddRange(JsonConvert.DeserializeObject<GanjoorGeoLocation[]>(await responseLocations.Content.ReadAsStringAsync()));
                        }

                        if (PageInformation.Poem.Sections.Where(s => s.SectionType == PoemSectionType.WholePoem && !string.IsNullOrEmpty(s.RhymeLetters)).Any())
                        {
                            RhymeLetters = PageInformation.Poem.Sections.Where(s => s.SectionType == PoemSectionType.WholePoem && !string.IsNullOrEmpty(s.RhymeLetters)).OrderBy(s => s.VerseType).First().RhymeLetters;
                        }

                        if (PageInformation.Poem.Sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.GanjoorMetre != null).Any())
                        {
                            GanjoorMetre1 = PageInformation.Poem.Sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.GanjoorMetre != null).OrderBy(s => s.VerseType).First().GanjoorMetre;
                            if (PageInformation.Poem.Sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.GanjoorMetre != null).Count() > 1)
                            {
                                GanjoorMetre2 = PageInformation.Poem.Sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.GanjoorMetre != null).OrderBy(s => s.VerseType).ToList()[1].GanjoorMetre;
                            }
                        }

                        if (PageInformation.Poem.Images.Where(i => i.IsTextOriginalSource).Any())
                        {
                            TextSourceImage = PageInformation.Poem.Images.Where(i => i.IsTextOriginalSource).First();
                        }

                        if (Correction.Title != null)
                        {
                            Correction.OriginalTitle = PageInformation.Poem.Title;
                            if (Correction.OriginalTitle == Correction.Title)
                                Correction.Result = CorrectionReviewResult.NotChanged;
                        }

                        if (Correction.VerseOrderText != null)
                            foreach (var verse in Correction.VerseOrderText)
                            {
                                
                                var v = PageInformation.Poem.Verses.Where(v => v.VOrder == verse.VORder).SingleOrDefault();
                                if (v != null)
                                {
                                    if (!verse.NewVerse)
                                    {
                                        verse.OriginalText = v.Text;
                                        verse.OriginalCoupletSummary = v.CoupletSummary;
                                        verse.OriginalLanguageId = v.LanguageId;
                                        verse.OriginalVersePosition = v.VersePosition;
                                        verse.CoupletIndex = v.CoupletIndex;
                                        if(string.IsNullOrEmpty(verse.Text))
                                        {
                                            verse.Text = v.Text;
                                        }
                                        if(!string.IsNullOrEmpty(verse.CoupletSummary))
                                        {
                                            if(verse.CoupletSummary == verse.OriginalCoupletSummary)
                                            {
                                                verse.SummaryReviewResult = CorrectionReviewResult.NotChanged;
                                            }
                                        }
                                        if (verse.OriginalText == verse.Text)
                                            verse.Result = CorrectionReviewResult.NotChanged;
                                    }
                                    else
                                    {
                                        verse.OriginalText = "";
                                    }
                                    
                                }
                            }

                        if (Correction.Rhythm != null)
                        {
                            GanjoorMetre originalMetre = null;
                            if (PageInformation.Poem.Sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.GanjoorMetre != null && s.VerseType == VersePoemSectionType.First).Any())
                            {
                                originalMetre = PageInformation.Poem.Sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First && s.GanjoorMetre != null).OrderBy(s => s.VerseType).First().GanjoorMetre;
                            }
                            Correction.OriginalRhythm = originalMetre == null ? null : originalMetre.Rhythm;
                            if (Correction.OriginalRhythm == Correction.Rhythm)
                                Correction.RhythmResult = CorrectionReviewResult.NotChanged;
                        }

                        if (Correction.Rhythm2 != null)
                        {
                            GanjoorMetre originalMetre2 = null;
                            if (PageInformation.Poem.Sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.GanjoorMetre != null && s.VerseType == VersePoemSectionType.Second).Any())
                            {
                                originalMetre2 = PageInformation.Poem.Sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.GanjoorMetre != null &&  s.VerseType == VersePoemSectionType.Second).OrderBy(s => s.VerseType).First().GanjoorMetre;
                            }
                            Correction.OriginalRhythm2 = originalMetre2 == null ? null : originalMetre2.Rhythm;
                            if (Correction.OriginalRhythm2 == Correction.Rhythm2)
                                Correction.Rhythm2Result = CorrectionReviewResult.NotChanged;
                        }
                    }
                    
                }
                else
                {
                    FatalError = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            OnlyUserCorrections = string.IsNullOrEmpty(Request.Query["onlyUserCorrections"]) ? true : bool.Parse(Request.Query["onlyUserCorrections"]);
            if (Request.Form["next"].Count == 1)
            {
                return Redirect($"/Admin/ReviewEdits/?skip={Skip + 1}&onlyUserCorrections={OnlyUserCorrections}");
            }
            return Page();
        }

        

        public async Task<IActionResult> OnPostSendCorrectionsModerationAsync([FromBody] PoemMoerationStructure pms)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var correctionResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/correction/{pms.correctionId}");
                    if (!correctionResponse.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await correctionResponse.Content.ReadAsStringAsync()));
                    }

                    Correction = JsonConvert.DeserializeObject<GanjoorPoemCorrectionViewModel>(await correctionResponse.Content.ReadAsStringAsync());

                    if (Correction.Title != null)
                    {
                        if(pms.titleReviewResult == null)
                        {
                            return new BadRequestObjectResult("لطفاً تغییر عنوان را بازبینی کنید.");
                        }
                        else
                        {
                            Correction.Result = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.titleReviewResult);
                            Correction.ReviewNote = pms.titleReviewNote;
                        }
                    }

                    if (Correction.PoemSummary != null)
                    {
                        if (pms.summaryReviewResult == null)
                        {
                            return new BadRequestObjectResult("لطفاً تغییر خلاصه را بازبینی کنید.");
                        }
                        else
                        {
                            Correction.SummaryReviewResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.summaryReviewResult);
                            Correction.ReviewNote = pms.titleReviewNote;
                        }
                    }

                    if (Correction.Rhythm != null)
                    {
                        if(pms.rhythmReviewResult == null)
                        {
                            return new BadRequestObjectResult("لطفاً تغییر وزن را بازبینی کنید.");
                        }
                        else
                        {
                            Correction.RhythmResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.rhythmReviewResult);
                            Correction.ReviewNote = pms.titleReviewNote;
                        }
                    }

                    if (Correction.Rhythm2 != null)
                    {
                        if (pms.rhythm2ReviewResult == null)
                        {
                            return new BadRequestObjectResult("لطفاً تغییر وزن دوم را بازبینی کنید.");
                        }
                        else
                        {
                            Correction.Rhythm2Result = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.rhythm2ReviewResult);
                            Correction.ReviewNote = pms.titleReviewNote;
                        }
                    }

                    if (Correction.RhymeLetters != null)
                    {
                        if (pms.rhymeReviewResult == null)
                        {
                            return new BadRequestObjectResult("لطفا تغییر قافیه را بازبینی کنید.");
                        }
                        else
                        {
                            Correction.RhymeLettersReviewResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.rhymeReviewResult);
                            Correction.ReviewNote = pms.titleReviewNote;
                        }
                    }

                    if (Correction.PoemFormat != null)
                    {
                        if (pms.poemformatReviewResult == null)
                        {
                            return new BadRequestObjectResult("لطفاً تغییر قالب شعری را بازبینی کنید.");
                        }
                        else
                        {
                            Correction.PoemFormatReviewResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.poemformatReviewResult);
                            Correction.ReviewNote = pms.poemformatReviewNote;
                        }
                    }

                    if (Correction.GeoDateTags != null && Correction.GeoDateTags.Length > 0)
                    {
                        if (pms.geoTagReviewResult == null || pms.geoTagReviewResult.Length != Correction.GeoDateTags.Length)
                        {
                            return new BadRequestObjectResult("لطفاً تکلیف بررسی تمام برچسب‌های جغرافیایی/تاریخی پیشنهادی را مشخص کنید.");
                        }
                        else
                        {
                            for (int i = 0; i < Correction.GeoDateTags.Length; i++)
                            {
                                if (pms.geoTagReviewResult[i] == null)
                                {
                                    return new BadRequestObjectResult("لطفاً تکلیف بررسی تمام برچسب‌های جغرافیایی/تاریخی پیشنهادی را مشخص کنید.");
                                }

                                if (!Correction.GeoDateTags[i].MarkForDelete)
                                {
                                    // let the moderator fix a mistyped location name / wrong coordinates, or link to an
                                    // existing catalog location, before the tag is approved - rather than only being able
                                    // to approve the user's suggestion as-is or reject it outright
                                    string correctedLocationIdText = (pms.geoTagLocationId != null && i < pms.geoTagLocationId.Length) ? pms.geoTagLocationId[i] : null;
                                    if (!string.IsNullOrWhiteSpace(correctedLocationIdText) && int.TryParse(correctedLocationIdText, out int correctedLocationId) && correctedLocationId > 0)
                                    {
                                        Correction.GeoDateTags[i].LocationId = correctedLocationId;
                                        Correction.GeoDateTags[i].SuggestedLocationName = null;
                                        Correction.GeoDateTags[i].SuggestedLatitude = null;
                                        Correction.GeoDateTags[i].SuggestedLongitude = null;
                                    }
                                    else if (Correction.GeoDateTags[i].LocationId == null)
                                    {
                                        string correctedName = (pms.geoTagLocationName != null && i < pms.geoTagLocationName.Length) ? pms.geoTagLocationName[i] : null;
                                        string correctedLatText = (pms.geoTagLatitude != null && i < pms.geoTagLatitude.Length) ? pms.geoTagLatitude[i] : null;
                                        string correctedLngText = (pms.geoTagLongitude != null && i < pms.geoTagLongitude.Length) ? pms.geoTagLongitude[i] : null;
                                        if (!string.IsNullOrWhiteSpace(correctedName))
                                        {
                                            Correction.GeoDateTags[i].SuggestedLocationName = correctedName.Trim();
                                        }
                                        if (!string.IsNullOrWhiteSpace(correctedLatText) && !string.IsNullOrWhiteSpace(correctedLngText))
                                        {
                                            if (!double.TryParse(correctedLatText, NumberStyles.Float, CultureInfo.InvariantCulture, out double correctedLat)
                                                || !double.TryParse(correctedLngText, NumberStyles.Float, CultureInfo.InvariantCulture, out double correctedLng)
                                                || correctedLat < -90 || correctedLat > 90 || correctedLng < -180 || correctedLng > 180)
                                            {
                                                return new BadRequestObjectResult("مقدار اصلاح‌شدهٔ عرض/طول جغرافیایی نامعتبر است.");
                                            }
                                            Correction.GeoDateTags[i].SuggestedLatitude = correctedLat;
                                            Correction.GeoDateTags[i].SuggestedLongitude = correctedLng;
                                        }
                                    }
                                }

                                Correction.GeoDateTags[i].Result = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.geoTagReviewResult[i]);
                                Correction.GeoDateTags[i].ReviewNote = (pms.geoTagReviewNotes != null && i < pms.geoTagReviewNotes.Length) ? pms.geoTagReviewNotes[i] : null;
                            }
                        }
                    }

                    if (pms.verseReviewResult.Length != Correction.VerseOrderText.Length)
                    {
                        return new BadRequestObjectResult("لطفاً تکلیف بررسی تمام مصرعهای پیشنهادی را مشخص کنید.");
                    }
                    else
                    {
                        for (int i = 0; i < Correction.VerseOrderText.Length; i++)
                        {
                            if (Correction.VerseOrderText[i].MarkForDelete)
                            {
                                Correction.VerseOrderText[i].MarkForDeleteResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.verseReviewResult[i]);
                            }
                            else
                            if (Correction.VerseOrderText[i].NewVerse)
                            {
                                Correction.VerseOrderText[i].NewVerseResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.verseReviewResult[i]);
                            }
                            else
                            {
                                Correction.VerseOrderText[i].Result = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.verseReviewResult[i]);
                                if (Correction.VerseOrderText[i].VersePosition != null)
                                {
                                    if( i >= pms.versePosReviewResult.Length)
                                    {
                                        if (Correction.VerseOrderText[i].Result != CorrectionReviewResult.Approved)
                                        {
                                            Correction.VerseOrderText[i].VersePositionResult = CorrectionReviewResult.NotSuggestedByUser;
                                        }
                                        else
                                        {
                                            Correction.VerseOrderText[i].VersePositionResult = CorrectionReviewResult.Approved;
                                        }
                                    }
                                    else
                                    if (pms.versePosReviewResult[i] == null)
                                    {
                                        return new BadRequestObjectResult("لطفاً تکلیف بررسی تمام مصرعهای پیشنهادی را مشخص کنید.");
                                    }
                                    else
                                    {
                                        Correction.VerseOrderText[i].VersePositionResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.versePosReviewResult[i]);
                                    }
                                    
                                }

                                if (Correction.VerseOrderText[i].LanguageId != null)
                                {
                                    if (i >= pms.verseLanguageReviewResult.Length)
                                    {
                                        if (Correction.VerseOrderText[i].Result != CorrectionReviewResult.Approved)
                                        {
                                            Correction.VerseOrderText[i].LanguageReviewResult = CorrectionReviewResult.NotSuggestedByUser;
                                        }
                                        else
                                        {
                                            Correction.VerseOrderText[i].LanguageReviewResult = CorrectionReviewResult.Approved;
                                        }
                                    }
                                    else
                                    if (pms.verseLanguageReviewResult[i] == null)
                                    {
                                        return new BadRequestObjectResult("لطفاً تکلیف بررسی تمام مصرعهای پیشنهادی را مشخص کنید.");
                                    }
                                    else
                                    {
                                        Correction.VerseOrderText[i].LanguageReviewResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.verseLanguageReviewResult[i]);
                                    }

                                }

                                if (Correction.VerseOrderText[i].CoupletSummary != null)
                                {
                                    if(pms.verseSummaryResults.Length <= i)
                                    {
                                        Correction.VerseOrderText[i].SummaryReviewResult = CorrectionReviewResult.Rejected;
                                    }
                                    else
                                    if (pms.verseSummaryResults[i] == null)
                                    {
                                        return new BadRequestObjectResult("لطفاً تکلیف بررسی تمام مصرعهای پیشنهادی را مشخص کنید.");
                                    }
                                    else
                                    {
                                        Correction.VerseOrderText[i].SummaryReviewResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.verseSummaryResults[i]);
                                    }

                                }
                            }
                            Correction.VerseOrderText[i].ReviewNote = pms.verseReviewNotes[i];
                        }
                    }

                    var moderationResponse = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/correction/moderate",
                        new StringContent(JsonConvert.SerializeObject(Correction), Encoding.UTF8, "application/json"
                        ));

                    if(!moderationResponse.IsSuccessStatusCode)
                    {
                        string err = await moderationResponse.Content.ReadAsStringAsync();
                        if(string.IsNullOrEmpty(err)) 
                        { 
                            if(!string.IsNullOrEmpty(moderationResponse.ReasonPhrase))
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
}
