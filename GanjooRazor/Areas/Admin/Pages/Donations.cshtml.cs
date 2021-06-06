using System;
using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Accounting.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class DonationsModel : PageModel
    {
        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        [BindProperty]
        public GanjoorDonationViewModel Donation { get; set; }

        /// <summary>
        /// donations
        /// </summary>
        public GanjoorDonationViewModel[] Donations { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = "";

            Donation = new GanjoorDonationViewModel()
            {
                RecordDate = DateTime.Now.Date,
                Unit = "تومان"
            };

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/donations");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = await response.Content.ReadAsStringAsync();
                    }

                    response.EnsureSuccessStatusCode();

                    Donations = JsonConvert.DeserializeObject<GanjoorDonationViewModel[]>(await response.Content.ReadAsStringAsync());

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
