﻿using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ModifyPageModel : PageModel
    {

        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public ModifyPageModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Corresponding Ganojoor Page
        /// </summary>
        /// <summary>
        /// api model
        /// </summary>
        [BindProperty]
        public GanjoorModifyPageViewModel ModifyModel { get; set; }

        /// <summary>
        /// rythm
        /// </summary>
        public GanjoorMetre[] Rhythms { get; set; }

        /// <summary>
        /// page
        /// </summary>
        public GanjoorPageCompleteViewModel PageInformation { get; set; }

        /// <summary>
        /// ganjoor toc
        /// </summary>
        [BindProperty]
        public GanjoorTOC GanjoorTOC { get; set; }
        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        public string DefaultHtmlText { get; set; }

        private static string _DefHtmlText(GanjoorVerseViewModel[] verses)
        {
            string htmlText = "";
            int coupletIndex = 0;
            VersePosition position = VersePosition.Right;
            for (int vIndex = 0; vIndex < verses.Length; vIndex++)
            {
                GanjoorVerseViewModel v = verses[vIndex];
                if (position == VersePosition.Right)
                {
                    coupletIndex++;
                    htmlText += $"<div class=\"b\" id=\"bn{coupletIndex}\"><div class=\"m1\"><p>{v.Text}</p></div>{Environment.NewLine}";
                    position = VersePosition.Left;
                }
                else
                {
                    htmlText += $"<div class=\"m2\"><p>{v.Text}</p></div></div>{Environment.NewLine}";
                    position = VersePosition.Right;
                }
            }
            return htmlText.Trim();
        }

        private async Task<bool> PreparePage()
        {

            var rhythmResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/rhythms");
            if (!rhythmResponse.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await rhythmResponse.Content.ReadAsStringAsync());
                return false;
            }

            Rhythms = JsonConvert.DeserializeObject<GanjoorMetre[]>(await rhythmResponse.Content.ReadAsStringAsync());

            if (Request.Query["id"] == "0")
            {
                ModifyModel = new GanjoorModifyPageViewModel();
                PageInformation = new GanjoorPageCompleteViewModel();
                return true;
            }

            var pageUrlResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={Request.Query["id"]}");
            if (!pageUrlResponse.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                return false;
            }

            var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

            var pageQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
            if (!pageQuery.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await pageQuery.Content.ReadAsStringAsync());
                return false;
            }
            PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

            if (PageInformation.Poem != null)
            {
                DefaultHtmlText = _DefHtmlText(PageInformation.Poem.Verses);
            }

            ModifyModel = new GanjoorModifyPageViewModel()
            {
                Title = PageInformation.Title,
                UrlSlug = PageInformation.UrlSlug,
                HtmlText = PageInformation.HtmlText,
                Note = "",
                SourceName = PageInformation.Poem == null ? null : PageInformation.Poem.SourceName,
                SourceUrlSlug = PageInformation.Poem == null ? null : PageInformation.Poem.SourceUrlSlug,
                OldTag = PageInformation.Poem == null ? null : PageInformation.Poem.OldTag,
                OldTagPageUrl = PageInformation.Poem == null ? null : PageInformation.Poem.OldTagPageUrl,
                NoIndex = PageInformation.NoIndex,
                RedirectFromFullUrl = PageInformation.RedirectFromFullUrl,
                Language = PageInformation.Poem == null ? null : PageInformation.Poem.Language,
                MixedModeOrder = PageInformation.Poem == null ? (PageInformation.GanjoorPageType == GanjoorPageType.CatPage || PageInformation.GanjoorPageType == GanjoorPageType.PoetPage ? PageInformation.PoetOrCat.Cat.MixedModeOrder : 0) : PageInformation.Poem.MixedModeOrder,
                Published = PageInformation.Poem == null ? (PageInformation.GanjoorPageType == GanjoorPageType.CatPage || PageInformation.GanjoorPageType == GanjoorPageType.PoetPage ? PageInformation.PoetOrCat.Cat.Published : true) : PageInformation.Poem.Published,
                TableOfContentsStyle = PageInformation.GanjoorPageType == GanjoorPageType.CatPage || PageInformation.GanjoorPageType == GanjoorPageType.PoetPage ? PageInformation.PoetOrCat.Cat.TableOfContentsStyle : GanjoorTOC.Analyse,
                CatType = PageInformation.GanjoorPageType == GanjoorPageType.CatPage || PageInformation.GanjoorPageType == GanjoorPageType.PoetPage ? PageInformation.PoetOrCat.Cat.CatType : GanjoorCatType.Default,
                Description = PageInformation.GanjoorPageType == GanjoorPageType.CatPage || PageInformation.GanjoorPageType == GanjoorPageType.PoetPage ? PageInformation.PoetOrCat.Cat.Description : "",
                DescriptionHtml = PageInformation.GanjoorPageType == GanjoorPageType.CatPage || PageInformation.GanjoorPageType == GanjoorPageType.PoetPage ? PageInformation.PoetOrCat.Cat.DescriptionHtml : ""
            };
            GanjoorTOC = ModifyModel.TableOfContentsStyle;
            return true;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");


            LastMessage = Request.Query["edit"] == "true" ? "ویرایش انجام شد." : "";
            if (string.IsNullOrEmpty(Request.Query["id"]))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "شناسهٔ صفحه مشخص نیست.");
            }
            GanjoorTOC = GanjoorTOC.Analyse;
            await PreparePage();
            
            return Page();
        }

        public async Task<IActionResult> OnGetComputeRhymeAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/analysisrhyme/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
            }
            var rhyme = JsonConvert.DeserializeObject<GanjooRhymeAnalysisResult>(await response.Content.ReadAsStringAsync());
            return new OkObjectResult(rhyme.Rhyme);
        }

        public async Task<IActionResult> OnPostAsync(GanjoorModifyPageViewModel ModifyModel)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    if(Request.Query["id"] == "0")
                    {
                        var response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/page", new StringContent(JsonConvert.SerializeObject(ModifyModel), Encoding.UTF8, "application/json"));
                        if (!response.IsSuccessStatusCode)
                        {
                            LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        }
                        else
                        {
                            var newPage = JsonConvert.DeserializeObject<GanjoorPage>(await response.Content.ReadAsStringAsync());
                            return Redirect($"/Admin/ModifyPage?id={newPage.Id}&edit=true");
                        }
                    }
                    else
                    {
                        var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/page/{Request.Query["id"]}", new StringContent(JsonConvert.SerializeObject(ModifyModel), Encoding.UTF8, "application/json"));
                        if (!response.IsSuccessStatusCode)
                        {
                            LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        }
                        else
                        {
                            return Redirect($"/Admin/ModifyPage?id={Request.Query["id"]}&edit=true");
                        }
                    }
                    
                }
                else
                {
                    LastMessage = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostGenerateCatPageAsync(GanjoorTOC GanjoorTOC)
        {
            if (!(await PreparePage()))
                return Page();

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat?url={PageInformation.FullUrl}&poems=true");
            if (!response.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }


            var Cat = JsonConvert.DeserializeObject<GanjoorPoetCompleteViewModel>(await response.Content.ReadAsStringAsync());

            if (Request.Form["directInsert"].Count == 1)
            {
                using (HttpClient secureClient = new HttpClient())
                {
                    if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        var htmlRes = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/toc/{Cat.Cat.Id}/{(int)GanjoorTOC}", null);
                        if (!htmlRes.IsSuccessStatusCode)
                        {
                            LastMessage = JsonConvert.DeserializeObject<string>(await htmlRes.Content.ReadAsStringAsync());
                            return Page();
                        }

                        return Redirect($"/Admin/ModifyPage?id={Request.Query["id"]}&edit=true");

                    }
                    else
                    {
                        LastMessage = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                    }
                }
                return Page();
            }
            else
            {
                using (HttpClient secureClient = new HttpClient())
                {
                    if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        var htmlRes = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat/toc/{Cat.Cat.Id}/{(int)GanjoorTOC}");
                        if (!htmlRes.IsSuccessStatusCode)
                        {
                            LastMessage = JsonConvert.DeserializeObject<string>(await htmlRes.Content.ReadAsStringAsync());
                            return Page();
                        }

                        ModifyModel.HtmlText = await htmlRes.Content.ReadAsStringAsync();

                        LastMessage = $"متن تولیدی دریافت شد. لطفا آن را کپی کنید و سپس <a href=\"/Admin/ModifyPage?id={Request.Query["id"]}\">اینجا</a> کلیک کنید و آن را درج نمایید.";


                    }
                    else
                    {
                        LastMessage = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                    }
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRebuildSitemapAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/sitemap", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostRebuildTajikSitemapAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/sitemap/tajik", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }


        public async Task<IActionResult> OnPostRebuildStatsAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/rebuild/stats", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        
        public async Task<IActionResult> OnPostRefillSectionsCoupletCountsAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/fillsectioncoupletcounts", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }




        public async Task<IActionResult> OnPostRebuildRelatedPagesAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/quoted/pages/generate", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostRebuildDigitalSourcesStatsAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/source/stats/rebuild", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostTransilerateAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/transilerate", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }


        


        public async Task<IActionResult> OnPostCleanCacheAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/page/cache/{id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostRebuildWordCountsAsync(int poetId)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/wordcounts/rebuild/{poetId}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostImportNaskbanPaperSourcesAsync(int poetId, string username, string password)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    LoginViewModel model = new LoginViewModel()
                    {
                        Username = username,
                        Password = password,
                        ClientAppName = "GanjooRazor",
                        Language = "fa-IR"
                    };
                    var stringContent = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/naskban/import/poetbooks/{poetId}", stringContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }



        public async Task<IActionResult> OnPostImportMuseumPaperSourcesAsync(int poetId)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/papersources/import/{poetId}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }




    }
}
