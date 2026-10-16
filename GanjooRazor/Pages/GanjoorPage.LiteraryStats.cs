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
    /// model of the literary devices statistics section of the /vazn page
    /// </summary>
    public class _LiteraryStatsPartialModel
    {
        public bool Failed { get; set; }
        public int PoetId { get; set; }
        public LiteraryStatsOverview Overview { get; set; }
        public System.Collections.Generic.Dictionary<int, LiteraryDevice> DevicesById { get; set; } = new System.Collections.Generic.Dictionary<int, LiteraryDevice>();

        public string PoetName => PoetId > 0 ? Overview?.Poets?.FirstOrDefault(p => p.PoetId == PoetId)?.Name : null;
    }

    public partial class GanjoorPageModel
    {
        /// <summary>
        /// statistics of literary devices (optionally for one poet), loaded by the /vazn page when its section is displayed.
        /// Note: the legacy redirect of /vazn?v... sends any query string containing the letter "v" to /simi,
        /// so none of this handler's query parameter names or values may contain it.
        /// </summary>
        public async Task<IActionResult> OnGetLiteraryStatsAsync(int poetId = 0)
        {
            var model = new _LiteraryStatsPartialModel() { PoetId = Math.Max(0, poetId) };
            try
            {
                var devices = await GetLiteraryDevicesCachedAsync();
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/stats/overview?poetId={model.PoetId}");
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
