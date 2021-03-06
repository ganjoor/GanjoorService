﻿using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class PoetsModel : PageModel
    {
        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        /// <summary>
        /// poets
        /// </summary>
        public GanjoorPoetViewModel[] Poets { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = "";

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets/secure");
                    response.EnsureSuccessStatusCode();

                    Poets = JsonConvert.DeserializeObject<GanjoorPoetViewModel[]>(await response.Content.ReadAsStringAsync());

                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }

            return Page();
        }

        public async Task<IActionResult> OnPostExportAllAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/sqlite/batchexport", null);
                    response.EnsureSuccessStatusCode();

                    return new OkObjectResult(true);


                }
            }

            return new OkObjectResult(false);
        }
    }
}
