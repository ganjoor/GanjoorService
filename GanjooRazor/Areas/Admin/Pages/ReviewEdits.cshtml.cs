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
        /// one line of a literary tag shown to the moderator
        /// </summary>
        /// <summary>
        /// comparison form of a concept name: no vowel marks, ZWNJ, spaces or punctuation, Arabic ye/kaf unified
        /// </summary>
        private static string _ConceptNorm(string text)
        {
            var sb = new StringBuilder();
            foreach (char ch in text ?? "")
            {
                if ((ch >= '\u064B' && ch <= '\u065F') || ch == '\u0670' || ch == '\u0640' || ch == '\u200c' || ch == '\u200d')
                    continue;
                if (char.IsWhiteSpace(ch) || char.IsPunctuation(ch))
                    continue;
                sb.Append(ch == 'ي' ? 'ی' : ch == 'ك' ? 'ک' : ch);
            }
            return sb.ToString();
        }

        private static HashSet<string> _ConceptWords(string text)
        {
            return new HashSet<string>((text ?? "").Replace('\u200c', ' ').Replace('ي', 'ی').Replace('ك', 'ک')
                .Split(new[] { ' ', '،', ',', ';', '-', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => _ConceptNorm(w)).Where(w => w.Length > 0));
        }

        /// <summary>
        /// the API finds concepts whose name merely contains the phrase; keeps only an identical concept (name or alias)
        /// or concepts sharing at least half of the words of both names
        /// </summary>
        private static Tuple<string, bool> _SimilarConcepts(string suggested, LiteraryConcept[] found)
        {
            string wanted = _ConceptNorm(suggested);
            var same = found.Where(c => _ConceptNorm(c.Name) == wanted
                    || (c.Aliases ?? "").Split(new[] { '،', ',', ';', '|', '\n' }, StringSplitOptions.RemoveEmptyEntries).Any(a => _ConceptNorm(a) == wanted))
                .Select(c => c.Name).ToList();
            if (same.Count > 0)
                return Tuple.Create(string.Join("، ", same), true);
            var mine = _ConceptWords(suggested);
            var near = found.Where(c =>
            {
                var theirs = _ConceptWords(c.Name);
                int shared = mine.Count(w => theirs.Contains(w));
                return shared > 0 && shared * 2 >= mine.Count && shared * 2 >= theirs.Count;
            }).Take(4).Select(c => c.Name);
            return Tuple.Create(string.Join("، ", near), false);
        }

        public class LiteraryReviewLine
        {
            public string Slot { get; set; }
            public string Value { get; set; }
            public bool NewConcept { get; set; }
            public string Similar { get; set; }
            /// <summary>
            /// Similar is an existing concept with the same name (or alias), not just a near one
            /// </summary>
            public bool SimilarIsSame { get; set; }
        }

        /// <summary>
        /// a suggested literary tag (or a request to delete one) prepared for display
        /// </summary>
        public class LiteraryReviewItem
        {
            public string DeviceName { get; set; }
            public string Couplet { get; set; }
            public int CoupletNumber { get; set; }
            public bool Delete { get; set; }
            public bool Uncertain { get; set; }
            public string Note { get; set; }
            public List<LiteraryReviewLine> Lines { get; set; } = new List<LiteraryReviewLine>();
        }

        public List<LiteraryReviewItem> LiteraryReviewItems { get; set; } = new List<LiteraryReviewItem>();

        private static Dictionary<int, Tuple<string, string>> GetHemistichs(GanjoorVerseViewModel[] verses)
        {
            var res = new Dictionary<int, Tuple<string, string>>();
            int n = -1; //0 based like GanjoorVerse.CoupletIndex
            string m1 = null;
            foreach (var v in verses)
            {
                switch (v.VersePosition)
                {
                    case VersePosition.Paragraph:
                    case VersePosition.Single:
                        n++;
                        res[n] = new Tuple<string, string>(v.Text, null);
                        m1 = null;
                        break;
                    case VersePosition.Right:
                    case VersePosition.CenteredVerse1:
                        n++;
                        m1 = v.Text;
                        res[n] = new Tuple<string, string>(m1, null);
                        break;
                    case VersePosition.Left:
                    case VersePosition.CenteredVerse2:
                        if (m1 != null)
                            res[n] = new Tuple<string, string>(m1, v.Text);
                        break;
                }
            }
            return res;
        }

        private async Task PrepareLiteraryReviewAsync(HttpClient secureClient)
        {
            LiteraryReviewItems = new List<LiteraryReviewItem>();
            if (Correction.LiteraryTags == null || Correction.LiteraryTags.Length == 0)
                return;
            try
            {
                var devicesResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/devices");
                var devices = devicesResponse.IsSuccessStatusCode
                    ? JsonConvert.DeserializeObject<LiteraryDevice[]>(await devicesResponse.Content.ReadAsStringAsync())
                    : Array.Empty<LiteraryDevice>();

                //devices still waiting for approval, so a tag using one is shown by name (and marked as pending)
                var pendingDevicesResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/devices/suggested");
                if (pendingDevicesResponse.IsSuccessStatusCode)
                {
                    var pendingDevices = JsonConvert.DeserializeObject<LiteraryDevice[]>(await pendingDevicesResponse.Content.ReadAsStringAsync());
                    if (pendingDevices != null && pendingDevices.Length > 0)
                        devices = devices.Concat(pendingDevices).ToArray();
                }

                var tagsResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/poem/{Correction.PoemId}/tags");
                var existingTags = tagsResponse.IsSuccessStatusCode
                    ? JsonConvert.DeserializeObject<PoemLiteraryTag[]>(await tagsResponse.Content.ReadAsStringAsync())
                    : Array.Empty<PoemLiteraryTag>();

                var hemistichs = GetHemistichs(PageInformation.Poem.Verses);
                var parsed = new List<List<LiteraryTagPartSuggestion>>();
                var conceptIds = new HashSet<int>();
                foreach (var t in Correction.LiteraryTags)
                {
                    List<LiteraryTagPartSuggestion> parts = new List<LiteraryTagPartSuggestion>();
                    if (!t.MarkForDelete && !string.IsNullOrEmpty(t.PartsJson))
                    {
                        try { parts = JsonConvert.DeserializeObject<List<LiteraryTagPartSuggestion>>(t.PartsJson) ?? parts; } catch (JsonException) { }
                    }
                    parsed.Add(parts);
                    foreach (var p in parts.Where(p => p.ConceptId != null))
                        conceptIds.Add(p.ConceptId.Value);
                }

                var conceptNames = new Dictionary<int, string>();
                if (conceptIds.Count > 0)
                {
                    var cr = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/concepts/by-ids?ids={string.Join(",", conceptIds)}");
                    if (cr.IsSuccessStatusCode)
                        foreach (var c in JsonConvert.DeserializeObject<LiteraryConcept[]>(await cr.Content.ReadAsStringAsync()))
                            conceptNames[c.Id] = c.Name;
                }

                var similarCache = new Dictionary<string, Tuple<string, bool>>();

                string SpanText(LiteraryTagPartSuggestion p)
                {
                    if (p.WordStart == null || p.WordEnd == null || !hemistichs.ContainsKey(p.CoupletIndex))
                        return null;
                    var h = hemistichs[p.CoupletIndex];
                    var words = (p.Misra == 1 ? h.Item1 : h.Item2 ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return string.Join(" ", words.Skip(p.WordStart.Value - 1).Take(p.WordEnd.Value - p.WordStart.Value + 1));
                }

                string CoupletLabel(int n)
                {
                    return n >= 0 && hemistichs.ContainsKey(n) ? hemistichs[n].Item1 : null;
                }

                for (int i = 0; i < Correction.LiteraryTags.Length; i++)
                {
                    var t = Correction.LiteraryTags[i];
                    var item = new LiteraryReviewItem()
                    {
                        Delete = t.MarkForDelete,
                        CoupletNumber = t.CoupletIndex,
                        Couplet = CoupletLabel(t.CoupletIndex),
                        Uncertain = t.Uncertain,
                        Note = t.SuggestionNote
                    };

                    if (t.MarkForDelete)
                    {
                        var ex = existingTags.FirstOrDefault(x => x.Id == t.ExistingTagId);
                        if (ex == null)
                        {
                            item.DeviceName = "(برچسب دیگر وجود ندارد)";
                        }
                        else
                        {
                            var dev = devices.FirstOrDefault(d => d.Id == ex.DeviceId);
                            item.DeviceName = dev?.Name ?? $"#{ex.DeviceId}";
                            item.CoupletNumber = ex.CoupletIndex;
                            item.Couplet = CoupletLabel(ex.CoupletIndex);
                            item.Uncertain = ex.Uncertain;
                            foreach (var p in ex.Parts)
                            {
                                var sp = new LiteraryTagPartSuggestion() { CoupletIndex = p.CoupletIndex, Misra = p.Misra, WordStart = p.WordStart, WordEnd = p.WordEnd };
                                var vals = new List<string>();
                                var st = SpanText(sp);
                                if (st != null) vals.Add($"«{st}»");
                                if (p.Concept != null) vals.Add(p.Concept.Name);
                                if (!string.IsNullOrEmpty(p.Text)) vals.Add(p.Text);
                                item.Lines.Add(new LiteraryReviewLine() { Slot = dev?.Slots.FirstOrDefault(s => s.Id == p.SlotId)?.Name ?? "?", Value = string.Join(" ← ", vals) });
                            }
                        }
                    }
                    else
                    {
                        var dev = devices.FirstOrDefault(d => d.Id == t.DeviceId);
                        item.DeviceName = (dev?.Name ?? t.Device?.Name ?? $"#{t.DeviceId}") + (dev != null && dev.Status == LiteraryEntityStatus.Suggested ? " (آرایهٔ تأییدنشده؛ ابتدا آن را تأیید کنید)" : "");
                        foreach (var p in parsed[i])
                        {
                            var vals = new List<string>();
                            var st = SpanText(p);
                            if (st != null) vals.Add($"«{st}»");
                            var line = new LiteraryReviewLine() { Slot = dev?.Slots.FirstOrDefault(s => s.Id == p.SlotId)?.Name ?? "?" };
                            if (p.ConceptId != null)
                            {
                                vals.Add(conceptNames.ContainsKey(p.ConceptId.Value) ? conceptNames[p.ConceptId.Value] : $"مفهوم #{p.ConceptId}");
                            }
                            else if (!string.IsNullOrWhiteSpace(p.SuggestedConceptName))
                            {
                                vals.Add(p.SuggestedConceptName);
                                line.NewConcept = true;
                                if (!similarCache.ContainsKey(p.SuggestedConceptName))
                                {
                                    var sr = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/concepts?query={Uri.EscapeDataString(p.SuggestedConceptName)}&take=8");
                                    similarCache[p.SuggestedConceptName] = sr.IsSuccessStatusCode
                                        ? _SimilarConcepts(p.SuggestedConceptName, JsonConvert.DeserializeObject<LiteraryConcept[]>(await sr.Content.ReadAsStringAsync()))
                                        : Tuple.Create("", false);
                                }
                                line.Similar = similarCache[p.SuggestedConceptName].Item1;
                                line.SimilarIsSame = similarCache[p.SuggestedConceptName].Item2;
                            }
                            if (!string.IsNullOrEmpty(p.Text)) vals.Add(p.Text);
                            line.Value = string.Join(" ← ", vals);
                            item.Lines.Add(line);
                        }
                    }
                    LiteraryReviewItems.Add(item);
                }
            }
            catch (Exception exp)
            {
                FatalError = "خطا در آماده‌سازی آرایه‌های ادبی پیشنهادی: " + exp.Message;
            }
        }

        /// <summary>
        /// full location catalog, offered as an alternative to a user's new-location suggestion so the
        /// moderator can correct a mistyped/duplicate name by linking it to the existing entry instead
        /// </summary>
        public List<GanjoorGeoLocation> Locations { get; set; }

        /// <summary>
        /// camelCase JSON of Locations (id/name/latitude/longitude only), used client-side for the
        /// search-as-you-type location picker on each geo tag (see setupLocationAutocomplete in bk.js) -
        /// same shape/purpose as Editor.cshtml.cs's AllLocationsJson
        /// </summary>
        public string AllLocationsJson =>
            JsonConvert.SerializeObject(
                (Locations ?? new List<GanjoorGeoLocation>()).Select(l => new { l.Id, l.Name, l.Latitude, l.Longitude }),
                new JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() }
            );

        /// <summary>
        /// full people catalog, offered as an alternative to a user's new-person suggestion so the
        /// moderator can correct a duplicate/misidentified suggestion by linking it to the existing
        /// entry instead - same purpose as Locations above
        /// </summary>
        public List<GanjoorRelatedPerson> People { get; set; }

        /// <summary>
        /// camelCase JSON of People (id/name/birthYearInLHijri/deathYearInLHijri only), used
        /// client-side for the search-as-you-type person picker on each geo tag (see
        /// setupPersonAutocomplete in bk.js) - same shape/purpose as Editor.cshtml.cs's AllPeopleJson
        /// </summary>
        public string AllPeopleJson =>
            JsonConvert.SerializeObject(
                (People ?? new List<GanjoorRelatedPerson>()).Select(p => new { p.Id, p.Name, p.BirthYearInLHijri, p.DeathYearInLHijri, p.Aliases }),
                new JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() }
            );

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
                FatalError = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response);
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
                        FatalError = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(nextResponse);
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
                            FatalError = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(pageUrlResponse);
                            return Page();
                        }
                        var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

                        var pageQuery = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
                        if (!pageQuery.IsSuccessStatusCode)
                        {
                            FatalError = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(pageQuery);
                            return Page();
                        }
                        PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

                        Couplets = GetCouplets(PageInformation.Poem.Verses);

                        await PrepareLiteraryReviewAsync(secureClient);

                        if (Correction.GeoDateTags != null && Correction.GeoDateTags.Any())
                        {
                            var responseLocations = await secureClient.GetAsync($"{APIRoot.Url}/api/locations");
                            if (!responseLocations.IsSuccessStatusCode)
                            {
                                FatalError = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(responseLocations);
                                return Page();
                            }
                            Locations = new List<GanjoorGeoLocation>();
                            Locations.Add(new GanjoorGeoLocation() { Id = 0, Latitude = 0, Longitude = 0, Name = "" });
                            Locations.AddRange(JsonConvert.DeserializeObject<GanjoorGeoLocation[]>(await responseLocations.Content.ReadAsStringAsync()));

                            var responsePeople = await secureClient.GetAsync($"{APIRoot.Url}/api/people");
                            if (!responsePeople.IsSuccessStatusCode)
                            {
                                FatalError = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(responsePeople);
                                return Page();
                            }
                            People = new List<GanjoorRelatedPerson>();
                            People.Add(new GanjoorRelatedPerson() { Id = 0, Name = "" });
                            People.AddRange(JsonConvert.DeserializeObject<GanjoorRelatedPerson[]>(await responsePeople.Content.ReadAsStringAsync()));
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
                        return new BadRequestObjectResult(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(correctionResponse));
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

                                    // same pattern as the location correction above, but for a suggested person
                                    // instead of a suggested location - let the moderator link a suggestion to an
                                    // existing catalog person (e.g. the contributor didn't find them in the search),
                                    // or just fix a typo in a brand new person's name, before approving
                                    string correctedPersonIdText = (pms.geoTagPersonId != null && i < pms.geoTagPersonId.Length) ? pms.geoTagPersonId[i] : null;
                                    if (!string.IsNullOrWhiteSpace(correctedPersonIdText) && int.TryParse(correctedPersonIdText, out int correctedPersonId) && correctedPersonId > 0)
                                    {
                                        Correction.GeoDateTags[i].PersonId = correctedPersonId;
                                        Correction.GeoDateTags[i].SuggestedPersonGraphJson = null;
                                    }
                                    else if (Correction.GeoDateTags[i].PersonId == null && !string.IsNullOrWhiteSpace(Correction.GeoDateTags[i].SuggestedPersonGraphJson))
                                    {
                                        string correctedPersonName = (pms.geoTagPersonName != null && i < pms.geoTagPersonName.Length) ? pms.geoTagPersonName[i] : null;
                                        if (!string.IsNullOrWhiteSpace(correctedPersonName))
                                        {
                                            try
                                            {
                                                var personGraph = JObject.Parse(Correction.GeoDateTags[i].SuggestedPersonGraphJson);
                                                if (personGraph["person"] != null)
                                                {
                                                    personGraph["person"]["name"] = correctedPersonName.Trim();
                                                    Correction.GeoDateTags[i].SuggestedPersonGraphJson = personGraph.ToString(Newtonsoft.Json.Formatting.None);
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                return new BadRequestObjectResult("برچسب نامبردۀ پیشنهادی قابل تفسیر نیست.");
                                            }
                                        }
                                    }
                                }

                                if (!Correction.GeoDateTags[i].MarkForDelete)
                                {
                                    // the moderator may overrule the contributor's role of a tagged person (also in a request to change
                                    // the role of an existing person tag) and the "ignore in the category map" choice of a location tag
                                    string mentionText = (pms.geoTagPersonMention != null && i < pms.geoTagPersonMention.Length) ? pms.geoTagPersonMention[i] : null;
                                    if (!string.IsNullOrWhiteSpace(mentionText) && int.TryParse(mentionText, out int mentionValue) && Enum.IsDefined(typeof(PersonMentionKind), mentionValue))
                                    {
                                        Correction.GeoDateTags[i].PersonMention = (PersonMentionKind)mentionValue;
                                    }
                                    string ignoreText = (pms.geoTagIgnoreInCategory != null && i < pms.geoTagIgnoreInCategory.Length) ? pms.geoTagIgnoreInCategory[i] : null;
                                    if (!string.IsNullOrWhiteSpace(ignoreText) && bool.TryParse(ignoreText, out bool ignoreValue))
                                    {
                                        Correction.GeoDateTags[i].IgnoreInCategory = ignoreValue;
                                    }
                                }

                                Correction.GeoDateTags[i].Result = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.geoTagReviewResult[i]);
                                Correction.GeoDateTags[i].ReviewNote = (pms.geoTagReviewNotes != null && i < pms.geoTagReviewNotes.Length) ? pms.geoTagReviewNotes[i] : null;
                            }
                        }
                    }

                    if (Correction.LiteraryTags != null && Correction.LiteraryTags.Length > 0)
                    {
                        if (pms.literaryReviewResult == null || pms.literaryReviewResult.Length != Correction.LiteraryTags.Length || pms.literaryReviewResult.Any(r => r == null))
                        {
                            return new BadRequestObjectResult("لطفاً تکلیف بررسی تمام آرایه‌های ادبی پیشنهادی را مشخص کنید.");
                        }
                        for (int i = 0; i < Correction.LiteraryTags.Length; i++)
                        {
                            Correction.LiteraryTags[i].Result = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), pms.literaryReviewResult[i]);
                            Correction.LiteraryTags[i].ReviewNote = (pms.literaryReviewNotes != null && i < pms.literaryReviewNotes.Length) ? pms.literaryReviewNotes[i] : null;
                            Correction.LiteraryTags[i].Device = null;
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
                            try
                            {
                                // normal case: the API returns the error as a JSON-encoded string (see
                                // OnPostDeletePoemCorrectionsAsync in Editor.cshtml.cs for the same
                                // pattern). Guard against it not being one - e.g. an HTML error page
                                // from a proxy/host in front of the API - so that doesn't throw an
                                // unhandled exception here and surface as GanjooRazor's own generic HTML
                                // error page, which is unreadable and leaves the real error only in the
                                // Windows Event Log.
                                err = JsonConvert.DeserializeObject<string>(err);
                            }
                            catch (JsonException)
                            {
                                err = "خطایی در سرور رخ داد. لطفاً بعداً دوباره تلاش کنید.";
                            }
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
