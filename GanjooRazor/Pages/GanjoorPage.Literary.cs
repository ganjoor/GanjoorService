using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// one word of a hemistich in the literary tags panel
    /// </summary>
    public class LiteraryWordView
    {
        public int Misra { get; set; }
        public int Index { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// one line of a tag (slot name and value)
    /// </summary>
    public class LiteraryLineView
    {
        public string Slot { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// one tag of a couplet
    /// </summary>
    public class LiteraryTagView
    {
        public string DeviceName { get; set; }
        public string Definition { get; set; }
        public bool Uncertain { get; set; }
        public string Note { get; set; }
        /// <summary>
        /// the source of an imported tag (null for tags added by editors or without a registered source)
        /// </summary>
        public string SourceKey { get; set; }
        public string SourceName { get; set; }
        /// <summary>
        /// located words as "misra:start-end" separated by spaces (used by the page script to highlight them)
        /// </summary>
        public string Spans { get; set; }
        public List<LiteraryLineView> Lines { get; set; } = new List<LiteraryLineView>();
    }

    /// <summary>
    /// the tags of one couplet (or of the whole poem when Number is 0)
    /// </summary>
    public class LiteraryCoupletView
    {
        public int Number { get; set; }
        public List<LiteraryWordView> Words1 { get; set; } = new List<LiteraryWordView>();
        public List<LiteraryWordView> Words2 { get; set; } = new List<LiteraryWordView>();
        public List<LiteraryTagView> Tags { get; set; } = new List<LiteraryTagView>();
    }

    public class _LiteraryTagsPartialModel
    {
        public int PoemId { get; set; }
        public bool Failed { get; set; }
        public List<LiteraryCoupletView> Couplets { get; set; } = new List<LiteraryCoupletView>();
        /// <summary>
        /// the sources of the tags shown (distinct)
        /// </summary>
        public List<LiteraryTagSource> Sources { get; set; } = new List<LiteraryTagSource>();
    }

    public partial class GanjoorPageModel
    {
        private static readonly object _literaryDevicesLock = new object();
        private static LiteraryDevice[] _literaryDevicesCache;
        private static DateTime _literaryDevicesCacheTime = DateTime.MinValue;

        private async Task<LiteraryDevice[]> GetLiteraryDevicesCachedAsync()
        {
            lock (_literaryDevicesLock)
            {
                if (_literaryDevicesCache != null && DateTime.Now - _literaryDevicesCacheTime < TimeSpan.FromMinutes(10))
                    return _literaryDevicesCache;
            }
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/devices");
            if (!response.IsSuccessStatusCode)
                return null;
            var devices = JsonConvert.DeserializeObject<LiteraryDevice[]>(await response.Content.ReadAsStringAsync());
            lock (_literaryDevicesLock)
            {
                _literaryDevicesCache = devices;
                _literaryDevicesCacheTime = DateTime.Now;
            }
            return devices;
        }

        private static LiteraryTagSource[] _literarySourcesCache;
        private static DateTime _literarySourcesCacheTime = DateTime.MinValue;

        private async Task<LiteraryTagSource[]> GetLiterarySourcesCachedAsync()
        {
            lock (_literaryDevicesLock)
            {
                if (_literarySourcesCache != null && DateTime.Now - _literarySourcesCacheTime < TimeSpan.FromMinutes(10))
                    return _literarySourcesCache;
            }
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/sources");
            if (!response.IsSuccessStatusCode)
                return Array.Empty<LiteraryTagSource>();
            var sources = JsonConvert.DeserializeObject<LiteraryTagSource[]>(await response.Content.ReadAsStringAsync());
            lock (_literaryDevicesLock)
            {
                _literarySourcesCache = sources;
                _literarySourcesCacheTime = DateTime.Now;
            }
            return sources;
        }

        private static string[] LiteraryWords(string text)
        {
            return (text ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// the approved literary tags (آرایه‌ها) of a poem, loaded when the reader asks for them
        /// </summary>
        public async Task<IActionResult> OnGetLiteraryTagsAsync(int poemId, string url)
        {
            var model = new _LiteraryTagsPartialModel() { PoemId = poemId };
            try
            {
                var tagsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/poem/{poemId}/tags");
                var devices = await GetLiteraryDevicesCachedAsync();
                if (!tagsResponse.IsSuccessStatusCode || devices == null)
                {
                    model.Failed = true;
                    return Partial("~/Pages/Partials/GanjoorPage/_LiteraryTagsPartial.cshtml", model);
                }
                var tags = JsonConvert.DeserializeObject<PoemLiteraryTag[]>(await tagsResponse.Content.ReadAsStringAsync());
                if (tags == null || tags.Length == 0)
                    return Partial("~/Pages/Partials/GanjoorPage/_LiteraryTagsPartial.cshtml", model);

                // hemistichs of couplets, numbered like the editor does
                var hemistichs = new Dictionary<int, Tuple<string, string>>();
                if (!string.IsNullOrEmpty(url))
                {
                    var pageUrl = "/" + url.Trim().Trim('/');
                    var pageResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={Uri.EscapeDataString(pageUrl)}");
                    if (pageResponse.IsSuccessStatusCode)
                    {
                        var page = JObject.Parse(await pageResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();
                        if (page?.Poem?.Verses != null)
                            hemistichs = LiteraryHemistichs(page.Poem.Verses);
                    }
                }

                var sourceByKey = (await GetLiterarySourcesCachedAsync()).GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.First());
                var deviceById = devices.ToDictionary(d => d.Id);
                foreach (var group in tags.GroupBy(t => t.CoupletIndex).OrderBy(g => g.Key))
                {
                    var couplet = new LiteraryCoupletView() { Number = group.Key };
                    if (group.Key > 0 && hemistichs.TryGetValue(group.Key, out var h))
                        FillLiteraryWords(couplet, h.Item1, h.Item2);
                    foreach (var tag in group)
                    {
                        if (!deviceById.TryGetValue(tag.DeviceId, out var device))
                            continue;
                        couplet.Tags.Add(BuildLiteraryTagView(tag, device, couplet, sourceByKey, model.Sources));
                    }
                    if (couplet.Tags.Count > 0)
                        model.Couplets.Add(couplet);
                }
            }
            catch (Exception)
            {
                model.Failed = true;
                model.Couplets.Clear();
            }
            return Partial("~/Pages/Partials/GanjoorPage/_LiteraryTagsPartial.cshtml", model);
        }

        /// <summary>
        /// the words of a couplet's two hemistichs, numbered from 1 in each (the numbering the tags' word ranges use)
        /// </summary>
        private static void FillLiteraryWords(LiteraryCoupletView couplet, string hemistich1, string hemistich2)
        {
            int i = 0;
            foreach (var w in LiteraryWords(hemistich1))
                couplet.Words1.Add(new LiteraryWordView() { Misra = 1, Index = ++i, Text = w });
            i = 0;
            foreach (var w in LiteraryWords(hemistich2))
                couplet.Words2.Add(new LiteraryWordView() { Misra = 2, Index = ++i, Text = w });
        }

        /// <summary>
        /// what the reader sees of one tag; <paramref name="couplet"/> must already hold the words of the tag's couplet
        /// </summary>
        /// <param name="sources">collects the sources of the tags shown (distinct)</param>
        private static LiteraryTagView BuildLiteraryTagView(PoemLiteraryTag tag, LiteraryDevice device, LiteraryCoupletView couplet, Dictionary<string, LiteraryTagSource> sourceByKey, List<LiteraryTagSource> sources)
        {
                var view = new LiteraryTagView()
                {
                    DeviceName = device.Name,
                    Definition = device.Definition,
                    Uncertain = tag.Uncertain,
                    Note = tag.Note,
                    SourceKey = null
                };
                if (!string.IsNullOrEmpty(tag.OriginRef) && sourceByKey.TryGetValue(tag.OriginRef, out var tagSource))
                {
                    view.SourceKey = tagSource.Key;
                    view.SourceName = tagSource.Name;
                    if (!sources.Any(x => x.Key == tagSource.Key))
                        sources.Add(tagSource);
                }
                var spans = new List<string>();
                foreach (var part in (tag.Parts ?? new List<PoemLiteraryTagPart>()).OrderBy(p => p.Order))
                {
                    var slot = device.Slots?.FirstOrDefault(s => s.Id == part.SlotId);
                    if (slot == null)
                        continue;
                    string words = null;
                    bool located = part.WordStart != null && part.WordEnd != null && (part.Misra == 1 || part.Misra == 2);
                    if (located)
                    {
                        var list = part.Misra == 1 ? couplet.Words1 : couplet.Words2;
                        var picked = list.Where(w => w.Index >= part.WordStart && w.Index <= part.WordEnd).Select(w => w.Text).ToList();
                        if (picked.Count > 0)
                        {
                            words = string.Join(" ", picked);
                            spans.Add($"{part.Misra}:{part.WordStart}-{part.WordEnd}");
                        }
                    }
                    if (words == null)
                        words = part.Text;
                    string value;
                    switch (slot.Kind)
                    {
                        case LiteraryDeviceSlotKind.Concept:
                            value = part.Concept?.Name ?? part.Text;
                            break;
                        case LiteraryDeviceSlotKind.SpanConcept:
                            value = (part.Concept != null && !string.IsNullOrEmpty(words) && words != part.Concept.Name)
                                ? $"{words} ← {part.Concept.Name}"
                                : (part.Concept?.Name ?? words);
                            break;
                        case LiteraryDeviceSlotKind.Text:
                            value = part.Text;
                            break;
                        default:
                            value = words;
                            break;
                    }
                    if (!string.IsNullOrEmpty(value))
                        view.Lines.Add(new LiteraryLineView() { Slot = slot.Name, Value = value });
                }
                view.Spans = string.Join(" ", spans);
            return view;
        }

        private static Dictionary<int, Tuple<string, string>> LiteraryHemistichs(GanjoorVerseViewModel[] verses)
        {
            var res = new Dictionary<int, Tuple<string, string>>();
            int n = 0;
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
    }
}
