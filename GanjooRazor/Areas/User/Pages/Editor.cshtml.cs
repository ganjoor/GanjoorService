using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class EditorModel : PageModel
    {
        /// <summary>
        /// my last edit
        /// </summary>
        public GanjoorPoemCorrectionViewModel MyLastEdit { get; set; }

        /// <summary>
        /// page
        /// </summary>
        public GanjoorPageCompleteViewModel PageInformation { get; set; }

        /// <summary>
        /// rhythms alphabetically
        /// </summary>
        public GanjoorMetre[] RhythmsAlphabetically { get; set; }

        /// <summary>
        /// rhythms by frequency
        /// </summary>
        public GanjoorMetre[] RhythmsByVerseCount { get; set; }

        public bool CanAssignRhythms { get; set; }

        /// <summary>
        /// fatal error
        /// </summary>
        public string FatalError { get; set; }

        public string GetVersePosition(GanjoorVerseViewModel verse)
        {
            return VersePositionHelper.GetVersePositionString(verse.VersePosition);
        }

        public int GetVerseCoupletNumber(GanjoorVerseViewModel verse)
        {
            int n = 1;
            VersePosition pre = VersePosition.Right;
            foreach (var v in PageInformation.Poem.Verses)
            {
                if (v.Id == verse.Id)
                {
                    if (pre == VersePosition.CenteredVerse1 && v.VersePosition != VersePosition.CenteredVerse2)
                        n++;
                    return n;
                }
                if (v.VersePosition == VersePosition.Left || v.VersePosition == VersePosition.CenteredVerse2
                    || v.VersePosition == VersePosition.Single || v.VersePosition == VersePosition.Paragraph)
                    n++;
                else
                if (pre == VersePosition.CenteredVerse1)
                    n++;
                pre = v.VersePosition;
            }
            return -1;
        }


        public PoemRelatedImage TextSourceImage { get; set; }

        public GanjoorMetre GanjoorMetre1 { get; set; }

        public GanjoorMetre GanjoorMetre2 { get; set; }

        public string RhymeLetters { get; set; }

        /// <summary>
        /// valid for whole poem sections
        /// </summary>
        public GanjoorPoemFormat? PoemFormat { get; set; }

        /// <summary>
        /// can edit
        /// </summary>
        public bool CanEdit { get; set; }


        /// <summary>
        /// show admin ops
        /// </summary>
        public bool ShowAdminOps { get; set; }

        /// <summary>
        /// locations
        /// </summary>
        public List<GanjoorGeoLocation> Locations { get; set; }


        /// <summary>
        /// poem geo date tags
        /// </summary>
        public PoemGeoDateTag[] PoemGeoDateTags { get; set; }

        public GanjoorLanguage[] Languages { get; set; }

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

        /// <summary>
        /// get
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            FatalError = "";
            CanEdit = Request.Cookies["CanEdit"] == "True";

            ShowAdminOps = CanEdit && Request.Query["admin"] == "1";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var editResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/correction/last/{Request.Query["id"]}");
                    if (!editResponse.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await editResponse.Content.ReadAsStringAsync());
                        return Page();
                    }
                    MyLastEdit = JsonConvert.DeserializeObject<GanjoorPoemCorrectionViewModel>(await editResponse.Content.ReadAsStringAsync());




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

                    var pageUrlResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={Request.Query["id"]}");
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

                    if (PageInformation.Poem.Sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.PoemFormat != null).Any())
                    {
                        PoemFormat = PageInformation.Poem.Sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.PoemFormat != null).OrderBy(s => s.VerseType).First().PoemFormat;
                    }
                    else
                    {
                        PoemFormat = GanjoorPoemFormat.Unknown;
                    }

                    if (PageInformation.Poem.Images.Where(i => i.IsTextOriginalSource).Any())
                    {
                        TextSourceImage = PageInformation.Poem.Images.Where(i => i.IsTextOriginalSource).First();
                    }

                    CanAssignRhythms = true;
                    if (PageInformation.Poem.Verses.Any(v => v.VersePosition == VersePosition.Paragraph))
                    {
                        CanAssignRhythms = false;
                    }
                    else
                        if (PageInformation.Poem.Sections.Count(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First) > 1)
                    {
                        CanAssignRhythms = false;
                    }
                    else
                        if (PageInformation.Poem.Sections.Length == 0)
                    {
                        CanAssignRhythms = false;
                    }

                    await ReadLanguagesAsync(secureClient);

                    if (ShowAdminOps)
                    {
                        var responseLocations = await secureClient.GetAsync($"{APIRoot.Url}/api/locations");
                        if (!responseLocations.IsSuccessStatusCode)
                        {
                            FatalError = JsonConvert.DeserializeObject<string>(await responseLocations.Content.ReadAsStringAsync());
                            return Page();
                        }

                        Locations = new List<GanjoorGeoLocation>();
                        Locations.Add
                            (
                            new GanjoorGeoLocation()
                            {
                                Id = 0,
                                Latitude = 0,
                                Longitude = 0,
                                Name = ""
                            }
                            );

                        Locations.AddRange(JsonConvert.DeserializeObject<GanjoorGeoLocation[]>(await responseLocations.Content.ReadAsStringAsync()));


                        var tagsResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{PageInformation.Id}/geotag");
                        if (!tagsResponse.IsSuccessStatusCode)
                        {
                            FatalError = JsonConvert.DeserializeObject<string>(await tagsResponse.Content.ReadAsStringAsync());
                            return Page();
                        }

                        PoemGeoDateTags = JsonConvert.DeserializeObject<PoemGeoDateTag[]>(await tagsResponse.Content.ReadAsStringAsync());
                    }
                }
                else
                {
                    FatalError = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostDeletePoemCorrectionsAsync(int poemid)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.DeleteAsync(
                        $"{APIRoot.Url}/api/ganjoor/poem/correction/{poemid}");
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
        }


        public class PoemCorrectionStructue
        {
            public int poemid { get; set; }
            public string[] verseOrderText { get; set; }
            
            public int[] verseOrderMarkedForDelete { get; set; }
            
            public string[] versePositions { get; set; }
            public string rhythm { get; set; }
            public string rhythm2 { get; set; }
            public string rhyme { get; set; }
            public string format { get; set; }
            public string[] verseOrderSummaries { get; set; }
            public string[] verseLanguages { get; set; }
            public string note { get; set; }
            public bool hideMyName { get; set; }
        }

        public async Task<IActionResult> OnPostSendPoemCorrectionsAsync([FromBody] PoemCorrectionStructue pcs)
        {
            if (pcs == null)
            {
                return new BadRequestObjectResult("خطای پیش‌بینی نشده: لطفاً نشانی این شعر را به ganjoor@ganjoor.net ارسال بفرمایید تا بررسی بیشتری انجام شود.");
            }
            
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var pageUrlResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={pcs.poemid}");
                    if (!pageUrlResponse.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(FatalError);
                    }
                    var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

                    var pageQuery = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
                    if (!pageQuery.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await pageQuery.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(FatalError);
                    }
                    var pageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

                    string title = null;
                    List<GanjoorVerseVOrderText> vOrderTexts = new List<GanjoorVerseVOrderText>();
                    List<VersePosition?> versePositions = new List<VersePosition?>();
                    foreach (var versePosition in pcs.versePositions)
                    {
                        if(versePosition != null)
                        {
                            versePositions.Add((VersePosition)Enum.Parse(typeof(VersePosition), versePosition));
                        }
                    }
                    foreach (string v in pcs.verseOrderText)
                    {
                        var vParts = v.Split("TextSeparator", StringSplitOptions.RemoveEmptyEntries);
                        int vOrder = int.Parse(vParts[0]);
                        if (vOrder == 0)
                            title = vParts[1].Replace("ۀ", "هٔ").Replace("ك", "ک");
                        else
                        {
                            int? langaugeId = null;
                            if (!(int.Parse(pcs.verseLanguages[vOrder]) == 1 && pageInformation.Poem.Verses.Single(v => v.VOrder == vOrder).LanguageId == null))
                            {
                                langaugeId = int.Parse(pcs.verseLanguages[vOrder]);
                            }
                            string verseText = pageInformation.Poem.Verses.Where(verse => verse.VOrder == vOrder).Single().Text == vParts[1] ? null : vParts[1].Replace("ۀ", "هٔ").Replace("ك", "ک");
                            VersePosition? versePos = pageInformation.Poem.Verses.Single(v => v.VOrder == vOrder).VersePosition == versePositions[vOrder - 1] ? null : versePositions[vOrder - 1];
                            bool markedForDelete = pcs.verseOrderMarkedForDelete.Any(v => v == vOrder);

                            if (verseText != null || markedForDelete || versePos != null || langaugeId != null)
                            {
                                vOrderTexts.Add
                                (
                                new GanjoorVerseVOrderText()
                                {
                                    VORder = vOrder,
                                    Text = verseText,
                                    MarkForDelete = markedForDelete,
                                    VersePosition = versePos,
                                    LanguageId = langaugeId,
                                }
                                );
                            }

                        }
                    }

                    string poemSummary = null;
                    foreach (string v in pcs.verseOrderSummaries)
                    {
                        var vParts = v.Split("TextSeparator", System.StringSplitOptions.RemoveEmptyEntries);
                        int vOrder = int.Parse(vParts[0]);
                        if (vOrder == 0)
                        {
                            poemSummary = vParts.Length > 1 ? vParts[1].Replace("ۀ", "هٔ").Replace("ك", "ک") : "";
                        }
                        else
                        {
                            if (vOrderTexts.Where(t => t.VORder == vOrder).Any())
                            {
                                vOrderTexts.First(t => t.VORder == vOrder).CoupletSummary = vParts[1].Replace("ۀ", "هٔ").Replace("ك", "ک");
                            }
                            else
                            {
                                vOrderTexts.Add
                                (
                                new GanjoorVerseVOrderText()
                                {
                                    VORder = vOrder,
                                    Text = null,
                                    MarkForDelete = false,
                                    VersePosition = null,
                                    CoupletSummary = vParts.Length > 1 ? vParts[1].Replace("ۀ", "هٔ").Replace("ك", "ک") :  "",
                                }
                                );
                            }
                        }
                    }

                    if (title == null && poemSummary == null && vOrderTexts.Count == 0 && pcs.rhythm == null && pcs.rhythm2 == null && pcs.rhyme == null && pcs.format == null)
                        return new BadRequestObjectResult("شما هیچ تغییری در اطلاعات نداده‌اید!");

                    if (pcs.rhythm == "null")
                        pcs.rhythm = "";

                    if (pcs.rhythm2 == "null")
                        pcs.rhythm2 = "";

                    if (pcs.rhythm2 != null)
                    {
                        if (pcs.rhythm == pcs.rhythm2)
                            return new BadRequestObjectResult("وزن اول و دوم یکسانند!");
                    }



                    GanjoorPoemCorrectionViewModel correction = new GanjoorPoemCorrectionViewModel()
                    {
                        PoemId = pcs.poemid,
                        Title = title,
                        VerseOrderText = vOrderTexts.ToArray(),
                        Rhythm = pcs.rhythm,
                        Rhythm2 = pcs.rhythm2,
                        RhymeLetters = pcs.rhyme,
                        PoemFormat = string.IsNullOrEmpty(pcs.format) ? (GanjoorPoemFormat?) null : (GanjoorPoemFormat)Enum.Parse(typeof(GanjoorPoemFormat), pcs.format),
                        PoemSummary = poemSummary,
                        Note = pcs.note,
                        HideMyName = pcs.hideMyName
                    };

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
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }


        public async Task<IActionResult> OnPostBreakPoemAsync(int poemId, int vOrder)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync(
                        $"{APIRoot.Url}/api/ganjoor/poem/break",
                        new StringContent(JsonConvert.SerializeObject
                        (
                            new PoemVerseOrder()
                            {
                                PoemId = poemId,
                                VOrder = vOrder
                            }
                        ),
                        Encoding.UTF8,
                        "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(JsonConvert.DeserializeObject<int>(await response.Content.ReadAsStringAsync()));
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }

        public async Task<IActionResult> OnPostUpdateRelatedSectionsAsync(int meterId, string rhyme)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync(
                        $"{APIRoot.Url}/api/ganjoor/sections/updaterelated?metreId={meterId}&rhyme={rhyme}",
                        null
                        );
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkResult();
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }

        public async Task<IActionResult> OnPostRegeneratePoemSectionsAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync(
                        $"{APIRoot.Url}/api/ganjoor/poem/{id}/sections/regenerate",
                        null
                        );
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkResult();
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }



        public async Task<IActionResult> OnGetComputeRhymeAsync(int id)
        {

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/analysisrhyme/{id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    var rhyme = JsonConvert.DeserializeObject<GanjooRhymeAnalysisResult>(await response.Content.ReadAsStringAsync());
                    return new OkObjectResult(rhyme.Rhyme);
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }

        }

        public async Task<IActionResult> OnPostNewGeoDateTagAsync(int poemId, int locationId, string year, int month, string day, bool verifiedDate, bool ignoreInCategory)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync(
                        $"{APIRoot.Url}/api/ganjoor/poem/geotag",
                         new StringContent(JsonConvert.SerializeObject(
                             new PoemGeoDateTag()
                             {
                                 PoemId = poemId,
                                 LocationId = locationId == 0 ? null : locationId,
                                 LunarYear = string.IsNullOrEmpty(year) ? null : int.Parse(year),
                                 LunarMonth = month == 0 ? null : month,
                                 LunarDay = string.IsNullOrEmpty(day) ? null : int.Parse(day),
                                 VerifiedDate = verifiedDate,
                                 IgnoreInCategory = ignoreInCategory,
                             }
                             ),
                        Encoding.UTF8,
                        "application/json")
                        );
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkResult();
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }

        public async Task<IActionResult> OnDeleteGeoDateTagAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.DeleteAsync(
                        $"{APIRoot.Url}/api/ganjoor/poem/geotag/{id}"
                        );
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkResult();
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }

        public async Task<IActionResult> OnPostSaveMetaAsync(int poemId, bool noindex, string redirectfromurl, int mixedmodeorder)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync(
                        $"{APIRoot.Url}/api/ganjoor/poem/adminedit/{poemId}",
                         new StringContent(JsonConvert.SerializeObject(
                             new GanjoorModifyPageViewModel()
                             {
                                 NoIndex = noindex,
                                 RedirectFromFullUrl = redirectfromurl,
                                 MixedModeOrder = mixedmodeorder,
                             }
                             ),
                        Encoding.UTF8,
                        "application/json")
                        );
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkResult();
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }
    }
}
