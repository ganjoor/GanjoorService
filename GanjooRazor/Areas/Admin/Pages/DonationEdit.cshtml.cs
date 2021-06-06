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


        public async Task<IActionResult> OnGetAsync()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/donations/{Request.Query["id"]}");

            response.EnsureSuccessStatusCode();

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
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);
                HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/donations/{Request.Query["id"]}", new StringContent(JsonConvert.SerializeObject(Donation), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                return Redirect("/Admin/Donations");
            }
        }
    }
}
