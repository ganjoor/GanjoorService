using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class CatUtilsModel : PageModel
    {
        // <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="memoryCache"></param>
        public CatUtilsModel(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// category
        /// </summary>
        public GanjoorPoetCompleteViewModel Cat { get; set; }

        /// <summary>
        /// cat page
        /// </summary>
        public GanjoorPageCompleteViewModel PageInformation { get; set; }

        [BindProperty]
        public GanjoorBatchNamingModel NamingModel { get; set; }


        /// <summary>
        /// rythm
        /// </summary>
        public GanjoorMetre[] Rhythms { get; set; }


        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        /// <summary>
        /// renaming output
        /// </summary>
        public string[] RenamingOutput { get; set; }

        /// <summary>
        /// numbering patterns
        /// </summary>
        public GanjoorNumbering[] Numberings { get; set; }

        /// <summary>
        /// numbering model
        /// </summary>
        [BindProperty]
        public GanjoorNumbering NumberingModel { get; set; }

        public GanjoorLanguage[] Languages { get; set; }

        [BindProperty]
        public GanjoorCatViewModel CatMeta { get; set; }

        public PoemRelatedImageEx[] PoemRelatedImages { get; set; }

        private async Task ReadLanguagesAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{APIRoot.Url}/api/translations/languages");
            if (!response.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return;
            }

            Languages = JsonConvert.DeserializeObject<GanjoorLanguage[]>(await response.Content.ReadAsStringAsync());
        }

        [BindProperty]
        public IFormFile SQLiteDb { get; set; }

        private async Task<bool> GetInformationAsync()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat?url={Request.Query["url"]}&poems=true&mainSections=true");
            if(!response.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return false;
            }
            Cat = JsonConvert.DeserializeObject<GanjoorPoetCompleteViewModel>(await response.Content.ReadAsStringAsync());

            if (CatMeta == null)
            {
                CatMeta = new GanjoorCatViewModel()
                {
                    Id = Cat.Cat.Id,
                    BookName = Cat.Cat.BookName,
                    SumUpSubsGeoLocations = Cat.Cat.SumUpSubsGeoLocations,
                    MapName = Cat.Cat.MapName,
                };
            }
           

            var pageQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={Request.Query["url"]}");
            if (!pageQuery.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await pageQuery.Content.ReadAsStringAsync());
                return false;
            }
            PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

            var rhythmsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/rhythms");
            if (!rhythmsResponse.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await rhythmsResponse.Content.ReadAsStringAsync());
                return false;
            }
            Rhythms = JsonConvert.DeserializeObject<GanjoorMetre[]>(await rhythmsResponse.Content.ReadAsStringAsync());

            var numberings = await _httpClient.GetAsync($"{APIRoot.Url}/api/numberings/cat/{Cat.Cat.Id}");
            if (!numberings.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await numberings.Content.ReadAsStringAsync());
                return false;
            }
            Numberings = JsonConvert.DeserializeObject<GanjoorNumbering[]>(await numberings.Content.ReadAsStringAsync());

            if (!string.IsNullOrEmpty(Request.Query["images"]))
            {
                var images = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat/{Cat.Cat.Id}/images");
                if (!images.IsSuccessStatusCode)
                {
                    LastMessage = JsonConvert.DeserializeObject<string>(await images.Content.ReadAsStringAsync());
                    return false;
                }
                PoemRelatedImages = JsonConvert.DeserializeObject<PoemRelatedImageEx[]>(await images.Content.ReadAsStringAsync());
            }
            else
            {
                PoemRelatedImages = [];
            }


            await ReadLanguagesAsync();
            return true;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            LastMessage = Request.Query["edit"] == "true" ? "ویرایش انجام شد." : "";

            if (string.IsNullOrEmpty(Request.Query["url"]))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "نشانی صفحه مشخص نیست.");
            }

            await GetInformationAsync();

            NamingModel = new GanjoorBatchNamingModel()
            {
                StartWithNotIncludingSpaces = "شمارهٔ ",
                RemovePreviousPattern = true,
                RemoveSetOfCharacters = ".-",
                Simulate = true
            };

            NumberingModel = new GanjoorNumbering()
            {
                Name = Cat.Cat.Title,
                StartCatId = Cat.Cat.Id,
                EndCatId = Cat.Cat.Id
            };

            return Page();
        }

        /// <summary>
        /// تغییر عنوان گروهی
        /// </summary>
        /// <param name="NamingModel"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostAsync(GanjoorBatchNamingModel NamingModel)
        {
            await GetInformationAsync();

            NumberingModel = new GanjoorNumbering()
            {
                Name = Cat.Cat.Title,
                StartCatId = Cat.Cat.Id,
                EndCatId = Cat.Cat.Id
            };

            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                if (Request.Form["renameSubcats"].Count == 1)
                {
                    foreach (var subCat in Cat.Cat.Children)
                    {
                        HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/recaptionpoems/{subCat.Id}", new StringContent(JsonConvert.SerializeObject(NamingModel), Encoding.UTF8, "application/json"));
                        if (!response.IsSuccessStatusCode)
                        {
                            LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                            return Page();
                        }
                        else
                        {
                            RenamingOutput = JsonConvert.DeserializeObject<string[]>(await response.Content.ReadAsStringAsync());
                        }
                    }
                }
                else
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/recaptionpoems/{Cat.Cat.Id}", new StringContent(JsonConvert.SerializeObject(NamingModel), Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return Page();
                    }
                    else
                    {
                        RenamingOutput = JsonConvert.DeserializeObject<string[]>(await response.Content.ReadAsStringAsync());
                    }
                    NamingModel.Simulate = false;
                }
                
            }

            return Page();
        }

        public async Task<IActionResult> OnPostNumberingAsync(GanjoorNumbering NumberingModel)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/numberings", new StringContent(JsonConvert.SerializeObject(NumberingModel), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                }
            }

            await GetInformationAsync();

            NamingModel = new GanjoorBatchNamingModel()
            {
                StartWithNotIncludingSpaces = "شمارهٔ ",
                RemovePreviousPattern = true,
                RemoveSetOfCharacters = ".-",
                Simulate = true
            };

            return Page();
        }

        public async Task<IActionResult> OnDeleteAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/numberings/{id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(res);
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostStartRhymeAnalysisAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/startassigningrhymes/{id}/{false}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(res);
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostStartGeneratingSubCatsTOCAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/subcats/startgentoc/{id}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(res);
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostStartRhythmAnalysisAsync(int id, string rhythm)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/startassigningrhythms/{id}/{false}?rhythm={rhythm}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(res);
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostStartRegeneratingRelatedSections(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/{id}/regenrelatedsections", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(res);
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostSetCategoryLanguageTagAsync(int id, string language)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/language/{id}/{language}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(res);
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostSetCategoryPoemFormatAsync(int id, GanjoorPoemFormat format)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/poemformat/{id}/{format}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(res);
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostRecountAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/numberings/recount/start/{id}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(res);
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostRegenerateNumberingsAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/numberings/generatemissing", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(res);
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostUploadDbAsync(IFormFile SQLiteDb)
        {
            await GetInformationAsync();


            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                MultipartFormDataContent form = new MultipartFormDataContent();

                using (MemoryStream stream = new MemoryStream())
                {
                    await SQLiteDb.CopyToAsync(stream);
                    var fileContent = stream.ToArray();
                    form.Add(new ByteArrayContent(fileContent, 0, fileContent.Length), Cat.Poet.Nickname, SQLiteDb.FileName);

                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/sqlite/import/cat/{Cat.Cat.Id}", form);
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return Page();
                    }

                    LastMessage = $"پایگاه داده‌ها بارگذاری شد. <a role=\"button\" href=\"/Admin/CatUtils?url={Cat.Cat.FullUrl}\" class=\"actionlink\">برگشت به صفحهٔ ویرایش دسته</a>";

                }
            }

            return Page();
        }

        public async Task<IActionResult> OnDeletePoemAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/poem/{id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(res);
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostBatchReSlugCatPoemsAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/reslugpoems/{id}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        var res = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(res);
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostUpdateCatMeta(GanjoorCatViewModel CatMeta)
        {
            await GetInformationAsync();


            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                CatMeta.BookName ??= "";
                CatMeta.MapName ??= "";

                MultipartFormDataContent form = new MultipartFormDataContent
                {
                    { new StringContent(CatMeta.BookName), "bookName" },
                    { new StringContent(CatMeta.SumUpSubsGeoLocations.ToString()), "sumUpSubsGeoLocations" },
                    { new StringContent(CatMeta.MapName), "mapName" }
                };

                if (CatMeta.NewImage != null)
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        await CatMeta.NewImage.CopyToAsync(stream);
                        var fileContent = stream.ToArray();
                        form.Add(new ByteArrayContent(fileContent, 0, fileContent.Length), CatMeta.BookName, CatMeta.NewImage.FileName);
                    }  
                }
                HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/extra/{Cat.Cat.Id}", form);
                if (!response.IsSuccessStatusCode)
                {
                    LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return Page();
                }
                return Redirect($"/Admin/CatUtils?url={Cat.Cat.FullUrl}");
            }
        }
    }
}
