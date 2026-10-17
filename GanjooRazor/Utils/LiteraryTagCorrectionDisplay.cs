using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Utils
{
    /// <summary>
    /// one line of a literary tag (slot name and its value)
    /// </summary>
    public class LiteraryDisplayLine
    {
        public string Slot { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// a literary tag correction prepared for display
    /// </summary>
    public class LiteraryDisplayItem
    {
        public string DeviceName { get; set; }
        public int CoupletNumber { get; set; }
        public List<LiteraryDisplayLine> Lines { get; set; } = new List<LiteraryDisplayLine>();
    }

    /// <summary>
    /// loads what is needed to show the literary tags of a list of corrections in readable form
    /// (device and slot names, concept names, the text of the tagged words) and describes each tag.
    /// Used by Edits.cshtml and PoemCorrectionsHistory.cshtml, like GeoDateTagCorrectionDisplay.
    /// </summary>
    public class LiteraryDisplayContext
    {
        private readonly Dictionary<int, LiteraryDevice> _devices = new Dictionary<int, LiteraryDevice>();
        private readonly Dictionary<int, string> _conceptNames = new Dictionary<int, string>();
        private readonly Dictionary<int, Dictionary<int, Tuple<string, string>>> _hemistichs = new Dictionary<int, Dictionary<int, Tuple<string, string>>>();

        /// <summary>
        /// hemistichs of couplets by 1-based couplet number
        /// </summary>
        public static Dictionary<int, Tuple<string, string>> GetHemistichs(GanjoorVerseViewModel[] verses)
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

        /// <summary>
        /// loads the data needed to describe the literary tags of the corrections; never throws - on failure
        /// the descriptions just lack names/texts
        /// </summary>
        public static async Task<LiteraryDisplayContext> LoadAsync(HttpClient secureClient, IEnumerable<GanjoorPoemCorrectionViewModel> corrections)
        {
            var ctx = new LiteraryDisplayContext();
            try
            {
                var withTags = (corrections ?? Array.Empty<GanjoorPoemCorrectionViewModel>())
                    .Where(c => c.LiteraryTags != null && c.LiteraryTags.Length > 0).ToList();
                if (withTags.Count == 0)
                    return ctx;

                var devicesResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/devices");
                if (devicesResponse.IsSuccessStatusCode)
                    foreach (var d in JsonConvert.DeserializeObject<LiteraryDevice[]>(await devicesResponse.Content.ReadAsStringAsync()))
                        ctx._devices[d.Id] = d;

                var conceptIds = new HashSet<int>();
                foreach (var t in withTags.SelectMany(c => c.LiteraryTags))
                {
                    foreach (var p in ReadParts(t.PartsJson).Where(p => p.ConceptId != null))
                        conceptIds.Add(p.ConceptId.Value);
                }
                if (conceptIds.Count > 0)
                {
                    var cr = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/concepts/by-ids?ids={string.Join(",", conceptIds)}");
                    if (cr.IsSuccessStatusCode)
                        foreach (var c in JsonConvert.DeserializeObject<LiteraryConcept[]>(await cr.Content.ReadAsStringAsync()))
                            ctx._conceptNames[c.Id] = c.Name;
                }

                foreach (var poemId in withTags.Select(c => c.PoemId).Distinct())
                {
                    var pageUrlResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={poemId}");
                    if (!pageUrlResponse.IsSuccessStatusCode)
                        continue;
                    var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                    var pageQuery = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
                    if (!pageQuery.IsSuccessStatusCode)
                        continue;
                    var page = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();
                    if (page?.Poem?.Verses != null)
                        ctx._hemistichs[poemId] = GetHemistichs(page.Poem.Verses);
                }
            }
            catch (Exception)
            {
                // display is best effort
            }
            return ctx;
        }

        private static List<LiteraryTagPartSuggestion> ReadParts(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new List<LiteraryTagPartSuggestion>();
            try
            {
                return JsonConvert.DeserializeObject<List<LiteraryTagPartSuggestion>>(json) ?? new List<LiteraryTagPartSuggestion>();
            }
            catch (JsonException)
            {
                return new List<LiteraryTagPartSuggestion>();
            }
        }

        /// <summary>
        /// describes a tag correction of a poem
        /// </summary>
        public LiteraryDisplayItem Describe(int poemId, GanjoorPoemLiteraryTagCorrection tag)
        {
            _devices.TryGetValue(tag.DeviceId, out LiteraryDevice device);
            var item = new LiteraryDisplayItem()
            {
                CoupletNumber = tag.CoupletIndex,
                DeviceName = device?.Name ?? tag.Device?.Name ?? (tag.DeviceId > 0 ? $"#{tag.DeviceId}" : "")
            };
            _hemistichs.TryGetValue(poemId, out var hemistichs);
            foreach (var p in ReadParts(tag.PartsJson))
            {
                var vals = new List<string>();
                if (p.WordStart != null && p.WordEnd != null && hemistichs != null && hemistichs.ContainsKey(p.CoupletIndex))
                {
                    var h = hemistichs[p.CoupletIndex];
                    var words = (p.Misra == 1 ? h.Item1 : h.Item2 ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    vals.Add("«" + string.Join(" ", words.Skip(p.WordStart.Value - 1).Take(p.WordEnd.Value - p.WordStart.Value + 1)) + "»");
                }
                if (p.ConceptId != null)
                    vals.Add(_conceptNames.ContainsKey(p.ConceptId.Value) ? _conceptNames[p.ConceptId.Value] : $"مفهوم #{p.ConceptId}");
                else if (!string.IsNullOrWhiteSpace(p.SuggestedConceptName))
                    vals.Add(p.SuggestedConceptName);
                if (!string.IsNullOrEmpty(p.Text))
                    vals.Add(p.Text);
                item.Lines.Add(new LiteraryDisplayLine()
                {
                    Slot = device?.Slots.FirstOrDefault(s => s.Id == p.SlotId)?.Name ?? "?",
                    Value = string.Join(" ← ", vals)
                });
            }
            return item;
        }
    }
}
