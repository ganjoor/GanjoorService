using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// statistics and browsing of literary devices (آرایه‌های ادبی):
    /// /literary (overview), /literary?device=ID, /literary?concept=ID (optionally with &amp;poet=ID and &amp;page=N)
    /// </summary>
    [OutputCache(PolicyName = "GanjoorPublicPage")]
    public class LiteraryModel : LoginPartialEnabledPageModel
    {
        /// <summary>
        /// tags per page
        /// </summary>
        public const int PageSize = 20;

        private readonly PoetCacheService _poetCache;

        public LiteraryModel(HttpClient httpClient, IConfiguration configuration, PoetCacheService poetCache) : base(httpClient, configuration)
        {
            _poetCache = poetCache;
        }

        public string LastError { get; set; }

        public List<GanjoorPoetViewModel> Poets { get; set; }

        public LiteraryStatsOverview Overview { get; set; }

        public Dictionary<int, LiteraryDevice> DevicesById { get; set; } = new Dictionary<int, LiteraryDevice>();

        public LiteraryDevice Device { get; set; }

        public LiteraryConcept Concept { get; set; }

        public LiteraryStatsConcept[] DeviceTopConcepts { get; set; } = Array.Empty<LiteraryStatsConcept>();

        public LiteraryStatsTagPage Tags { get; set; }

        public int DeviceId { get; set; }

        public int ConceptId { get; set; }

        public int PoetId { get; set; }

        public int PageNumber { get; set; } = 1;

        public string PoetName => PoetId > 0 ? Overview?.Poets?.FirstOrDefault(p => p.PoetId == PoetId)?.Name : null;

        public int PageCount => Tags == null ? 0 : (Tags.Total + PageSize - 1) / PageSize;

        /// <summary>
        /// a link to this page with some of the filters changed (0 clears a filter)
        /// </summary>
        public string Link(int? device = null, int? concept = null, int? poet = null, int? page = null)
        {
            int d = device ?? DeviceId, c = concept ?? ConceptId, p = poet ?? PoetId, n = page ?? 1;
            var parts = new List<string>();
            if (d > 0) parts.Add("device=" + d);
            if (c > 0) parts.Add("concept=" + c);
            if (p > 0) parts.Add("poet=" + p);
            if (n > 1) parts.Add("page=" + n);
            return "/literary" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
        }

        /// <summary>
        /// the hemistich with the words of the located parts marked
        /// </summary>
        public string MarkedHemistich(LiteraryStatsTagItem item, int misra)
        {
            string text = misra == 1 ? item.Hemistich1 : item.Hemistich2;
            if (string.IsNullOrEmpty(text))
                return "";
            var words = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            var marked = new bool[words.Length];
            foreach (var part in item.Parts ?? Array.Empty<LiteraryStatsTagPart>())
            {
                if (part.Misra != misra || part.WordStart == null || part.WordEnd == null)
                    continue;
                for (int i = Math.Max(1, part.WordStart.Value); i <= Math.Min(words.Length, part.WordEnd.Value); i++)
                    marked[i - 1] = true;
            }
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < words.Length; i++)
            {
                if (i > 0)
                    sb.Append(' ');
                string w = System.Net.WebUtility.HtmlEncode(words[i]);
                sb.Append(marked[i] ? "<mark>" + w + "</mark>" : w);
            }
            return sb.ToString();
        }

        public async Task<IActionResult> OnGetAsync(int device = 0, int concept = 0, int poet = 0, int page = 1)
        {
            var maintenanceResult = TryGetMaintenanceModeResult();
            if (maintenanceResult != null)
                return maintenanceResult;

            // the overview lives in the /vazn page now; this page lists the verses of a device or concept
            if (device <= 0 && concept <= 0 && poet <= 0)
                return Redirect("/vazn#literary-stats-tab-items");

            InitializeCommonPageState();

            DeviceId = Math.Max(0, device);
            ConceptId = Math.Max(0, concept);
            PoetId = Math.Max(0, poet);
            PageNumber = Math.Max(1, Math.Min(page, 100000));
            ViewData["Title"] = "آرایه‌های ادبی";

            var (poetsOk, poets, poetsError) = await _poetCache.GetPoetsAsync(EditorCacheBypass);
            if (!poetsOk)
            {
                LastError = poetsError;
                return Page();
            }
            Poets = poets;

            try
            {
                var devicesResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/devices");
                if (!devicesResponse.IsSuccessStatusCode)
                {
                    LastError = await ReadErrorMessageAsync(devicesResponse);
                    return Page();
                }
                DevicesById = JsonConvert.DeserializeObject<LiteraryDevice[]>(await devicesResponse.Content.ReadAsStringAsync()).ToDictionary(d => d.Id);

                var overviewResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/stats/overview?poetId={PoetId}");
                if (!overviewResponse.IsSuccessStatusCode)
                {
                    LastError = await ReadErrorMessageAsync(overviewResponse);
                    return Page();
                }
                Overview = JsonConvert.DeserializeObject<LiteraryStatsOverview>(await overviewResponse.Content.ReadAsStringAsync());

                if (DeviceId > 0)
                {
                    DevicesById.TryGetValue(DeviceId, out var d);
                    Device = d;
                    if (Device == null)
                    {
                        LastError = "آرایه پیدا نشد.";
                        return Page();
                    }
                }

                if (ConceptId > 0)
                {
                    var conceptResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/concepts/by-ids?ids={ConceptId}");
                    if (conceptResponse.IsSuccessStatusCode)
                        Concept = JsonConvert.DeserializeObject<LiteraryConcept[]>(await conceptResponse.Content.ReadAsStringAsync()).FirstOrDefault();
                    if (Concept == null)
                    {
                        LastError = "مفهوم پیدا نشد.";
                        return Page();
                    }
                }

                if (DeviceId > 0 || ConceptId > 0)
                {
                    if (DeviceId > 0)
                    {
                        var topResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/stats/device/{DeviceId}/top-concepts?poetId={PoetId}&take=30");
                        if (topResponse.IsSuccessStatusCode)
                            DeviceTopConcepts = JsonConvert.DeserializeObject<LiteraryStatsConcept[]>(await topResponse.Content.ReadAsStringAsync());
                    }
                    var tagsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/stats/tags?deviceId={DeviceId}&conceptId={ConceptId}&poetId={PoetId}&skip={(PageNumber - 1) * PageSize}&take={PageSize}");
                    if (!tagsResponse.IsSuccessStatusCode)
                    {
                        LastError = await ReadErrorMessageAsync(tagsResponse);
                        return Page();
                    }
                    Tags = JsonConvert.DeserializeObject<LiteraryStatsTagPage>(await tagsResponse.Content.ReadAsStringAsync());
                }

                string title = "آرایه‌های ادبی";
                if (Device != null)
                    title = Device.Name;
                if (Concept != null)
                    title += (Device != null ? " - " : "مفهوم ") + Concept.Name;
                if (PoetName != null)
                    title += " - " + PoetName;
                ViewData["Title"] = title;
            }
            catch (Exception exp)
            {
                LastError = exp.Message;
            }
            return Page();
        }
    }
}
