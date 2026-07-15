using DNTPersianUtils.Core;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.Ganjoor;
using RMuseum.Utils;
using RSecurityBackend.Models.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class TaggedModel : LoginPartialEnabledPageModel
    {
        private readonly PoetCacheService _poetCache;

        /// <summary>
        /// constructor
        /// </summary>
        public TaggedModel(HttpClient httpClient, IConfiguration configuration, PoetCacheService poetCache) : base(httpClient, configuration)
        {
            _poetCache = poetCache;
        }

        public List<GanjoorPoetViewModel> Poets { get; set; }
        public int PoetId { get; set; }
        public GanjoorPoetCompleteViewModel Poet { get; set; }
        public List<GanjoorPoemCompleteViewModel> Poems { get; set; }
        public string PagingToolsHtml { get; set; }
        public string LastError { get; set; }
        public string Language { get; set; }

        public GanjoorLanguage[] Languages { get; set; }

        private async Task ReadLanguagesAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{APIRoot.Url}/api/translations/languages");
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return;
            }

            Languages = JsonConvert.DeserializeObject<GanjoorLanguage[]>(await response.Content.ReadAsStringAsync());
        }

        public async Task<IActionResult> OnGetPoetInformationAsync(int id)
        {
            if (id == 0)
            {
                return new OkObjectResult(null);
            }
            var (success, poet, error) = await _poetCache.GetPoetAsync(id, AggressiveCacheEnabled);
            if (!success)
            {
                return BadRequest(error);
            }
            return new OkObjectResult(poet);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var maintenanceResult = TryGetMaintenanceModeResult();
            if (maintenanceResult != null)
            {
                return maintenanceResult;
            }

            InitializeCommonPageState();

            PoetId = string.IsNullOrEmpty(Request.Query["a"]) ? 0 : int.Parse(Request.Query["a"]);

            //todo: use html master layout or make it partial
            // 1. poets 
            var (poetsOk, poets, poetsError) = await _poetCache.GetPoetsAsync(AggressiveCacheEnabled);
            if (!poetsOk)
            {
                LastError = poetsError;
                return Page();
            }
            Poets = poets;
            await ReadLanguagesAsync();

            if (PoetId != 0)
            {
                var (poetOk, poet, poetError) = await _poetCache.GetPoetAsync(PoetId, AggressiveCacheEnabled);
                if (!poetOk)
                {
                    LastError = poetError;
                    return Page();
                }
                Poet = poet;
            }

            // 2. search verses



            var rhythmResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/rhythms?sortOnVerseCount=true");
            if (!rhythmResponse.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(rhythmResponse);
                return Page();
            }


            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            Language = Request.Query["l"];

            Language ??= "fa-IR";



            string title = "شعرها یا ابیات ";

            if (PoetId != 0)
            {
                var poetInfo = Poets.Where(p => p.Id == PoetId).SingleOrDefault();
                if (poetInfo != null)
                {
                    title += $"{poetInfo.Nickname} ";
                }
            }
            var langModel = Languages.Where(l => l.Code == Language).FirstOrDefault();
            if (langModel != null)
            {
                title += $"با زبان غالب «{langModel.Name}»";
            }



            string url = $"{APIRoot.Url}/api/ganjoor/sections/tagged/language?PageNumber={pageNumber}&PageSize=20&language={Language}&poetId={PoetId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return Page();
            }

            Poems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();

            foreach (var poem in Poems)
            {
                poem.HtmlText = GanjoorPoemTools.GetPoemHtmlExcerpt(poem.HtmlText);
            }

            string paginnationMetadata = response.Headers.GetValues("paging-headers").FirstOrDefault();
            PaginationMetadata paginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata);

            string htmlText = "";
            if (paginationMetadata.totalPages > 1)
            {
                htmlText = $"<div>{Environment.NewLine}";
                string authorParam = PoetId != 0 ? $"&amp;a={PoetId}" : "";
                title += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";
                if (paginationMetadata.currentPage > 3)
                {
                    htmlText += $"<a href=\"/tagged/?l={Uri.EscapeDataString(Language)}&amp;page=1{authorParam}\"><div class=\"circled-number\">۱</div></a> …";
                }
                for (int i = paginationMetadata.currentPage - 2; i <= (paginationMetadata.currentPage + 2); i++)
                {
                    if (i >= 1 && i <= paginationMetadata.totalPages)
                    {
                        if (i == paginationMetadata.currentPage)
                        {
                            htmlText += $"<div class=\"circled-number-diff\">{i.ToPersianNumbers()}</div>{Environment.NewLine}";
                        }
                        else
                        {
                            htmlText += $"<a href=\"/tagged/?l={Uri.EscapeDataString(Language)}&amp;page={i}{authorParam}\"><div class=\"circled-number\">{i.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                        }
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… <a href=\"/tagged/?l={Uri.EscapeDataString(Language)}&amp;page={paginationMetadata.totalPages}{authorParam}\"><div class=\"circled-number\">{paginationMetadata.totalPages.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                }
                htmlText += $"</div>{Environment.NewLine}";
            }

            ViewData["Title"] = $"گنجور » {title}";
            PagingToolsHtml = htmlText;

            return Page();
        }
        public string HtmlText { get; set; }

    }
}
