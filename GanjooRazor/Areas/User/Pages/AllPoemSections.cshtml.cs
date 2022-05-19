using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Areas.User.Pages
{
    public class AllPoemSectionsModel : PageModel
    {
        /// <summary>
        /// fatal error
        /// </summary>
        public string FatalError { get; set; }

        public GanjoorPoemSection[] PoemSections { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var sectionsResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/sections/{Request.Query["id"]}");
                    if (!sectionsResponse.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await sectionsResponse.Content.ReadAsStringAsync());
                        return Page();
                    }
                    PoemSections = JsonConvert.DeserializeObject<GanjoorPoemSection[]>(await sectionsResponse.Content.ReadAsStringAsync());
                }
            }
            return Page();
        }
    }
}
