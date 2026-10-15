using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Configuration;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// attribution page for the academic corpus whose rhetorical annotations are imported as literary tags
    /// </summary>
    [OutputCache(PolicyName = "GanjoorPublicPage")]
    public class LiteraryCorpusModel : LoginPartialEnabledPageModel
    {
        private readonly PoetCacheService _poetCache;

        public string LastError { get; set; }

        public List<GanjoorPoetViewModel> Poets { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var maintenanceResult = TryGetMaintenanceModeResult();
            if (maintenanceResult != null)
            {
                return maintenanceResult;
            }

            ViewData["Title"] = "گنجور » آرایه‌های ادبی » پیکرهٔ متن ادبی فارسی";
            InitializeCommonPageState();

            var (poetsOk, poets, poetsError) = await _poetCache.GetPoetsAsync(EditorCacheBypass);
            if (!poetsOk)
            {
                LastError = poetsError;
                return Page();
            }
            Poets = poets;
            return Page();
        }

        /// <summary>
        /// constructor
        /// </summary>
        public LiteraryCorpusModel(HttpClient httpClient, IConfiguration configuration, PoetCacheService poetCache) : base(httpClient, configuration)
        {
            _poetCache = poetCache;
        }
    }
}
