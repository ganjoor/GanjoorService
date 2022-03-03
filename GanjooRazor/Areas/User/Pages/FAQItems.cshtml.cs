using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.FAQ;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class FAQItemsModel : PageModel
    {
        public string LastMessage { get; set; }

        public FAQCategory[] Categories { get; set; }

        public FAQItem[] CategoryItems { get; set; }

        public int CatId { get; set; }

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

            CatId = 0;

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

                    if(Categories.Length > 0)
                    {
                        CatId = string.IsNullOrEmpty(Request.Query["catId"]) ? Categories[0].Id : int.Parse(Request.Query["catId"]);
                        response = await secureClient.GetAsync($"{APIRoot.Url}/api/faq/cat/items?catId={CatId}");
                        if (!response.IsSuccessStatusCode)
                        {
                            return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                        }

                        var items = JsonConvert.DeserializeObject<FAQItem[]>(await response.Content.ReadAsStringAsync());
                        if (items == null)
                        {
                            items = new FAQItem[] { };
                        }

                        CategoryItems = items;
                    }

                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnDeleteAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/faq/cat/{id}");

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                }
                else
                {
                    return new BadRequestObjectResult("لطفا از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
            return new JsonResult(true);
        }

        public async Task<IActionResult> OnGetCategoryItemsAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/faq/cat/items?catId={id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }

                    var items = JsonConvert.DeserializeObject<FAQItem[]>(await response.Content.ReadAsStringAsync());
                    if(items == null)
                    {
                        items = new FAQItem[] { };
                    }
                    return new JsonResult(items);

                }
                else
                {
                    return new BadRequestObjectResult("لطفا از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }
    }
}
