using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RSecurityBackend.Models.Generic.Db;

namespace GanjooRazor.Areas.Admin.Pages
{
    public class LongRunningJobsModel : PageModel
    {
        public RLongRunningJobStatus[] Jobs { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/rjobs");
                    response.EnsureSuccessStatusCode();

                    Jobs = JsonConvert.DeserializeObject<RLongRunningJobStatus[]>(await response.Content.ReadAsStringAsync());

                }
            }

            return Page();
        }
    }
}
