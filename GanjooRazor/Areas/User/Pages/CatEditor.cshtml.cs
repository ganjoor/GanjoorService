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
using RMuseum.Utils;

namespace GanjooRazor.Areas.User.Pages
{
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

            FatalError = Request.Query["FatalError"];
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var pageUrlResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={Request.Query["id"]}");
                    if (!pageUrlResponse.IsSuccessStatusCode)
                    {
                        FatalError = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(pageUrlResponse);
                        return Page();
                    }
                    var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

                    var pageQuery = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
                    if (!pageQuery.IsSuccessStatusCode)
                    {
                        FatalError = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(pageQuery);
                        return Page();
                    }
                    PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

                    CatId = PageInformation.PoetOrCat.Cat.Id;
                    var editResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat/correction/last/{CatId}");
                    if (!editResponse.IsSuccessStatusCode)
                    {
                        FatalError = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(editResponse);
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

        public async Task<IActionResult> OnPostDeleteCatCorrectionAsync(int catid)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.DeleteAsync(
                        $"{APIRoot.Url}/api/ganjoor/cat/correction/{catid}");
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
        }

        

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Correction.Description = GanjoorHtmlTools.StripHtmlTags(Correction.DescriptionHtml);
                using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
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
                            FatalError = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response);
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
            return Redirect($"/User/CatEditor?id={Request.Query["id"]}&FatalError={FatalError}");

        }


    }
}
