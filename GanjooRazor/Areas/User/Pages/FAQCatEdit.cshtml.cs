using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.FAQ;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class FAQCatEditModel : PageModel
    {
        [BindProperty]
        public FAQCategory Category { get; set; }
        public string LastMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            LastMessage = "";
            await GanjoorSessionChecker.ApplyPermissionsToViewData(Request, Response, ViewData);
            if (!ViewData.ContainsKey($"{RMuseumSecurableItem.FAQEntityShortName}-{RMuseumSecurableItem.ModerateOperationShortName}"))
            {
                LastMessage = "شما به این بخش دسترسی ندارید.";
                return Page();
            }

            if(!string.IsNullOrWhiteSpace(Request.Query["id"]))
            {
                using (HttpClient secureClient = new HttpClient())
                {
                    if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/faq/cat/secure/{Request.Query["id"]}");
                        if (!response.IsSuccessStatusCode)
                        {
                            LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                            return Page();
                        }

                        Category = JsonConvert.DeserializeObject<FAQCategory>(await response.Content.ReadAsStringAsync());

                    }
                    else
                    {
                        LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);
                HttpResponseMessage response =
                    Category.Id == 0 ? 
                    await secureClient.PostAsync($"{APIRoot.Url}/api/faq/cat", new StringContent(JsonConvert.SerializeObject(Category), Encoding.UTF8, "application/json"))
                    :
                    await secureClient.PutAsync($"{APIRoot.Url}/api/faq/cat", new StringContent(JsonConvert.SerializeObject(Category), Encoding.UTF8, "application/json"))
                    ;
                if (!response.IsSuccessStatusCode)
                {
                    LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return Page();
                }

                return Redirect("/User/FAQItems");
            }
        }
    }
}
