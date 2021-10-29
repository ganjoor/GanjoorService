using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }

            return new OkObjectResult(false);
        }

        public async Task<ActionResult> OnPostSavePoetMetaAsync(int id, int birth, int death, int pinorder)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var poet = new GanjoorPoetViewModel()
                    {
                        Id = id,
                        BirthYearInLHijri = birth,
                        DeathYearInLHijri = death,
                        PinOrder = pinorder
                    };
                    var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/poet/{id}", new StringContent(JsonConvert.SerializeObject(poet), Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }

                    _memoryCache.Remove($"/api/ganjoor/poets");
                    _memoryCache.Remove($"/api/ganjoor/poet/{id}");

                    return new OkObjectResult(true);


                }
            }

            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostUpdatePeriodsAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/periods", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }

            return new OkObjectResult(false);
        }

        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="memoryCache"></param>
        public PoetsModel(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }
    }
}
