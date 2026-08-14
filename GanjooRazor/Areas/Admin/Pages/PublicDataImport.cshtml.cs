using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    /// <summary>
    /// (Re)build local Ganjoor content from the public data export — local git clone or a URL
    /// (e.g. jsDelivr) — mainly meant to make setting up a fork or local dev copy easier than the
    /// old per-poet SQLite import.
    /// </summary>
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class PublicDataImportModel : PageModel
    {
        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        public IActionResult OnGet()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            LastMessage = "";

            return Page();
        }

        /// <summary>
        /// trigger the import job (runs in the background — check the Jobs page for progress)
        /// </summary>
        /// <param name="useHttp">true: location is a URL fetched over HTTP. false: location is a local folder path.</param>
        /// <param name="location">base URL or local folder path of the exported data tree</param>
        /// <param name="poetId">0 imports every poet; a specific poet id imports only that poet</param>
        public async Task<IActionResult> OnPostImportAsync(bool useHttp, string location, int poetId)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return BadRequest("مسیر یا نشانی نمی‌تواند خالی باشد.");
            }

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var body = new
                    {
                        useHttp,
                        location,
                        poetId
                    };

                    var response = await secureClient.PostAsync
                        (
                        $"{APIRoot.Url}/api/ganjoor/publicdata/import",
                        new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
                        );

                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }

                    return new OkObjectResult(true);
                }
            }

            return new OkObjectResult(false);
        }
    }
}
