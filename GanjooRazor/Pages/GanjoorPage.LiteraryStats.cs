using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// model of the literary devices statistics section (/vazn page, poet pages and category pages)
    /// </summary>
    public class _LiteraryStatsPartialModel
    {
        public bool Failed { get; set; }

        /// <summary>
        /// 0 = all poets
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// 0 = no category restriction
        /// </summary>
        public int CatId { get; set; }

        public LiteraryStatsOverview Overview { get; set; }

        public System.Collections.Generic.Dictionary<int, LiteraryDevice> DevicesById { get; set; } = new System.Collections.Generic.Dictionary<int, LiteraryDevice>();

        /// <summary>
        /// poet and category pages have their own stats section: nothing is displayed there when nothing is tagged
        /// </summary>
        public bool Scoped => PoetId > 0 || CatId > 0;

        /// <summary>
        /// the filter part of links to /literary
        /// </summary>
        public string FilterQuery => CatId > 0 ? "&cat=" + CatId : PoetId > 0 ? "&poet=" + PoetId : "";
    }

    public partial class GanjoorPageModel
    {
        /// <summary>
        /// statistics of literary devices (optionally for one poet or one category), loaded when their section is displayed.
        /// Note: the legacy redirect of /vazn?v... sends any query string containing the letter "v" to /simi,
        /// so none of this handler's query parameter names or values may contain it.
        /// </summary>
        public async Task<IActionResult> OnGetLiteraryStatsAsync(int poetId = 0, int catId = 0)
        {
            var model = new _LiteraryStatsPartialModel() { PoetId = Math.Max(0, poetId), CatId = Math.Max(0, catId) };
            try
            {
                var devices = await GetLiteraryDevicesCachedAsync();
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/stats/overview?poetId={model.PoetId}&catId={model.CatId}&includePoets=false");
                if (devices == null || !response.IsSuccessStatusCode)
                {
                    model.Failed = true;
                }
                else
                {
                    model.DevicesById = devices.ToDictionary(d => d.Id);
                    model.Overview = JsonConvert.DeserializeObject<LiteraryStatsOverview>(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception)
            {
                model.Failed = true;
            }
            return Partial("~/Pages/Partials/GanjoorPage/_LiteraryStatsPartial.cshtml", model);
        }
    }
}
