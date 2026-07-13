using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class CatUtilsModel : GanjoorPageModelBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="memoryCache"></param>
        public CatUtilsModel(HttpClient httpClient, IMemoryCache memoryCache) : base(httpClient)
        {
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
                LastMessage = await ReadErrorMessageAsync(response);
                return;
            }

            Languages = JsonConvert.DeserializeObject<GanjoorLanguage[]>(await response.Content.ReadAsStringAsync());
        }

        [BindProperty]
        public IFormFile SQLiteDb { get; set; }

        private async Task<bool> GetInformationAsync()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat?url={Request.Query["url"]}&poems=true&mainSections=true");
            if (!response.IsSuccessStatusCode)
            {
                LastMessage = await ReadErrorMessageAsync(response);
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
                LastMessage = await ReadErrorMessageAsync(pageQuery);
                return false;
            }
            PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

            var rhythmsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/rhythms");
            if (!rhythmsResponse.IsSuccessStatusCode)
            {
                LastMessage = await ReadErrorMessageAsync(rhythmsResponse);
                return false;
            }
            Rhythms = JsonConvert.DeserializeObject<GanjoorMetre[]>(await rhythmsResponse.Content.ReadAsStringAsync());

            var numberings = await _httpClient.GetAsync($"{APIRoot.Url}/api/numberings/cat/{Cat.Cat.Id}");
            if (!numberings.IsSuccessStatusCode)
            {
                LastMessage = await ReadErrorMessageAsync(numberings);
                return false;
            }
            Numberings = JsonConvert.DeserializeObject<GanjoorNumbering[]>(await numberings.Content.ReadAsStringAsync());

            if (!string.IsNullOrEmpty(Request.Query["images"]))
            {
                var images = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat/{Cat.Cat.Id}/images");
                if (!images.IsSuccessStatusCode)
                {
                    LastMessage = await ReadErrorMessageAsync(images);
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

            // Full-page form post: re-renders the page with LastMessage on failure rather than
            // returning a bare error result, so this keeps its own client/session block instead of
            // going through WithSecureClientAsync (which is for AJAX-style handlers).
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
                            LastMessage = await ReadErrorMessageAsync(response);
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
                        LastMessage = await ReadErrorMessageAsync(response);
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
            // Same note as OnPostAsync above re: full-page form post.
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/numberings", new StringContent(JsonConvert.SerializeObject(NumberingModel), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    LastMessage = await ReadErrorMessageAsync(response);
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

        public Task<IActionResult> OnDeleteAsync(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/numberings/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }

        public Task<IActionResult> OnPostStartRhymeAnalysisAsync(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/startassigningrhymes/{id}/{false}", null);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }

        public Task<IActionResult> OnPostStartGeneratingSubCatsTOCAsync(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/subcats/startgentoc/{id}", null);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }

        public Task<IActionResult> OnPostStartRhythmAnalysisAsync(int id, string rhythm)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/startassigningrhythms/{id}/{false}?rhythm={rhythm}", null);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }

        public Task<IActionResult> OnPostStartRegeneratingRelatedSections(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/{id}/regenrelatedsections", null);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }

        public Task<IActionResult> OnPostSetCategoryLanguageTagAsync(int id, string language)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/language/{id}/{language}", null);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }

        public Task<IActionResult> OnPostSetCategoryPoemFormatAsync(int id, GanjoorPoemFormat format)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/poemformat/{id}/{format}", null);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }

        public Task<IActionResult> OnPostRecountAsync(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/numberings/recount/start/{id}", null);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }

        public Task<IActionResult> OnPostRegenerateNumberingsAsync()
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PostAsync($"{APIRoot.Url}/api/numberings/generatemissing", null);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }

        public async Task<IActionResult> OnPostUploadDbAsync(IFormFile SQLiteDb)
        {
            await GetInformationAsync();

            // Full-page form post (file upload): re-renders the page with LastMessage, so this keeps
            // its own client/session block rather than using WithSecureClientAsync.
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
                        LastMessage = await ReadErrorMessageAsync(response);
                        return Page();
                    }

                    LastMessage = $"پایگاه داده‌ها بارگذاری شد. <a role=\"button\" href=\"/Admin/CatUtils?url={Cat.Cat.FullUrl}\" class=\"actionlink\">برگشت به صفحهٔ ویرایش دسته</a>";

                }
            }

            return Page();
        }

        public Task<IActionResult> OnDeletePoemAsync(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/poem/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }

        public Task<IActionResult> OnPostBatchReSlugCatPoemsAsync(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/reslugpoems/{id}", null);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }

        public async Task<IActionResult> OnPostUpdateCatMeta(GanjoorCatViewModel CatMeta)
        {
            await GetInformationAsync();

            // Full-page form post: re-renders the page with LastMessage on failure (or redirects on
            // success), so this keeps its own client/session block rather than using
            // WithSecureClientAsync.
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
                    LastMessage = await ReadErrorMessageAsync(response);
                    return Page();
                }
                return Redirect($"/Admin/CatUtils?url={Cat.Cat.FullUrl}");
            }
        }

        public Task<IActionResult> OnPostRemNaskbanImage(string url)
        {
            // NOTE: originally this endpoint called PrepareClient without checking its result (unlike
            // every sibling AJAX handler in this file), so an expired/missing session would silently
            // proceed with an unauthenticated client instead of failing fast with NotLoggedInMessage.
            // That looked like an oversight rather than an intentional difference, so it's now
            // consistent with the rest of this file via WithSecureClientAsync.
            return WithSecureClientAsync(async secureClient =>
            {
                HttpResponseMessage response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/naskban?naskbanUrl={url}");
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }

        public Task<IActionResult> OnPostSetCategoryDigitalSourceTagAsync(int id, string tag, string name)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/source?sourceUrlSlug={tag}");
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                DigitalSource source = JsonConvert.DeserializeObject<DigitalSource>(await response.Content.ReadAsStringAsync());
                if (source == null)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        return new BadRequestObjectResult("منبع وجود ندارد. باید نام آن را وارد کنید.");
                    }
                    source = new DigitalSource()
                    {
                        UrlSlug = tag,
                        ShortName = name,
                        FullName = name,
                        SourceType = "همراهان گنجور"
                    };
                }

                HttpResponseMessage responsePost = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/source/{id}",
                    new StringContent(JsonConvert.SerializeObject(source), Encoding.UTF8, "application/json")
                    );
                if (!responsePost.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(responsePost));
                }
                return new OkObjectResult(true);
            }, new OkObjectResult(false));
        }
    }
}
