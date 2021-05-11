using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    public class ModifyPageModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public ModifyPageModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Corresponding Ganojoor Page
        /// </summary>
        /// <summary>
        /// api model
        /// </summary>
        [BindProperty]
        public GanjoorModifyPageViewModel ModifyModel { get; set; }

        /// <summary>
        /// rythm
        /// </summary>
        public GanjoorMetre[] Rhythms { get; set; }

        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = Request.Query["edit"] == "true" ? "ویرایش انجام شد." : "";
            if (string.IsNullOrEmpty(Request.Query["id"]))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "شناسهٔ صفحه مشخص نیست.");
            }
            var rhythmResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/rhythms");

            rhythmResponse.EnsureSuccessStatusCode();

            Rhythms = JsonConvert.DeserializeObject<GanjoorMetre[]>(await rhythmResponse.Content.ReadAsStringAsync());


            var pageUrlResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={Request.Query["id"]}");

            if (!pageUrlResponse.IsSuccessStatusCode)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, await pageUrlResponse.Content.ReadAsStringAsync());
            }

            pageUrlResponse.EnsureSuccessStatusCode();

            var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

            var pageQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
            if (!pageQuery.IsSuccessStatusCode)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, await pageQuery.Content.ReadAsStringAsync());
            }
            GanjoorPageCompleteViewModel page = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

            ModifyModel = new GanjoorModifyPageViewModel()
            {
                Title = page.Title,
                UrlSlug = page.UrlSlug,
                HtmlText = page.HtmlText,
                Note = "",
                SourceName = page.Poem == null ? null : page.Poem.SourceName,
                SourceUrlSlug = page.Poem == null ? null : page.Poem.SourceUrlSlug,
                OldTag = page.Poem == null ? null : page.Poem.OldTag,
                OldTagPageUrl = page.Poem == null ? null : page.Poem.OldTagPageUrl,
                RhymeLetters = page.Poem == null ? null : page.Poem.RhymeLetters,
                Rhythm = page.Poem == null ? null : page.Poem.GanjoorMetre == null ? null : page.Poem.GanjoorMetre.Rhythm
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var putResponse = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/page/{Request.Query["id"]}", new StringContent(JsonConvert.SerializeObject(ModifyModel), Encoding.UTF8, "application/json"));
                    if (!putResponse.IsSuccessStatusCode)
                    {
                        LastMessage = await putResponse.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        return Redirect($"/Admin/ModifyPage?id={Request.Query["id"]}&edit=true");
                    }
                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }


            return Page();

        }
    }
}
