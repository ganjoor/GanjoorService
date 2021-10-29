using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class LocationsModel : PageModel
    {
        public string LastMessage { get; set; }
        public GanjoorGeoLocation[] Locations { get; set; }

        [BindProperty]
        public GanjoorGeoLocation Location { get; set; }

        private async Task ReadLocationsAsync()
        {
            LastMessage = "";

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/locations");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return;
                    }

                    response.EnsureSuccessStatusCode();

                    Locations = JsonConvert.DeserializeObject<GanjoorGeoLocation[]>(await response.Content.ReadAsStringAsync());

                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }
        }
        public async Task<IActionResult> OnGetAsync()
        {
            await ReadLocationsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(GanjoorGeoLocation Location)
        {
            LastMessage = "";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/locations", new StringContent(JsonConvert.SerializeObject(Location), Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        await ReadLocationsAsync();
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
