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

        public async Task<IActionResult> OnDeleteAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/locations/{id}");

                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }

                }
            }
            return new JsonResult(true);
        }

        public async Task<IActionResult> OnPutEditAsync(int id, string name, double latitude, double longitude)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    GanjoorGeoLocation model = new GanjoorGeoLocation()
                    {
                       Id = id,
                       Name = name,
                       Latitude = latitude,
                       Longitude = longitude
                    };
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/locations", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }

            return new JsonResult(true);
        }
    }
}
