using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;
using System.Web;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class CatEditorModel : PageModel
    {
        /// <summary>
        /// my last edit
        /// </summary>
        public GanjoorCatCorrectionViewModel MyLastEdit { get; set; }

        /// <summary>
        /// page
        /// </summary>
        public GanjoorPageCompleteViewModel PageInformation { get; set; }
        /// <summary>
        /// fatal error
        /// </summary>
        public string FatalError { get; set; }

        /// <summary>
        /// cat id
        /// </summary>
        public int CatId { get; set; }

        [BindProperty]
        public GanjoorCatCorrectionViewModel Correction { get; set; }

        /// <summary>
        /// get
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            FatalError = "";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var pageUrlResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={Request.Query["id"]}");
                    if (!pageUrlResponse.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                        return Page();
                    }
                    var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

                    var pageQuery = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
                    if (!pageQuery.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await pageQuery.Content.ReadAsStringAsync());
                        return Page();
                    }
                    PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

                    CatId = PageInformation.PoetOrCat.Cat.Id;
                    var editResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat/correction/last/{CatId}");
                    if (!editResponse.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await editResponse.Content.ReadAsStringAsync());
                        return Page();
                    }
                    MyLastEdit = JsonConvert.DeserializeObject<GanjoorCatCorrectionViewModel>(await editResponse.Content.ReadAsStringAsync());

                    if (MyLastEdit != null)
                    {
                        Correction = JsonConvert.DeserializeObject<GanjoorCatCorrectionViewModel>(await editResponse.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        Correction = new GanjoorCatCorrectionViewModel()
                        {
                            CatId = CatId,
                            DescriptionHtml = PageInformation.PoetOrCat.Cat.DescriptionHtml,
                        };
                    }
                    

                }
                else
                {
                    FatalError = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteCatCorrectionsAsync(int catid)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.DeleteAsync(
                        $"{APIRoot.Url}/api/ganjoor/cat/correction/{catid}");
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
        }

        static string StripHtmlTags(string input)
        {
            // Remove HTML tags using Regex
            string textWithoutTags = Regex.Replace(input, "<.*?>", string.Empty);

            // Decode HTML entities (e.g., &amp; → &)
            return HttpUtility.HtmlDecode(textWithoutTags);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Correction.Description = StripHtmlTags(Correction.DescriptionHtml);
                using (HttpClient secureClient = new HttpClient())
                {
                    if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        HttpResponseMessage response = await secureClient.PostAsync(
                            $"{APIRoot.Url}/api/ganjoor/cat/correction",
                            new StringContent(JsonConvert.SerializeObject(Correction),
                            Encoding.UTF8,
                            "application/json"));
                        if (!response.IsSuccessStatusCode)
                        {
                            FatalError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        }
                        else
                        {
                            MyLastEdit = JsonConvert.DeserializeObject<GanjoorCatCorrectionViewModel>(await response.Content.ReadAsStringAsync());
                        }
                    }
                    else
                    {
                        FatalError = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                    }
                    
                }
            }
            catch (Exception exp)
            {
                FatalError = exp.ToString();

            }
            return Page();

        }


    }
}
