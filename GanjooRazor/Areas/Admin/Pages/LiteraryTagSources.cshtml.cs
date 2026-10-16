using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Areas.Admin.Pages
{
    /// <summary>
    /// management of the sources of imported literary tags (shown to readers when they open a tag's source)
    /// </summary>
    public class LiteraryTagSourcesModel : PageModel
    {
        public string LastMessage { get; set; }

        public LiteraryTagSource[] Sources { get; set; } = Array.Empty<LiteraryTagSource>();

        public string SourcesJson => JsonConvert.SerializeObject(Sources, new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        });

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/sources");
                    if (!response.IsSuccessStatusCode)
                        LastMessage = await ApiErrorReader.ReadErrorAsync(response);
                    else
                        Sources = JsonConvert.DeserializeObject<LiteraryTagSource[]>(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    return Redirect("/login?redirect=" + Request.Path);
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync([FromBody] LiteraryTagSource source)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/literary/sources",
                        new StringContent(JsonConvert.SerializeObject(source), Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                        return new BadRequestObjectResult(await ApiErrorReader.ReadErrorAsync(response));
                    return new OkObjectResult(true);
                }
            }
            return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/literary/sources/{id}");
                    if (!response.IsSuccessStatusCode)
                        return new BadRequestObjectResult(await ApiErrorReader.ReadErrorAsync(response));
                    return new OkObjectResult(true);
                }
            }
            return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
        }
    }
}
