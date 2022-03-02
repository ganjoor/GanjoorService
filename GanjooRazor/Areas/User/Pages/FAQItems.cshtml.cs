using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.FAQ;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class FAQItemsModel : PageModel
    {
        public string LastMessage { get; set; }

        public FAQCategory[] Categories { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            if(string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            LastMessage = "";
            await GanjoorSessionChecker.ApplyPermissionsToViewData(Request, Response, ViewData);
            if (!ViewData.ContainsKey($"{RMuseumSecurableItem.FAQEntityShortName}-{RMuseumSecurableItem.ModerateOperationShortName}"))
            {
                LastMessage = "شما به این بخش دسترسی ندارید.";
                return Page();
            }

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/faq/cat/secure");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return Page();
                    }

                    Categories = JsonConvert.DeserializeObject<FAQCategory[]>(await response.Content.ReadAsStringAsync());

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
