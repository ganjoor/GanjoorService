using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services.Implementation;
using RMuseum.Utils;
using RSecurityBackend.Models.Generic;


namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class SimiModel : LoginPartialEnabledPageModel
    {
        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration Configuration;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="memoryCache"></param>
        /// <param name="configuration"></param>
        public SimiModel(HttpClient httpClient, IMemoryCache memoryCache, IConfiguration configuration) : base(httpClient)
        {
            _memoryCache = memoryCache;
            Configuration
                = configuration;
        }

        /// <summary>
        /// aggressive cache
        /// </summary>
        public bool AggressiveCacheEnabled
        {
            get
            {
                try
                {
                    return bool.Parse(Configuration["AggressiveCacheEnabled"]);
                }
                catch
                {
                    return false;
                }
            }
        }

        public List<GanjoorPoetViewModel> Poets { get; set; }
        public int PoetId { get; set; }
        public int CatId { get; set; }
        public string CatFullTitle { get; set; }
        public string CatFullUrl { get; set; }
        public string Metre { get; set; }
        public string Rhyme { get; set; }
        public int CoupletCountsFrom { get; set; }
        public int CoupletCountsTo { get; set; }
        public GanjoorLanguage[] Languages { get; set; }
        public GanjoorPoetCompleteViewModel Poet { get; set; }
        public List<GanjoorPoemCompleteViewModel> Poems { get; set; }
        public string PagingToolsHtml { get; set; }

        public PaginationMetadata PaginationMetadata { get; set; }
        public string LastError { get; set; }
        public string Language { get; set; }
        public GanjoorPoemFormat Format { get; set; }
        public string Query { get; set; }
        public bool Quoted { get; set; }
        public bool ExactSearch { get; set; }


        /// <summary>
        /// can edit
        /// </summary>
        public bool CanEdit { get; set; }

        private async Task<bool> preparePoets()
        {
            var cacheKey = $"/api/ganjoor/poets";
            if (!_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel> poets))
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return false;
                }
                poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poets);
                }
            }

            Poets = poets;

            await ReadLanguagesAsync();
            return true;
        }

        private async Task<bool> preparePoet()
        {
            var cacheKey = $"/api/ganjoor/poet/{PoetId}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                var poetResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{PoetId}");
                if (!poetResponse.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await poetResponse.Content.ReadAsStringAsync());
                    return false;
                }
                poet = JObject.Parse(await poetResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poet);
                }
            }

            Poet = poet;
            return true;
        }

        private async Task ReadLanguagesAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{APIRoot.Url}/api/translations/languages");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return;
            }

            Languages = JsonConvert.DeserializeObject<GanjoorLanguage[]>(await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// rhythms alphabetically
        /// </summary>
        public GanjoorMetre[] RhythmsAlphabetically { get; set; }

        /// <summary>
        /// rhythms by frequency
        /// </summary>
        public GanjoorMetre[] RhythmsByVerseCount { get; set; }

        public async Task<IActionResult> OnGetPoetInformationAsync(int id)
        {
            if (id == 0)
                return new OkObjectResult(null);
            var cacheKey = $"/api/ganjoor/poet/{id}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                var poetResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{id}");
                if (!poetResponse.IsSuccessStatusCode)
                {
                    return BadRequest(JsonConvert.DeserializeObject<string>(await poetResponse.Content.ReadAsStringAsync()));
                }
                poet = JObject.Parse(await poetResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poet);
                }
            }
            return new OkObjectResult(poet);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }

            bool anyParamsGiven = false;

            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);

            CanEdit = Request.Cookies["CanEdit"] == "True";

            PoetId = string.IsNullOrEmpty(Request.Query["a"]) ? 0 : int.Parse(Request.Query["a"]);
            
            CatId = string.IsNullOrEmpty(Request.Query["c"]) ? 0 : int.Parse(Request.Query["c"]);

            anyParamsGiven |= PoetId != 0;

            Language = Request.Query["l"];

            anyParamsGiven |= Language != null;

            Language ??= "fa-IR";

            string f = Request.Query["f"];
            anyParamsGiven |= f != null;

            Format = GanjoorPoemFormat.Unknown;
            if(f != null)
            {
                Format = (GanjoorPoemFormat)int.Parse(f);
            }

            Query = Request.Query["s"].ApplyCorrectYeKe().Trim();
            ExactSearch = Request.Query["es"] == "1";
            bool quotes = Query.IndexOf("\"") != -1 || ExactSearch;
            Query = LanguageUtils.MakeTextSearchable(Query); //replace zwnj with space
            if (quotes)
                Query = $"\"{Query}\"";

            Quoted = quotes && Query.Contains(" ");


            ViewData["GoogleAnalyticsCode"] = Configuration["GoogleAnalyticsCode"];

            //todo: use html master layout or make it partial
            // 1. poets 
            if (false == (await preparePoets()))
                return Page();

            if (PoetId != 0)
            {
                if (false == (await preparePoet()))
                    return Page();
            }

            // 2. search verses

            

            var rhythmResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/rhythms?sortOnVerseCount=true");
            if (!rhythmResponse.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await rhythmResponse.Content.ReadAsStringAsync());
                return Page();
            }

            RhythmsByVerseCount = JsonConvert.DeserializeObject<GanjoorMetre[]>(await rhythmResponse.Content.ReadAsStringAsync());

            List<GanjoorMetre> rhythmsByVerseCount = new List<GanjoorMetre>(RhythmsByVerseCount);
            rhythmsByVerseCount.Sort((a, b) => a.Rhythm.CompareTo(b.Rhythm));
            rhythmsByVerseCount.Insert(0, new GanjoorMetre()
            {
                Rhythm = "null"
            }
            );
            rhythmsByVerseCount.Insert(0, new GanjoorMetre()
            {
                Rhythm = ""
            }
            );

            RhythmsAlphabetically = rhythmsByVerseCount.ToArray();

            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            Metre = Request.Query["v"];
            Rhyme = Request.Query["g"];

            anyParamsGiven |= Metre != null;
            anyParamsGiven |= Rhyme != null;

            Metre ??= "";
            Rhyme ??= "";


            if (!string.IsNullOrEmpty(Rhyme))
            {
                Rhyme = Rhyme.Replace(" ", "");
            }

            CoupletCountsFrom = 0;
            if (!string.IsNullOrEmpty(Request.Query["c1"]))
            {
                if(int.TryParse(Request.Query["c1"], out int i))
                {
                    CoupletCountsFrom = i;
                }
            }

            CoupletCountsTo = 0;
            if (!string.IsNullOrEmpty(Request.Query["c2"]))
            {
                if (int.TryParse(Request.Query["c2"], out int i))
                {
                    CoupletCountsTo = i;
                }
            }


            if (!anyParamsGiven)
            {
                ViewData["Title"] = $"گنجور » شعر‌ها یا ابیات مشابه";
                return Page();
            }




            string title = "شعرها یا ابیات ";

            if (PoetId != 0)
            {
                var poetInfo = Poets.Where(p => p.Id == PoetId).SingleOrDefault();
                if (poetInfo != null)
                {
                    title += $"{poetInfo.Nickname} ";
                }
            }
            if(!string.IsNullOrEmpty(Metre))
            {
                title += $"با وزن «{Metre}»";
            }

            if(!string.IsNullOrEmpty(Rhyme))
            {
                if(!string.IsNullOrEmpty(Metre))
                {
                    title += $" و";
                }
                else
                {
                    title += $"با";
                }
                title += $" حروف قافیهٔ «{Rhyme}»";
            }


            if(Language != "fa-IR")
            {
                var langModel = Languages.Where(l => l.Code == Language).FirstOrDefault();
                if (langModel != null)
                {
                    title += $" با زبان غالب «{langModel.Name}»";
                }
            }

            if(Format != GanjoorPoemFormat.Unknown)
            {
                title += $" در قالب شعری «{GanjoorPoemFormatConvertor.GetString(Format)}»";
            }

            if (CoupletCountsFrom != 0)
            {
                title += $" حداقل تعداد ابیات «{CoupletCountsFrom.ToPersianNumbers()}»";
            }

            if (CoupletCountsTo != 0)
            {
                title += $" حداکثر تعداد ابیات «{CoupletCountsTo.ToPersianNumbers()}»";
            }

            if (CatId != 0)
            {
                var catResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat/{CatId}?poems=false&mainSections=false");
                if (!catResponse.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await catResponse.Content.ReadAsStringAsync());
                    return Page();
                }
                else
                {
                    var cat = JObject.Parse(await catResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                    CatFullUrl = cat.Cat.FullUrl;
                    CatFullTitle = "";
                    foreach (var parentCat  in cat.Cat.Ancestors)
                    {
                        CatFullTitle += parentCat.Title;
                        CatFullTitle += " »";
                    }
                    CatFullTitle += " " + cat.Cat.Title;

                    title += $" در بخش {CatFullTitle}";
                }
            }
            else
            {
                CatFullUrl = "";
                CatFullTitle = "";
            }

            if (!string.IsNullOrEmpty(Query))
            {
                title += $" شامل کلیدواژهٔ «{Query}»";
            }

            string url = $"{APIRoot.Url}/api/ganjoor/poems/similar?PageNumber={pageNumber}&PageSize=20&metre={Metre}&rhyme={Rhyme}&poetId={PoetId}&catId={CatId}&language={Language}&format={(int)Format}&c1={CoupletCountsFrom}&c2={CoupletCountsTo}";
            if(!string.IsNullOrEmpty(Query))
            {
                url += $"&term={Query}";
            }
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }

            Poems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();

            foreach (var poem in Poems)
            {
                poem.HtmlText = GanjoorPoemTools.GetPoemHtmlExcerpt(poem.HtmlText);
            }

            string paginnationMetadata = response.Headers.GetValues("paging-headers").FirstOrDefault();
            PaginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata);

            string htmlText = "";
            if (PaginationMetadata.totalPages > 1)
            {
                htmlText = $"<div>{Environment.NewLine}";
                string authorParam = PoetId != 0 ? $"&amp;a={PoetId}" : "";
                if(authorParam != "" && CatId != 0)
                {
                    authorParam += $"&amp;c={CatId}";
                }
                title += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";
                if (PaginationMetadata.currentPage > 3)
                {
                    htmlText += $"<a href=\"/simi/?v={Uri.EscapeDataString(Metre)}&amp;g={Uri.EscapeDataString(Rhyme)}&amp;page=1{authorParam}&amp;l={Language}&amp;f={(int)Format}\"><div class=\"circled-number\">۱</div></a> …";
                }
                for (int i = PaginationMetadata.currentPage - 2; i <= (PaginationMetadata.currentPage + 2); i++)
                {
                    if (i >= 1 && i <= PaginationMetadata.totalPages)
                    {
                        if (i == PaginationMetadata.currentPage)
                        {
                            htmlText += $"<div class=\"circled-number-diff\">{i.ToPersianNumbers()}</div>{Environment.NewLine}";
                        }
                        else
                        {
                            htmlText += $"<a href=\"/simi/?v={Uri.EscapeDataString(Metre)}&amp;g={Uri.EscapeDataString(Rhyme)}&amp;page={i}{authorParam}&amp;l={Language}&amp;f={(int)Format}\"><div class=\"circled-number\">{i.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                        }
                    }
                }
                if (PaginationMetadata.totalPages > (PaginationMetadata.currentPage + 2))
                {
                    htmlText += $"… <a href=\"/simi/?v={Uri.EscapeDataString(Metre)}&amp;g={Uri.EscapeDataString(Rhyme)}&amp;page={PaginationMetadata.totalPages}{authorParam}&amp;l={Language}&amp;f={(int)Format}\"><div class=\"circled-number\">{PaginationMetadata.totalPages.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                }
                htmlText += $"</div>{Environment.NewLine}";
            }

            ViewData["Title"] = $"گنجور » {title}";
            PagingToolsHtml = htmlText;

            return Page();
        }
        public string HtmlText { get; set; }

        public async Task<IActionResult> OnPostSendSectionMetreSuggestionAsync(int poemId, int sectionIndex, string rhythm)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {

                    if (rhythm == "null")
                        rhythm = "";

                    if(string.IsNullOrEmpty(rhythm))
                    {
                        return new BadRequestObjectResult("وزن انتخاب نشده");
                    }

                    var sectionResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/sections/{poemId}");
                    if (!sectionResponse.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await sectionResponse.Content.ReadAsStringAsync()));
                    }
                    var sections = JsonConvert.DeserializeObject<GanjoorPoemSection[]>(await sectionResponse.Content.ReadAsStringAsync());

                    var section = sections.Where(s => s.Index == sectionIndex).Single();

                    var correctionResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/section/correction/last/{section.Id}");
                    if (!correctionResponse.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await correctionResponse.Content.ReadAsStringAsync()));
                        
                    }

                    if(null != JsonConvert.DeserializeObject<GanjoorPoemSectionCorrectionViewModel>(await correctionResponse.Content.ReadAsStringAsync()))
                    {
                        return new BadRequestObjectResult("شما پیشتر پیشنهادی تصحیحی برای این قطع ثبت کرده‌اید.");
                    }

                    GanjoorPoemSectionCorrectionViewModel correction = new GanjoorPoemSectionCorrectionViewModel()
                    {
                        SectionId = section.Id,
                        Rhythm = rhythm,
                    };

                    HttpResponseMessage response = await secureClient.PostAsync(
                        $"{APIRoot.Url}/api/ganjoor/section/correction",
                        new StringContent(JsonConvert.SerializeObject(correction),
                        Encoding.UTF8,
                        "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    return new OkObjectResult(true);
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }

    }
}
