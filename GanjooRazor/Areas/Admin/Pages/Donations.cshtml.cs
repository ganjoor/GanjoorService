﻿using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
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
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public DonationsModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        /// <summary>
        /// email content
        /// </summary>
        public string EmailContent { get; set; }

        [BindProperty]
        public GanjoorDonationViewModel Donation { get; set; }

        /// <summary>
        /// donations
        /// </summary>
        public GanjoorDonationViewModel[] Donations { get; set; }

        /// <summary>
        /// show account info
        /// </summary>
        public string ShowAccountInfo { get; set; }

        private async Task ReadDonations()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/donations");
            if (!response.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
            }

            response.EnsureSuccessStatusCode();

            Donations = JsonConvert.DeserializeObject<GanjoorDonationViewModel[]>(await response.Content.ReadAsStringAsync());

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage resAccountInfo = await secureClient.GetAsync($"{APIRoot.Url}/api/donations/accountinfo/visible");
                    if (!resAccountInfo.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await resAccountInfo.Content.ReadAsStringAsync());
                    }

                    resAccountInfo.EnsureSuccessStatusCode();

                    ShowAccountInfo = JsonConvert.DeserializeObject<bool>(await resAccountInfo.Content.ReadAsStringAsync()) ? "نمایش حساب فعال است." : "نمایش حساب غیرفعال است.";
                }
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = "";

            Donation = new GanjoorDonationViewModel()
            {
                RecordDate = DateTime.Now.Date,
                Unit = "تومان"
            };

            await ReadDonations();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(GanjoorDonationViewModel Donation)
        {
            LastMessage = "";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    Donation.ImportedRecord = false;

                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/donations", new StringContent(JsonConvert.SerializeObject(Donation), Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        await ReadDonations();

                        EmailContent = $"با درود و سپاس از بزرگواری شما{Environment.NewLine}" +
                            $"کمک دریافتی به شماره ردیف {Donations.Length.ToPersianNumbers()} در این نشانی ثبت شد:{Environment.NewLine}" +
                            $"https://ganjoor.net/donate{Environment.NewLine}" +
                            $"نحوهٔ هزینه شدن آن متعاقباً در همان ردیف مستند خواهد شد.{Environment.NewLine}" +
                            $"سرافراز باشید.";
                    }

                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }

            return Page();
        }

        public async Task<IActionResult> OnPostRebuildPageAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/donations/page", null);
                    response.EnsureSuccessStatusCode();
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnDeleteAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/donations/{id}");
                    response.EnsureSuccessStatusCode();
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }
    }
}
