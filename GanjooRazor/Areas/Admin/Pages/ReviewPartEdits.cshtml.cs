using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Collections.Generic;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ReviewPartEditsModel : PageModel
    {
        /// <summary>
        /// correction
        /// </summary>
        public GanjoorPoemSectionCorrectionViewModel Correction { get; set; }
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
        /// deleted user sections
        /// </summary>
        public bool DeletedUserSections { get; set; }

        public PoemRelatedImage TextSourceImage { get; set; }

        /// <summary>
        /// page
        /// </summary>
        public GanjoorPageCompleteViewModel PageInformation { get; set; }

        /// <summary>
        /// poem section
        /// </summary>
        public GanjoorPoemSection PoemSection { get; set; }

        /// <summary>
        /// verses
        /// </summary>
        public List<GanjoorVerseViewModel> Verses { get; set; }

        /// <summary>
        /// rhythms alphabetically
        /// </summary>
        public GanjoorMetre[] RhythmsAlphabetically { get; set; }

        /// <summary>
        /// rhythms by frequency
        /// </summary>
        public GanjoorMetre[] RhythmsByVerseCount { get; set; }

        private List<GanjoorVerseViewModel> _FilterSectionVerses(GanjoorPoemSection section, GanjoorVerseViewModel[] verses)
        {
            List<GanjoorVerseViewModel> sectionVerses = new List<GanjoorVerseViewModel>();
            foreach (GanjoorVerseViewModel verse in verses)
            {
                switch (section.VerseType)
                {
                    case VersePoemSectionType.First:
                        if (verse.SectionIndex1 == section.Index)
                            sectionVerses.Add(verse);
                        break;
                    case VersePoemSectionType.Second:
                        if (verse.SectionIndex2 == section.Index)
                            sectionVerses.Add(verse);
                        break;
                    case VersePoemSectionType.Third:
                        if (verse.SectionIndex3 == section.Index)
                            sectionVerses.Add(verse);
                        break;
                    default:
                        if (verse.SectionIndex4 == section.Index)
                            sectionVerses.Add(verse);
                        break;
                }
            }
            return sectionVerses;
        }


        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            FatalError = "";
            TotalCount = 0;
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            DeletedUserSections = string.IsNullOrEmpty(Request.Query["deletedUserSections"]) ? false : bool.Parse(Request.Query["deletedUserSections"]);
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var nextResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/section/correction/next?skip={Skip}&deletedUserSections={DeletedUserSections}");
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
                    Correction = JsonConvert.DeserializeObject<GanjoorPoemSectionCorrectionViewModel>(await nextResponse.Content.ReadAsStringAsync());
                    if (Correction != null)
                    {
                        var sectionResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/section/{Correction.SectionId}");
                        if (!sectionResponse.IsSuccessStatusCode)
                        {
                            FatalError = JsonConvert.DeserializeObject<string>(await sectionResponse.Content.ReadAsStringAsync());
                            return Page();
                        }
                        PoemSection = JsonConvert.DeserializeObject<GanjoorPoemSection>(await sectionResponse.Content.ReadAsStringAsync());

                        var pageUrlResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={PoemSection.PoemId}");
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

                        if (PageInformation.Poem.Images.Where(i => i.IsTextOriginalSource).Any())
                        {
                            TextSourceImage = PageInformation.Poem.Images.Where(i => i.IsTextOriginalSource).First();
                        }

                        if (Correction.Rhythm != null)
                        {
                            Correction.OriginalRhythm = PoemSection.GanjoorMetre == null ? null : PoemSection.GanjoorMetre.Rhythm;
                            if (Correction.OriginalRhythm == Correction.Rhythm)
                                Correction.RhythmResult = CorrectionReviewResult.NotChanged;
                        }

                        Verses = _FilterSectionVerses(PoemSection, PageInformation.Poem.Verses);
                    }

                    if (DeletedUserSections)
                    {
                        var rhythmResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/rhythms?sortOnVerseCount=true");
                        if (!rhythmResponse.IsSuccessStatusCode)
                        {
                            FatalError = JsonConvert.DeserializeObject<string>(await rhythmResponse.Content.ReadAsStringAsync());
                            return Page();
                        }

                        RhythmsByVerseCount = JsonConvert.DeserializeObject<GanjoorMetre[]>(await rhythmResponse.Content.ReadAsStringAsync());

                        List<GanjoorMetre> rhythmsByVerseCount = new List<GanjoorMetre>(RhythmsByVerseCount);
                        rhythmsByVerseCount.Sort((a, b) => a.Rhythm.CompareTo(b.Rhythm));
                        rhythmsByVerseCount.Insert(0, new GanjoorMetre()
                        {
                            Rhythm = "null"
                        }
                        );
                        rhythmsByVerseCount.Insert(0, new GanjoorMetre()
                        {
                            Rhythm = ""
                        }
                        );

                        RhythmsAlphabetically = rhythmsByVerseCount.ToArray();
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
            DeletedUserSections = string.IsNullOrEmpty(Request.Query["deletedUserSections"]) ? false : bool.Parse(Request.Query["deletedUserSections"]);
            if (Request.Form["next"].Count == 1)
            {
                return Redirect($"/Admin/ReviewPartEdits/?skip={Skip + 1}&deletedUserSections={DeletedUserSections}");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostSendCorrectionsModerationAsync(int correctionId,
            string rhythmReviewResult,
            string rhymeReviewResult,
            int[] breakFromVIndices,
            string titleReviewNote,
            string languageReviewResult, 
            string languageReviewNote,
            string reviewNote)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var correctionResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/section/correction/{correctionId}");
                    if (!correctionResponse.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await correctionResponse.Content.ReadAsStringAsync()));
                    }

                    Correction = JsonConvert.DeserializeObject<GanjoorPoemSectionCorrectionViewModel>(await correctionResponse.Content.ReadAsStringAsync());

                    int? breakFromVerse1VOrder = null;
                    int? breakFromVerse2VOrder = null;
                    int? breakFromVerse3VOrder = null;
                    int? breakFromVerse4VOrder = null;
                    int? breakFromVerse5VOrder = null;
                    int? breakFromVerse6VOrder = null;
                    int? breakFromVerse7VOrder = null;
                    int? breakFromVerse8VOrder = null;
                    int? breakFromVerse9VOrder = null;
                    int? breakFromVerse10VOrder = null;

                    if (breakFromVIndices.Length > 0)
                    {
                        var sectionResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/section/{Correction.SectionId}");
                        if (!sectionResponse.IsSuccessStatusCode)
                        {
                            FatalError = JsonConvert.DeserializeObject<string>(await sectionResponse.Content.ReadAsStringAsync());
                            return Page();
                        }
                        PoemSection = JsonConvert.DeserializeObject<GanjoorPoemSection>(await sectionResponse.Content.ReadAsStringAsync());

                        var pageUrlResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={PoemSection.PoemId}");
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

                        Verses = _FilterSectionVerses(PoemSection, PageInformation.Poem.Verses);



                        breakFromVerse1VOrder = Verses[breakFromVIndices[0]].VOrder;

                        if (breakFromVIndices.Length > 1)
                        {
                            breakFromVerse2VOrder = Verses[breakFromVIndices[1]].VOrder;
                        }

                        if (breakFromVIndices.Length > 2)
                        {
                            breakFromVerse3VOrder = Verses[breakFromVIndices[2]].VOrder;
                        }

                        if (breakFromVIndices.Length > 3)
                        {
                            breakFromVerse4VOrder = Verses[breakFromVIndices[3]].VOrder;
                        }

                        if (breakFromVIndices.Length > 4)
                        {
                            breakFromVerse5VOrder = Verses[breakFromVIndices[4]].VOrder;
                        }

                        if (breakFromVIndices.Length > 5)
                        {
                            breakFromVerse6VOrder = Verses[breakFromVIndices[5]].VOrder;
                        }

                        if (breakFromVIndices.Length > 6)
                        {
                            breakFromVerse7VOrder = Verses[breakFromVIndices[6]].VOrder;
                        }

                        if (breakFromVIndices.Length > 7)
                        {
                            breakFromVerse8VOrder = Verses[breakFromVIndices[7]].VOrder;
                        }

                        if (breakFromVIndices.Length > 8)
                        {
                            breakFromVerse9VOrder = Verses[breakFromVIndices[8]].VOrder;
                        }

                        if (breakFromVIndices.Length > 9)
                        {
                            breakFromVerse10VOrder = Verses[breakFromVIndices[9]].VOrder;
                        }
                    }


                    if (Correction.Rhythm != null)
                    {
                        if (rhythmReviewResult == null)
                        {
                            return new BadRequestObjectResult("لطفا تغییر وزن را بازبینی کنید.");
                        }
                        else
                        {
                            Correction.RhythmResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), rhythmReviewResult);
                            Correction.ReviewNote = titleReviewNote;
                        }
                    }

                    if (Correction.RhymeLetters != null)
                    {
                        if (rhymeReviewResult == null)
                        {
                            return new BadRequestObjectResult("لطفا تغییر قافیه را بازبینی کنید.");
                        }
                        else
                        {
                            Correction.RhymeLettersReviewResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), rhymeReviewResult);
                            Correction.ReviewNote = titleReviewNote;
                        }
                    }

                    if(Correction.Language != null)
                    {
                        if(languageReviewResult == null)
                        {
                            return new BadRequestObjectResult("لطفا تغییر زبان را بازبینی کنید.");
                        }
                        else
                        {
                            Correction.LanguageReviewResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), languageReviewResult);
                            Correction.ReviewNote = languageReviewNote;
                        }
                    }

                    Correction.BreakFromVerse1VOrder = breakFromVerse1VOrder;
                    Correction.BreakFromVerse2VOrder = breakFromVerse2VOrder;
                    Correction.BreakFromVerse3VOrder = breakFromVerse3VOrder;
                    Correction.BreakFromVerse4VOrder = breakFromVerse4VOrder;
                    Correction.BreakFromVerse5VOrder = breakFromVerse5VOrder;
                    Correction.BreakFromVerse6VOrder = breakFromVerse6VOrder;
                    Correction.BreakFromVerse7VOrder = breakFromVerse7VOrder;
                    Correction.BreakFromVerse8VOrder = breakFromVerse8VOrder;
                    Correction.BreakFromVerse9VOrder = breakFromVerse9VOrder;
                    Correction.BreakFromVerse10VOrder = breakFromVerse10VOrder;

                    Correction.ReviewNote = reviewNote;

                    


                    var moderationResponse = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/section/moderate",
                        new StringContent(JsonConvert.SerializeObject(Correction), Encoding.UTF8, "application/json"
                        ));

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
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }


        public async Task<IActionResult> OnPostSuggestRhythmAsync(int sectionId, int correctionId, string rhythm)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var correctionResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/section/correction/{correctionId}");
                    if (!correctionResponse.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await correctionResponse.Content.ReadAsStringAsync()));
                    }
                    Correction = JsonConvert.DeserializeObject<GanjoorPoemSectionCorrectionViewModel>(await correctionResponse.Content.ReadAsStringAsync());
                    Correction.RhythmResult = CorrectionReviewResult.Rejected;
                    var moderationResponse = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/section/moderate",
                        new StringContent(JsonConvert.SerializeObject(Correction), Encoding.UTF8, "application/json"
                        ));
                    if (!moderationResponse.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await moderationResponse.Content.ReadAsStringAsync()));
                    }

                    if(!string.IsNullOrEmpty(rhythm))
                    {
                        GanjoorPoemSectionCorrectionViewModel correction = new GanjoorPoemSectionCorrectionViewModel()
                        {
                            SectionId = sectionId,
                            Rhythm = rhythm,
                        };

                        HttpResponseMessage response = await secureClient.PostAsync(
                            $"{APIRoot.Url}/api/ganjoor/section/correction",
                            new StringContent(JsonConvert.SerializeObject(correction),
                            Encoding.UTF8,
                            "application/json"));
                        if (!response.IsSuccessStatusCode)
                        {
                            return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                        }
                    }

                    return new OkObjectResult(true);
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }
    }
}
