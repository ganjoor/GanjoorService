using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Accounting.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class DonationEditModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public DonationEditModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [BindProperty]
        public UpdateDateDescriptionViewModel Donation { get; set; }

        public string LastMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = "";
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/donations/{Request.Query["id"]}");
            if(!response.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }

            var donation = JsonConvert.DeserializeObject<GanjoorDonationViewModel>(await response.Content.ReadAsStringAsync());

            Donation = new UpdateDateDescriptionViewModel()
            {
                Date = donation.RecordDate,
                Description = donation.DonorName
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LastMessage = "";
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);
                HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/donations/{Request.Query["id"]}", new StringContent(JsonConvert.SerializeObject(Donation), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return Page();
                }

                return Redirect("/Admin/Donations");
            }
        }
    }
}
