using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// the source of imported literary tags (a corpus, a book, ...) as a bare fragment, opened as an inline modal via
    /// LiteraryTagSourceWindow.open(key) in personwindow.js. The data comes from the LiteraryTagSources table
    /// (the tag's OriginRef is the source key), so a new source needs no code.
    /// </summary>
    public class LiteraryTagSourceWindowModel : LoginPartialEnabledPageModel
    {
        public LiteraryTagSourceWindowModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public string LastError { get; set; }

        public LiteraryTagSource Source { get; set; }

        public async Task<IActionResult> OnGetAsync(string key)
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/sources");
            if (!response.IsSuccessStatusCode)
            {
                LastError = "خطا در دریافت اطلاعات منبع.";
                return Page();
            }
            Source = JsonConvert.DeserializeObject<LiteraryTagSource[]>(await response.Content.ReadAsStringAsync())
                .FirstOrDefault(s => s.Key == key);
            if (Source == null)
                LastError = "اطلاعاتی برای این منبع ثبت نشده است.";
            return Page();
        }
    }
}
