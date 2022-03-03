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
    public class FAQItemEditModel : PageModel
    {
        [BindProperty]
        public FAQItem Question { get; set; }
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

            if (!string.IsNullOrWhiteSpace(Request.Query["id"]))
            {
                using (HttpClient secureClient = new HttpClient())
                {
                    if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/faq/secure/{Request.Query["id"]}");
                        if (!response.IsSuccessStatusCode)
                        {
                            LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                            return Page();
                        }

                        Question = JsonConvert.DeserializeObject<FAQItem>(await response.Content.ReadAsStringAsync());

                    }
                    else
                    {
                        LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                    }
                }
            }
            else
            {
                Question = new FAQItem()
                {
                    CategoryId = int.Parse(Request.Query["catId"])
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);
                HttpResponseMessage response =
                    Question.Id == 0 ?
                    await secureClient.PostAsync($"{APIRoot.Url}/api/faq", new StringContent(JsonConvert.SerializeObject(Question), Encoding.UTF8, "application/json"))
                    :
                    await secureClient.PutAsync($"{APIRoot.Url}/api/faq", new StringContent(JsonConvert.SerializeObject(Question), Encoding.UTF8, "application/json"))
                    ;
                if (!response.IsSuccessStatusCode)
                {
                    LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return Page();
                }

                return Redirect($"/User/FAQItems?catId={Question.CategoryId}");
            }
        }

    }
}
